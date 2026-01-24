using System.Security.Claims;
using FamilyFitness.Application;
using FamilyFitness.Domain;
using FamilyFitness.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Azure.Identity;
using Azure.Core;

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

    // Configure Npgsql to use Azure.Identity for managed identity authentication
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    
    // Add periodic token refresh for Azure AD authentication
    dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
    {
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://ossrdbms-aad.database.windows.net/.default" }),
            ct);
        return token.Token;
    }, TimeSpan.FromHours(1), TimeSpan.FromSeconds(10));
    
    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
        options.UseNpgsql(dataSource));
}

// Register repositories
builder.Services.AddScoped<IWorkoutTypeRepository, PostgresWorkoutTypeRepository>();
builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<IGroupRepository, PostgresGroupRepository>();
builder.Services.AddScoped<IGroupMembershipRepository, PostgresGroupMembershipRepository>();
builder.Services.AddScoped<IGroupInviteRepository, PostgresGroupInviteRepository>();
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
builder.Services.AddScoped<GroupInviteService>();
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



// Run migrations on startup for all environments
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FamilyFitnessDbContext>();
    
    var maxRetries = 30;
    var delay = TimeSpan.FromSeconds(2);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            app.Logger.LogInformation("Running database migrations... (Attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations completed successfully");
            
            // Seed only in Development
            if (app.Environment.IsDevelopment())
            {
                await DatabaseSeeder.SeedAsync(dbContext);
                app.Logger.LogInformation("Database seeded successfully");
            }
            
            break;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            app.Logger.LogWarning(ex, "Migration attempt {Attempt}/{MaxRetries} failed, retrying in {Delay} seconds...", 
                i + 1, maxRetries, delay.TotalSeconds);
            await Task.Delay(delay);
        }
        catch (Exception ex) when (i == maxRetries - 1)
        {
            app.Logger.LogError(ex, "Failed to run migrations after {MaxRetries} attempts. Application will continue but may not function correctly.", maxRetries);
        }
    }
}

// Add /api/me endpoint for user provisioning
app.MapGet("/api/me", async (ClaimsPrincipal user, UserService userService, ILogger<Program> logger) =>
{
    // Log all claims for debugging
    logger.LogInformation("[API/ME] Processing request for user provisioning");
    logger.LogInformation("[API/ME] Claims: {Claims}", string.Join(" | ", user.Claims.Select(c => $"{c.Type}={c.Value}")));
    
    // Get Entra Object ID (oid) - this is the stable identifier for the user
    var entraObjectId = user.FindFirst("oid")?.Value 
        ?? user.FindFirst("objectidentifier")?.Value
        ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    logger.LogInformation("[API/ME] Entra Object ID: {OID}", entraObjectId ?? "NOT FOUND");
    
    // Check identity provider - may be google.com, facebook.com, etc.
    var idp = user.FindFirst("idp")?.Value 
        ?? user.FindFirst("identityprovider")?.Value
        ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/identityprovider")?.Value;
    logger.LogInformation("[API/ME] Identity Provider: {IDP}", idp ?? "Local/Entra");
    
    // Get the real email address (not the .onmicrosoft.com shadow account)
    // Priority: emailaddress > emails > email > ClaimTypes.Email
    var emailClaim = user.FindFirst("emailaddress")?.Value
        ?? user.FindFirst("emails")?.Value 
        ?? user.FindFirst("email")?.Value 
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst("signInNames.emailAddress")?.Value
        ?? user.FindFirst("otherMails")?.Value;
    
    // If email is still null or looks like @onmicrosoft.com, try preferred_username as last resort
    if (string.IsNullOrWhiteSpace(emailClaim) || emailClaim.Contains(".onmicrosoft.com"))
    {
        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrWhiteSpace(preferredUsername) && !preferredUsername.Contains(".onmicrosoft.com"))
        {
            emailClaim = preferredUsername;
        }
    }
    
    logger.LogInformation("[API/ME] Extracted email claim: {Email}", emailClaim ?? "NOT FOUND");
    
    if (string.IsNullOrWhiteSpace(emailClaim))
    {
        logger.LogWarning("[API/ME] Email claim not found in token");
        return Results.BadRequest(new { error = "Email claim not found. Available claims: " + string.Join(", ", user.Claims.Select(c => c.Type)) });
    }

    // Try to find existing user - first by EntraObjectId (most reliable), then by email
    try
    {
        // Priority 1: Look up by EntraObjectId (stable identifier)
        if (!string.IsNullOrWhiteSpace(entraObjectId))
        {
            var existingByEntraId = await userService.GetByEntraObjectIdAsync(entraObjectId);
            if (existingByEntraId != null)
            {
                logger.LogInformation("[API/ME] Found existing user by EntraObjectId: {UserId} - {Email}", existingByEntraId.Id, existingByEntraId.Email);
                return Results.Ok(existingByEntraId);
            }
        }
        
        // Priority 2: Look up by email (for backwards compatibility / users created before EntraObjectId was added)
        var existingUsers = await userService.GetAllAsync();
        var existingByEmail = existingUsers.FirstOrDefault(u => u.Email.Equals(emailClaim, StringComparison.OrdinalIgnoreCase));
        
        if (existingByEmail != null)
        {
            logger.LogInformation("[API/ME] Found existing user by email: {UserId} - {Email}", existingByEmail.Id, existingByEmail.Email);
            return Results.Ok(existingByEmail);
        }
        
        logger.LogInformation("[API/ME] No existing user found for EntraObjectId: {OID} or email: {Email}", entraObjectId, emailClaim);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "[API/ME] Error looking up existing user, will attempt to create new user");
    }

    // Create new user
    try
    {
        // Get display name from various claims
        var givenName = user.FindFirst("givenname")?.Value 
            ?? user.FindFirst("given_name")?.Value 
            ?? user.FindFirst(ClaimTypes.GivenName)?.Value;
        var familyName = user.FindFirst("surname")?.Value 
            ?? user.FindFirst("family_name")?.Value 
            ?? user.FindFirst(ClaimTypes.Surname)?.Value;
        
        // Construct full name from given + family name
        string? nameClaim = null;
        if (!string.IsNullOrWhiteSpace(givenName))
        {
            nameClaim = string.IsNullOrWhiteSpace(familyName) 
                ? givenName 
                : $"{givenName} {familyName}";
        }
        
        // If still no name, try the "name" claim (but skip if it's "unknown" or looks like email)
        if (string.IsNullOrWhiteSpace(nameClaim))
        {
            var rawName = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrWhiteSpace(rawName) && 
                !rawName.Equals("unknown", StringComparison.OrdinalIgnoreCase) &&
                !rawName.Contains("@") && 
                !rawName.Contains(".onmicrosoft.com"))
            {
                nameClaim = rawName;
            }
        }
        
        // Fallback: use the local part of email as name
        if (string.IsNullOrWhiteSpace(nameClaim) || nameClaim.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            nameClaim = emailClaim.Split('@')[0];
            if (nameClaim.Length > 0)
            {
                nameClaim = char.ToUpper(nameClaim[0]) + nameClaim.Substring(1);
            }
        }
        
        if (string.IsNullOrWhiteSpace(nameClaim))
        {
            nameClaim = "Unknown";
        }
        
        // Generate username from name (remove spaces, lowercase, alphanumeric only)
        var username = nameClaim.Replace(" ", "").ToLowerInvariant();
        username = new string(username.Where(c => char.IsLetterOrDigit(c)).ToArray());
        
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "user";
        }
        
        logger.LogInformation("[API/ME] Creating new user - EntraObjectId: {OID}, Name: {Name}, Username: {Username}, Email: {Email}", 
            entraObjectId, nameClaim, username, emailClaim);
        
        // Handle potential username conflicts by adding numbers
        var baseUsername = username;
        var counter = 1;
        var allUsers = await userService.GetAllAsync();
        
        while (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        var createCommand = new CreateUserCommand(entraObjectId, username, emailClaim);
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

// Helper function to get current user ID from claims with auto-provisioning
async Task<Guid?> GetCurrentUserIdAsync(ClaimsPrincipal user, UserService userService)
{
    // Priority 1: Look up by EntraObjectId (most reliable)
    var entraObjectId = user.FindFirst("oid")?.Value 
        ?? user.FindFirst("objectidentifier")?.Value
        ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
    
    if (!string.IsNullOrWhiteSpace(entraObjectId))
    {
        var userByEntraId = await userService.GetByEntraObjectIdAsync(entraObjectId);
        if (userByEntraId != null)
        {
            return userByEntraId.Id;
        }
    }
    
    // Priority 2: Look up by email (for backwards compatibility)
    var emailClaim = user.FindFirst("emailaddress")?.Value
        ?? user.FindFirst("emails")?.Value 
        ?? user.FindFirst("email")?.Value 
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst("signInNames.emailAddress")?.Value;
    
    // Only use preferred_username if it doesn't look like an @onmicrosoft.com shadow account
    if (string.IsNullOrWhiteSpace(emailClaim))
    {
        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrWhiteSpace(preferredUsername) && !preferredUsername.Contains(".onmicrosoft.com"))
        {
            emailClaim = preferredUsername;
        }
    }
    
    if (string.IsNullOrWhiteSpace(emailClaim))
        return null;
    
    var users = await userService.GetAllAsync();
    var currentUser = users.FirstOrDefault(u => u.Email.Equals(emailClaim, StringComparison.OrdinalIgnoreCase));
    
    // If user exists, return their ID
    if (currentUser != null)
    {
        return currentUser.Id;
    }
    
    // User doesn't exist - auto-provision them (similar to /api/me endpoint logic)
    try
    {
        // Get display name from various claims
        var givenName = user.FindFirst("givenname")?.Value 
            ?? user.FindFirst("given_name")?.Value 
            ?? user.FindFirst(ClaimTypes.GivenName)?.Value;
        var familyName = user.FindFirst("surname")?.Value 
            ?? user.FindFirst("family_name")?.Value 
            ?? user.FindFirst(ClaimTypes.Surname)?.Value;
        
        // Construct full name from given + family name
        string? nameClaim = null;
        if (!string.IsNullOrWhiteSpace(givenName))
        {
            nameClaim = string.IsNullOrWhiteSpace(familyName) 
                ? givenName 
                : $"{givenName} {familyName}";
        }
        
        // If still no name, try the "name" claim (but skip if it's "unknown" or looks like email)
        if (string.IsNullOrWhiteSpace(nameClaim))
        {
            var rawName = user.FindFirst("name")?.Value ?? user.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrWhiteSpace(rawName) && 
                !rawName.Equals("unknown", StringComparison.OrdinalIgnoreCase) &&
                !rawName.Contains("@") && 
                !rawName.Contains(".onmicrosoft.com"))
            {
                nameClaim = rawName;
            }
        }
        
        // Fallback: use the local part of email as name
        if (string.IsNullOrWhiteSpace(nameClaim) || nameClaim.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            nameClaim = emailClaim.Split('@')[0];
            if (nameClaim.Length > 0)
            {
                nameClaim = char.ToUpper(nameClaim[0]) + nameClaim.Substring(1);
            }
        }
        
        if (string.IsNullOrWhiteSpace(nameClaim))
        {
            nameClaim = "Unknown";
        }
        
        // Generate username from name (remove spaces, lowercase, alphanumeric only)
        var username = nameClaim.Replace(" ", "").ToLowerInvariant();
        username = new string(username.Where(c => char.IsLetterOrDigit(c)).ToArray());
        
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "user";
        }
        
        // Handle potential username conflicts by adding numbers
        var baseUsername = username;
        var counter = 1;
        var allUsers = await userService.GetAllAsync();
        
        while (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        var createCommand = new CreateUserCommand(entraObjectId, username, emailClaim);
        var newUser = await userService.CreateAsync(createCommand);
        
        return newUser.Id;
    }
    catch (Exception)
    {
        // If user creation fails, return null to maintain existing behavior
        return null;
    }
}

// Get current user's groups only (scoped)
app.MapGet("/api/users/me/groups", async (ClaimsPrincipal user, GroupService groupService, UserService userService) =>
{
    var userId = await GetCurrentUserIdAsync(user, userService);
    if (userId == null)
    {
        return Results.Unauthorized();
    }
    
    var groups = await groupService.GetUserGroupsAsync(userId.Value);
    return Results.Ok(groups);
})
.WithName("GetCurrentUserGroups")
.RequireAuthorization();

// Keep GetAllGroups for admin purposes but mark it differently
app.MapGet("/api/groups", async (ClaimsPrincipal user, GroupService service, UserService userService) =>
{
    var userId = await GetCurrentUserIdAsync(user, userService);
    var groups = await service.GetAllAsync(userId);
    return Results.Ok(groups);
})
.WithName("GetAllGroups")
.RequireAuthorization();

app.MapGet("/api/groups/{id:guid}", async (Guid id, ClaimsPrincipal user, GroupService service, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        var group = await service.GetByIdAsync(id, userId);
        return Results.Ok(group);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
})
.WithName("GetGroupById")
.RequireAuthorization();

app.MapPost("/api/groups", async (CreateGroupCommand command, ClaimsPrincipal user, GroupService service, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        var group = await service.CreateAsync(command, userId.Value);
        return Results.Created($"/api/groups/{group.Id}", group);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateGroup")
.RequireAuthorization();

app.MapPut("/api/groups/{id:guid}", async (Guid id, UpdateGroupCommand command, ClaimsPrincipal user, GroupService service, UserService userService) =>
{
    if (id != command.Id)
    {
        return Results.BadRequest(new { error = "ID in URL does not match ID in request body" });
    }

    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        var group = await service.UpdateAsync(command, userId.Value);
        return Results.Ok(group);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateGroup")
.RequireAuthorization();

app.MapDelete("/api/groups/{id:guid}", async (Guid id, ClaimsPrincipal user, GroupService service, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        await service.DeleteAsync(id, userId.Value);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.WithName("DeleteGroup")
.RequireAuthorization();

// Group Invite endpoints
app.MapPost("/api/groups/{groupId:guid}/invites", async (Guid groupId, ClaimsPrincipal user, GroupInviteService inviteService, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        var command = new CreateInviteCommand(groupId);
        var invite = await inviteService.CreateInviteAsync(command, userId.Value);
        return Results.Created($"/api/invites/{invite.Token}", invite);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.WithName("CreateGroupInvite")
.RequireAuthorization();

app.MapGet("/api/groups/{groupId:guid}/invites", async (Guid groupId, ClaimsPrincipal user, GroupInviteService inviteService, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        var invites = await inviteService.GetGroupInvitesAsync(groupId, userId.Value);
        return Results.Ok(invites);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.WithName("GetGroupInvites")
.RequireAuthorization();

app.MapGet("/api/invites/{token}", async (string token, GroupInviteService inviteService) =>
{
    var invite = await inviteService.GetInviteByTokenAsync(token);
    if (invite == null)
    {
        return Results.NotFound(new { error = "Invalid or inactive invite link." });
    }
    return Results.Ok(invite);
})
.WithName("GetInviteByToken")
.RequireAuthorization();

app.MapPost("/api/invites/{token}/accept", async (string token, ClaimsPrincipal user, GroupInviteService inviteService, UserService userService) =>
{
    var userId = await GetCurrentUserIdAsync(user, userService);
    if (userId == null)
    {
        return Results.Unauthorized();
    }
    
    var result = await inviteService.AcceptInviteAsync(token, userId.Value);
    if (!result.Success)
    {
        return Results.BadRequest(new { error = result.ErrorMessage });
    }
    
    return Results.Ok(result);
})
.WithName("AcceptInvite")
.RequireAuthorization();

app.MapDelete("/api/invites/{id:guid}", async (Guid id, ClaimsPrincipal user, GroupInviteService inviteService, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        await inviteService.RevokeInviteAsync(id, userId.Value);
        return Results.NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
})
.WithName("RevokeInvite")
.RequireAuthorization();

// Get group members endpoint
app.MapGet("/api/groups/{groupId:guid}/members", async (Guid groupId, ClaimsPrincipal user, GroupMembershipService membershipService, UserService userService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        // Check if user has access to this group (is owner or member)
        var memberships = await membershipService.GetByGroupIdAsync(groupId);
        var userMembership = memberships.FirstOrDefault(m => m.UserId == userId.Value);
        
        if (userMembership == null)
        {
            return Results.Forbid();
        }
        
        // Return all members of the group
        var allUsers = await userService.GetAllAsync();
        var groupMembers = memberships
            .Select(m => allUsers.FirstOrDefault(u => u.Id == m.UserId))
            .Where(u => u != null)
            .ToList();
            
        return Results.Ok(groupMembers);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error getting group members: {ex.Message}");
    }
})
.WithName("GetGroupMembers")
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

// Get all workout sessions for a group (workout history)
app.MapGet("/api/groups/{groupId:guid}/workout-sessions", async (Guid groupId, ClaimsPrincipal user, WorkoutSessionService service, UserService userService, GroupMembershipService membershipService) =>
{
    try
    {
        var userId = await GetCurrentUserIdAsync(user, userService);
        if (userId == null)
        {
            return Results.Unauthorized();
        }
        
        // Check if user has access to this group (is owner or member)
        var memberships = await membershipService.GetByGroupIdAsync(groupId);
        var userMembership = memberships.FirstOrDefault(m => m.UserId == userId.Value);
        
        if (userMembership == null)
        {
            return Results.Forbid();
        }
        
        // Get all workout sessions for this group
        var sessions = await service.GetAllAsync();
        var groupSessions = sessions.Where(s => s.GroupId == groupId).OrderByDescending(s => s.SessionDate).ToList();
        
        return Results.Ok(groupSessions);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error getting group workout sessions: {ex.Message}");
    }
})
.WithName("GetGroupWorkoutSessions")
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
app.MapGet("/api/workout-interval-scores", async (WorkoutIntervalScoreService service, Guid? workoutSessionId) =>
{
    if (workoutSessionId.HasValue)
    {
        var scores = await service.GetByWorkoutSessionIdAsync(workoutSessionId.Value);
        return Results.Ok(scores);
    }
    else
    {
        var scores = await service.GetAllAsync();
        return Results.Ok(scores);
    }
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
