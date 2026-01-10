using System.Security.Claims;
using FamilyFitness.Application;
using FamilyFitness.Domain;
using FamilyFitness.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register database
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
        options.UseInMemoryDatabase("FamilyFitnessTesting"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("family-fitness")
        ?? throw new InvalidOperationException("PostgreSQL connection string not found");

    builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Register repositories
builder.Services.AddScoped<IWorkoutTypeRepository, PostgresWorkoutTypeRepository>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<IGroupRepository, PostgresGroupRepository>();
builder.Services.AddScoped<IGroupMembershipRepository, PostgresGroupMembershipRepository>();
builder.Services.AddScoped<IWorkoutSessionRepository, PostgresWorkoutSessionRepository>();
builder.Services.AddScoped<IWorkoutSessionWorkoutTypeRepository, PostgresWorkoutSessionWorkoutTypeRepository>();
builder.Services.AddScoped<IWorkoutSessionParticipantRepository, PostgresWorkoutSessionParticipantRepository>();
builder.Services.AddScoped<IWorkoutIntervalScoreRepository, PostgresWorkoutIntervalScoreRepository>();

// Register services
builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();
builder.Services.AddScoped<WorkoutTypeService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<GroupMembershipService>();
builder.Services.AddScoped<WorkoutSessionService>();
builder.Services.AddScoped<WorkoutSessionWorkoutTypeService>();
builder.Services.AddScoped<WorkoutSessionParticipantService>();
builder.Services.AddScoped<WorkoutIntervalScoreService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = builder.Configuration["AzureAd:Audience"],
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AzureAd:Issuer"] ?? builder.Configuration["AzureAd:Authority"],
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Debug endpoint to check API authentication
app.MapGet("/api/debug/auth", (ClaimsPrincipal user) =>
{
    return new
    {
        IsAuthenticated = user.Identity?.IsAuthenticated,
        AuthenticationType = user.Identity?.AuthenticationType,
        Name = user.Identity?.Name,
        Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
    };
}).RequireAuthorization();

// Ensure database is created and seeded (for Development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FamilyFitnessDbContext>();
    
    // Retry logic to wait for PostgreSQL to be ready
    var maxRetries = 30;
    var delay = TimeSpan.FromSeconds(2);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            
            // Seed the database with sample data
            await DatabaseSeeder.SeedAsync(dbContext);
            
            break;
        }
        catch (Npgsql.NpgsqlException) when (i < maxRetries - 1)
        {
            app.Logger.LogInformation("Waiting for PostgreSQL to be ready... (Attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
            await Task.Delay(delay);
        }
    }
}

// Add /api/me endpoint for user provisioning
app.MapGet("/api/me", async (ClaimsPrincipal user, UserService userService, ILogger<Program> logger) =>
{
    // Log all claims for debugging
    logger.LogInformation("[API/ME] Processing request for user provisioning");
    logger.LogInformation("[API/ME] Claims: {Claims}", string.Join(" | ", user.Claims.Select(c => $"{c.Type}={c.Value}")));
    
    // Azure Entra External ID sends email as "emails" (plural) claim
    // Also check "email", "preferred_username", and standard ClaimTypes.Email
    var emailClaim = user.FindFirst("emails")?.Value 
        ?? user.FindFirst("email")?.Value 
        ?? user.FindFirst("preferred_username")?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value;
    
    logger.LogInformation("[API/ME] Extracted email claim: {Email}", emailClaim ?? "NOT FOUND");
    
    if (string.IsNullOrWhiteSpace(emailClaim))
    {
        logger.LogWarning("[API/ME] Email claim not found in token");
        return Results.BadRequest(new { error = "Email claim not found. Available claims: " + string.Join(", ", user.Claims.Select(c => c.Type)) });
    }

    // Try to find existing user by email
    try
    {
        var existingUsers = await userService.GetAllAsync();
        var existingUser = existingUsers.FirstOrDefault(u => u.Email.Equals(emailClaim, StringComparison.OrdinalIgnoreCase));
        
        if (existingUser != null)
        {
            logger.LogInformation("[API/ME] Found existing user: {UserId} - {Email}", existingUser.Id, existingUser.Email);
            return Results.Ok(existingUser);
        }
        
        logger.LogInformation("[API/ME] No existing user found for email: {Email}", emailClaim);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "[API/ME] Error looking up existing user, will attempt to create new user");
    }

    // Create new user
    try
    {
        var nameClaim = user.FindFirst("name")?.Value 
            ?? user.FindFirst(ClaimTypes.Name)?.Value 
            ?? user.FindFirst("given_name")?.Value
            ?? "Unknown";
        var username = nameClaim.Replace(" ", "").ToLowerInvariant(); // Simple username generation
        
        logger.LogInformation("[API/ME] Creating new user - Name: {Name}, Username: {Username}, Email: {Email}", nameClaim, username, emailClaim);
        
        // Handle potential username conflicts by adding numbers
        var baseUsername = username;
        var counter = 1;
        var allUsers = await userService.GetAllAsync();
        
        while (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        var createCommand = new CreateUserCommand(username, emailClaim);
        var newUser = await userService.CreateAsync(createCommand);
        
        logger.LogInformation("[API/ME] Successfully created new user: {UserId} - {Username} - {Email}", newUser.Id, newUser.Username, newUser.Email);
        
        return Results.Ok(newUser);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[API/ME] Error creating user for email: {Email}", emailClaim);
        return Results.Problem($"Error creating user: {ex.Message}");
    }
})
.WithName("GetCurrentUser")
.RequireAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// Workout Types endpoints
app.MapGet("/api/workout-types", async (WorkoutTypeService service) =>
{
    var workoutTypes = await service.GetAllAsync();
    return Results.Ok(workoutTypes);
})
.WithName("GetAllWorkoutTypes")
.RequireAuthorization();

app.MapGet("/api/workout-types/{id}", async (string id, WorkoutTypeService service) =>
{
    try
    {
        var workoutType = await service.GetByIdAsync(id);
        return Results.Ok(workoutType);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutTypeById")
.RequireAuthorization();

app.MapPost("/api/workout-types", async (CreateWorkoutTypeCommand command, WorkoutTypeService service) =>
{
    try
    {
        var workoutType = await service.CreateAsync(command);
        return Results.Created($"/api/workout-types/{workoutType.Id}", workoutType);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateWorkoutType")
.RequireAuthorization();

app.MapPut("/api/workout-types/{id}", async (string id, UpdateWorkoutTypeCommand command, WorkoutTypeService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var workoutType = await service.UpdateAsync(command);
        return Results.Ok(workoutType);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateWorkoutType")
.RequireAuthorization();

app.MapDelete("/api/workout-types/{id}", async (string id, WorkoutTypeService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteWorkoutType")
.RequireAuthorization();

// User endpoints
app.MapGet("/api/users", async (UserService service) =>
{
    var users = await service.GetAllAsync();
    return Results.Ok(users);
})
.WithName("GetAllUsers")
.RequireAuthorization();

app.MapGet("/api/users/{id:guid}", async (Guid id, UserService service) =>
{
    try
    {
        var user = await service.GetByIdAsync(id);
        return Results.Ok(user);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetUserById")
.RequireAuthorization();

app.MapPost("/api/users", async (CreateUserCommand command, UserService service) =>
{
    try
    {
        var user = await service.CreateAsync(command);
        return Results.Created($"/api/users/{user.Id}", user);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateUser")
.RequireAuthorization();

app.MapPut("/api/users/{id:guid}", async (Guid id, UpdateUserCommand command, UserService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var user = await service.UpdateAsync(command);
        return Results.Ok(user);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateUser")
.RequireAuthorization();

app.MapDelete("/api/users/{id:guid}", async (Guid id, UserService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteUser")
.RequireAuthorization();

// Group endpoints
app.MapGet("/api/groups", async (GroupService service) =>
{
    var groups = await service.GetAllAsync();
    return Results.Ok(groups);
})
.WithName("GetAllGroups")
.RequireAuthorization();

app.MapGet("/api/groups/{id:guid}", async (Guid id, GroupService service) =>
{
    try
    {
        var group = await service.GetByIdAsync(id);
        return Results.Ok(group);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetGroupById")
.RequireAuthorization();

app.MapPost("/api/groups", async (CreateGroupCommand command, GroupService service) =>
{
    try
    {
        var group = await service.CreateAsync(command);
        return Results.Created($"/api/groups/{group.Id}", group);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateGroup")
.RequireAuthorization();

app.MapPut("/api/groups/{id:guid}", async (Guid id, UpdateGroupCommand command, GroupService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var group = await service.UpdateAsync(command);
        return Results.Ok(group);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateGroup")
.RequireAuthorization();

app.MapDelete("/api/groups/{id:guid}", async (Guid id, GroupService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteGroup")
.RequireAuthorization();

// GroupMembership endpoints
app.MapGet("/api/group-memberships", async (GroupMembershipService service) =>
{
    var memberships = await service.GetAllAsync();
    return Results.Ok(memberships);
})
.WithName("GetAllGroupMemberships")
.RequireAuthorization();

app.MapGet("/api/group-memberships/{id:guid}", async (Guid id, GroupMembershipService service) =>
{
    try
    {
        var membership = await service.GetByIdAsync(id);
        return Results.Ok(membership);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetGroupMembershipById")
.RequireAuthorization();

app.MapGet("/api/groups/{groupId:guid}/memberships", async (Guid groupId, GroupMembershipService service) =>
{
    var memberships = await service.GetByGroupIdAsync(groupId);
    return Results.Ok(memberships);
})
.WithName("GetGroupMembershipsByGroupId")
.RequireAuthorization();

app.MapGet("/api/users/{userId:guid}/memberships", async (Guid userId, GroupMembershipService service) =>
{
    var memberships = await service.GetByUserIdAsync(userId);
    return Results.Ok(memberships);
})
.WithName("GetGroupMembershipsByUserId")
.RequireAuthorization();

app.MapPost("/api/group-memberships", async (CreateGroupMembershipCommand command, GroupMembershipService service) =>
{
    try
    {
        var membership = await service.CreateAsync(command);
        return Results.Created($"/api/group-memberships/{membership.Id}", membership);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateGroupMembership")
.RequireAuthorization();

app.MapPut("/api/group-memberships/{id:guid}", async (Guid id, UpdateGroupMembershipCommand command, GroupMembershipService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var membership = await service.UpdateAsync(command);
        return Results.Ok(membership);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateGroupMembership")
.RequireAuthorization();

app.MapDelete("/api/group-memberships/{id:guid}", async (Guid id, GroupMembershipService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteGroupMembership")
.RequireAuthorization();

// WorkoutSession endpoints
app.MapGet("/api/workout-sessions", async (WorkoutSessionService service) =>
{
    var sessions = await service.GetAllAsync();
    return Results.Ok(sessions);
})
.WithName("GetAllWorkoutSessions")
.RequireAuthorization();

app.MapGet("/api/workout-sessions/{id:guid}", async (Guid id, WorkoutSessionService service) =>
{
    try
    {
        var session = await service.GetByIdAsync(id);
        return Results.Ok(session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutSessionById")
.RequireAuthorization();

app.MapGet("/api/groups/{groupId:guid}/workout-sessions", async (Guid groupId, WorkoutSessionService service) =>
{
    var sessions = await service.GetByGroupIdAsync(groupId);
    return Results.Ok(sessions);
})
.WithName("GetWorkoutSessionsByGroupId")
.RequireAuthorization();

app.MapGet("/api/users/{creatorId:guid}/workout-sessions", async (Guid creatorId, WorkoutSessionService service) =>
{
    var sessions = await service.GetByCreatorIdAsync(creatorId);
    return Results.Ok(sessions);
})
.WithName("GetWorkoutSessionsByCreatorId")
.RequireAuthorization();

app.MapPost("/api/workout-sessions", async (CreateWorkoutSessionCommand command, WorkoutSessionService service) =>
{
    try
    {
        var session = await service.CreateAsync(command);
        return Results.Created($"/api/workout-sessions/{session.Id}", session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("CreateWorkoutSession")
.RequireAuthorization();

app.MapPut("/api/workout-sessions/{id:guid}", async (Guid id, UpdateWorkoutSessionCommand command, WorkoutSessionService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var session = await service.UpdateAsync(command);
        return Results.Ok(session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("UpdateWorkoutSession")
.RequireAuthorization();

app.MapDelete("/api/workout-sessions/{id:guid}", async (Guid id, WorkoutSessionService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteWorkoutSession")
.RequireAuthorization();

// WorkoutSession control endpoints
app.MapPost("/api/workout-sessions/{id:guid}/start", async (Guid id, WorkoutSessionService service) =>
{
    try
    {
        var command = new StartSessionCommand(id);
        var session = await service.StartSessionAsync(command);
        return Results.Ok(session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("StartWorkoutSession")
.RequireAuthorization();

app.MapPost("/api/workout-sessions/{id:guid}/cancel", async (Guid id, WorkoutSessionService service) =>
{
    try
    {
        var command = new CancelSessionCommand(id);
        var session = await service.CancelSessionAsync(command);
        return Results.Ok(session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CancelWorkoutSession")
.RequireAuthorization();

app.MapPost("/api/workout-sessions/{id:guid}/complete", async (Guid id, WorkoutSessionService service) =>
{
    try
    {
        var command = new CompleteSessionCommand(id);
        var session = await service.CompleteSessionAsync(command);
        return Results.Ok(session);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CompleteWorkoutSession")
.RequireAuthorization();

app.MapGet("/api/groups/{groupId:guid}/active-session", async (Guid groupId, WorkoutSessionService service) =>
{
    var session = await service.GetActiveSessionByGroupIdAsync(groupId);
    if (session == null)
    {
        return Results.NotFound(new { error = "No active session found for this group" });
    }
    return Results.Ok(session);
})
.WithName("GetActiveWorkoutSession")
.RequireAuthorization();

app.MapGet("/api/workout-sessions/{id:guid}/assignments", async (Guid id, WorkoutSessionService service, WorkoutTypeService workoutTypeService) =>
{
    try
    {
        var assignments = await service.GetSessionAssignmentsAsync(id);
        
        // Enrich station data with workout type names
        var enrichedStations = new List<StationDto>();
        foreach (var station in assignments.Stations)
        {
            try
            {
                var workoutType = await workoutTypeService.GetByIdAsync(station.WorkoutTypeId);
                enrichedStations.Add(new StationDto(
                    station.StationIndex,
                    station.WorkoutTypeId,
                    workoutType.Name,
                    workoutType.Description
                ));
            }
            catch
            {
                // If workout type not found, use original
                enrichedStations.Add(station);
            }
        }
        
        var enrichedAssignments = new SessionAssignmentDto(
            assignments.SessionId,
            assignments.Status,
            assignments.Participants,
            enrichedStations
        );
        
        return Results.Ok(enrichedAssignments);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutSessionAssignments")
.RequireAuthorization();


// WorkoutSessionWorkoutType endpoints
app.MapGet("/api/workout-session-workout-types", async (WorkoutSessionWorkoutTypeService service) =>
{
    var items = await service.GetAllAsync();
    return Results.Ok(items);
})
.WithName("GetAllWorkoutSessionWorkoutTypes")
.RequireAuthorization();

app.MapGet("/api/workout-session-workout-types/{id:guid}", async (Guid id, WorkoutSessionWorkoutTypeService service) =>
{
    try
    {
        var item = await service.GetByIdAsync(id);
        return Results.Ok(item);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutSessionWorkoutTypeById")
.RequireAuthorization();

app.MapGet("/api/workout-sessions/{workoutSessionId:guid}/workout-types", async (Guid workoutSessionId, WorkoutSessionWorkoutTypeService service) =>
{
    var items = await service.GetByWorkoutSessionIdAsync(workoutSessionId);
    return Results.Ok(items);
})
.WithName("GetWorkoutSessionWorkoutTypesBySessionId")
.RequireAuthorization();

app.MapPost("/api/workout-session-workout-types", async (CreateWorkoutSessionWorkoutTypeCommand command, WorkoutSessionWorkoutTypeService service) =>
{
    try
    {
        var item = await service.CreateAsync(command);
        return Results.Created($"/api/workout-session-workout-types/{item.Id}", item);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateWorkoutSessionWorkoutType")
.RequireAuthorization();

app.MapPut("/api/workout-session-workout-types/{id:guid}", async (Guid id, UpdateWorkoutSessionWorkoutTypeCommand command, WorkoutSessionWorkoutTypeService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var item = await service.UpdateAsync(command);
        return Results.Ok(item);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateWorkoutSessionWorkoutType")
.RequireAuthorization();

app.MapDelete("/api/workout-session-workout-types/{id:guid}", async (Guid id, WorkoutSessionWorkoutTypeService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteWorkoutSessionWorkoutType")
.RequireAuthorization();

// WorkoutSessionParticipant endpoints
app.MapGet("/api/workout-session-participants", async (WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetAllAsync();
    return Results.Ok(participants);
})
.WithName("GetAllWorkoutSessionParticipants")
.RequireAuthorization();

app.MapGet("/api/workout-session-participants/{id:guid}", async (Guid id, WorkoutSessionParticipantService service) =>
{
    try
    {
        var participant = await service.GetByIdAsync(id);
        return Results.Ok(participant);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutSessionParticipantById")
.RequireAuthorization();

app.MapGet("/api/workout-sessions/{workoutSessionId:guid}/participants", async (Guid workoutSessionId, WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetByWorkoutSessionIdAsync(workoutSessionId);
    return Results.Ok(participants);
})
.WithName("GetWorkoutSessionParticipantsBySessionId")
.RequireAuthorization();

app.MapGet("/api/users/{userId:guid}/participations", async (Guid userId, WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetByUserIdAsync(userId);
    return Results.Ok(participants);
})
.WithName("GetWorkoutSessionParticipantsByUserId")
.RequireAuthorization();

app.MapPost("/api/workout-session-participants", async (CreateWorkoutSessionParticipantCommand command, WorkoutSessionParticipantService service) =>
{
    try
    {
        var participant = await service.CreateAsync(command);
        return Results.Created($"/api/workout-session-participants/{participant.Id}", participant);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateWorkoutSessionParticipant")
.RequireAuthorization();

app.MapPut("/api/workout-session-participants/{id:guid}", async (Guid id, UpdateWorkoutSessionParticipantCommand command, WorkoutSessionParticipantService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var participant = await service.UpdateAsync(command);
        return Results.Ok(participant);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateWorkoutSessionParticipant")
.RequireAuthorization();

app.MapDelete("/api/workout-session-participants/{id:guid}", async (Guid id, WorkoutSessionParticipantService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteWorkoutSessionParticipant")
.RequireAuthorization();

// WorkoutIntervalScore endpoints
app.MapGet("/api/workout-interval-scores", async (WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetAllAsync();
    return Results.Ok(scores);
})
.WithName("GetAllWorkoutIntervalScores")
.RequireAuthorization();

app.MapGet("/api/workout-interval-scores/{id:guid}", async (Guid id, WorkoutIntervalScoreService service) =>
{
    try
    {
        var score = await service.GetByIdAsync(id);
        return Results.Ok(score);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetWorkoutIntervalScoreById")
.RequireAuthorization();

app.MapGet("/api/workout-session-participants/{participantId:guid}/scores", async (Guid participantId, WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetByParticipantIdAsync(participantId);
    return Results.Ok(scores);
})
.WithName("GetWorkoutIntervalScoresByParticipantId")
.RequireAuthorization();

app.MapGet("/api/workout-types/{workoutTypeId}/scores", async (string workoutTypeId, WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetByWorkoutTypeIdAsync(workoutTypeId);
    return Results.Ok(scores);
})
.WithName("GetWorkoutIntervalScoresByWorkoutTypeId")
.RequireAuthorization();

app.MapPost("/api/workout-interval-scores", async (CreateWorkoutIntervalScoreCommand command, WorkoutIntervalScoreService service) =>
{
    try
    {
        var score = await service.CreateAsync(command);
        return Results.Created($"/api/workout-interval-scores/{score.Id}", score);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateWorkoutIntervalScore")
.RequireAuthorization();

app.MapPut("/api/workout-interval-scores/{id:guid}", async (Guid id, UpdateWorkoutIntervalScoreCommand command, WorkoutIntervalScoreService service) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var score = await service.UpdateAsync(command);
        return Results.Ok(score);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateWorkoutIntervalScore")
.RequireAuthorization();

app.MapDelete("/api/workout-interval-scores/{id:guid}", async (Guid id, WorkoutIntervalScoreService service) =>
{
    try
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("DeleteWorkoutIntervalScore")
.RequireAuthorization();

app.Run();

public partial class Program { }
