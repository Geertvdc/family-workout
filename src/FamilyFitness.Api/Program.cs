using FamilyFitness.Application;
using FamilyFitness.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

// Register PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("family-fitness") 
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
    options.UseNpgsql(connectionString));

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

var app = builder.Build();

// Ensure database is created (for development)
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
            break;
        }
        catch (Npgsql.NpgsqlException) when (i < maxRetries - 1)
        {
            app.Logger.LogInformation("Waiting for PostgreSQL to be ready... (Attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
            await Task.Delay(delay);
        }
    }
}

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
.WithName("GetAllWorkoutTypes");

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
.WithName("GetWorkoutTypeById");

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
.WithName("CreateWorkoutType");

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
.WithName("UpdateWorkoutType");

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
.WithName("DeleteWorkoutType");

// User endpoints
app.MapGet("/api/users", async (UserService service) =>
{
    var users = await service.GetAllAsync();
    return Results.Ok(users);
})
.WithName("GetAllUsers");

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
.WithName("GetUserById");

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
.WithName("CreateUser");

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
.WithName("UpdateUser");

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
.WithName("DeleteUser");

// Group endpoints
app.MapGet("/api/groups", async (GroupService service) =>
{
    var groups = await service.GetAllAsync();
    return Results.Ok(groups);
})
.WithName("GetAllGroups");

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
.WithName("GetGroupById");

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
.WithName("CreateGroup");

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
.WithName("UpdateGroup");

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
.WithName("DeleteGroup");

// GroupMembership endpoints
app.MapGet("/api/group-memberships", async (GroupMembershipService service) =>
{
    var memberships = await service.GetAllAsync();
    return Results.Ok(memberships);
})
.WithName("GetAllGroupMemberships");

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
.WithName("GetGroupMembershipById");

app.MapGet("/api/groups/{groupId:guid}/memberships", async (Guid groupId, GroupMembershipService service) =>
{
    var memberships = await service.GetByGroupIdAsync(groupId);
    return Results.Ok(memberships);
})
.WithName("GetGroupMembershipsByGroupId");

app.MapGet("/api/users/{userId:guid}/memberships", async (Guid userId, GroupMembershipService service) =>
{
    var memberships = await service.GetByUserIdAsync(userId);
    return Results.Ok(memberships);
})
.WithName("GetGroupMembershipsByUserId");

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
.WithName("CreateGroupMembership");

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
.WithName("UpdateGroupMembership");

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
.WithName("DeleteGroupMembership");

// WorkoutSession endpoints
app.MapGet("/api/workout-sessions", async (WorkoutSessionService service) =>
{
    var sessions = await service.GetAllAsync();
    return Results.Ok(sessions);
})
.WithName("GetAllWorkoutSessions");

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
.WithName("GetWorkoutSessionById");

app.MapGet("/api/groups/{groupId:guid}/workout-sessions", async (Guid groupId, WorkoutSessionService service) =>
{
    var sessions = await service.GetByGroupIdAsync(groupId);
    return Results.Ok(sessions);
})
.WithName("GetWorkoutSessionsByGroupId");

app.MapGet("/api/users/{creatorId:guid}/workout-sessions", async (Guid creatorId, WorkoutSessionService service) =>
{
    var sessions = await service.GetByCreatorIdAsync(creatorId);
    return Results.Ok(sessions);
})
.WithName("GetWorkoutSessionsByCreatorId");

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
.WithName("CreateWorkoutSession");

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
.WithName("UpdateWorkoutSession");

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
.WithName("DeleteWorkoutSession");

// WorkoutSessionWorkoutType endpoints
app.MapGet("/api/workout-session-workout-types", async (WorkoutSessionWorkoutTypeService service) =>
{
    var items = await service.GetAllAsync();
    return Results.Ok(items);
})
.WithName("GetAllWorkoutSessionWorkoutTypes");

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
.WithName("GetWorkoutSessionWorkoutTypeById");

app.MapGet("/api/workout-sessions/{workoutSessionId:guid}/workout-types", async (Guid workoutSessionId, WorkoutSessionWorkoutTypeService service) =>
{
    var items = await service.GetByWorkoutSessionIdAsync(workoutSessionId);
    return Results.Ok(items);
})
.WithName("GetWorkoutSessionWorkoutTypesBySessionId");

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
.WithName("CreateWorkoutSessionWorkoutType");

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
.WithName("UpdateWorkoutSessionWorkoutType");

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
.WithName("DeleteWorkoutSessionWorkoutType");

// WorkoutSessionParticipant endpoints
app.MapGet("/api/workout-session-participants", async (WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetAllAsync();
    return Results.Ok(participants);
})
.WithName("GetAllWorkoutSessionParticipants");

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
.WithName("GetWorkoutSessionParticipantById");

app.MapGet("/api/workout-sessions/{workoutSessionId:guid}/participants", async (Guid workoutSessionId, WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetByWorkoutSessionIdAsync(workoutSessionId);
    return Results.Ok(participants);
})
.WithName("GetWorkoutSessionParticipantsBySessionId");

app.MapGet("/api/users/{userId:guid}/participations", async (Guid userId, WorkoutSessionParticipantService service) =>
{
    var participants = await service.GetByUserIdAsync(userId);
    return Results.Ok(participants);
})
.WithName("GetWorkoutSessionParticipantsByUserId");

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
.WithName("CreateWorkoutSessionParticipant");

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
.WithName("UpdateWorkoutSessionParticipant");

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
.WithName("DeleteWorkoutSessionParticipant");

// WorkoutIntervalScore endpoints
app.MapGet("/api/workout-interval-scores", async (WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetAllAsync();
    return Results.Ok(scores);
})
.WithName("GetAllWorkoutIntervalScores");

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
.WithName("GetWorkoutIntervalScoreById");

app.MapGet("/api/workout-session-participants/{participantId:guid}/scores", async (Guid participantId, WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetByParticipantIdAsync(participantId);
    return Results.Ok(scores);
})
.WithName("GetWorkoutIntervalScoresByParticipantId");

app.MapGet("/api/workout-types/{workoutTypeId}/scores", async (string workoutTypeId, WorkoutIntervalScoreService service) =>
{
    var scores = await service.GetByWorkoutTypeIdAsync(workoutTypeId);
    return Results.Ok(scores);
})
.WithName("GetWorkoutIntervalScoresByWorkoutTypeId");

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
.WithName("CreateWorkoutIntervalScore");

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
.WithName("UpdateWorkoutIntervalScore");

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
.WithName("DeleteWorkoutIntervalScore");

app.Run();
