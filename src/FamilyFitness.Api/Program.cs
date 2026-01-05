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
var connectionString = builder.Configuration.GetConnectionString("postgres") 
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IWorkoutTypeRepository, PostgresWorkoutTypeRepository>();
builder.Services.AddSingleton<IIdGenerator, GuidIdGenerator>();
builder.Services.AddScoped<WorkoutTypeService>();

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

app.Run();
