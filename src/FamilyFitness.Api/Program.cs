using FamilyFitness.Application;
using FamilyFitness.Infrastructure;
using Microsoft.Azure.Cosmos;

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

// Register Cosmos DB
var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos") 
    ?? throw new InvalidOperationException("Cosmos connection string not found");
var cosmosClient = new CosmosClient(cosmosConnectionString);
var database = cosmosClient.GetDatabase("family-fitness");
var workoutTypesContainer = database.GetContainer("workout-types");

builder.Services.AddSingleton(workoutTypesContainer);
builder.Services.AddSingleton<IWorkoutTypeRepository>(sp => 
    new CosmosWorkoutTypeRepository(sp.GetRequiredService<Container>()));
builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();
builder.Services.AddScoped<WorkoutTypeService>();

var app = builder.Build();

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

app.Run();
