# Coding Agent Instructions

## Purpose
This document provides guidelines for AI coding agents working on the FamilyFitness app to ensure consistency and adherence to architectural principles.

## Core Principles

### 1. Keep It Simple (KISS)
- Don't add features, libraries, or abstractions that aren't explicitly needed
- Start with the simplest solution that works
- Add complexity only when there's a clear, demonstrated need
- Question every new dependency before adding it

### 2. Clean Architecture Boundaries
- **Domain Layer**: Pure business logic, no I/O, no frameworks
- **Application Layer**: Use cases and business workflows, depends only on Domain
- **Infrastructure Layer**: Technical implementations, depends on Application and Domain
- **API Layer**: HTTP concerns only, depends on Application and Infrastructure
- **Blazor UI**: Calls API via HTTP, no direct access to other layers

### 3. Test-Driven Development (TDD)
- Write tests **before** implementing features
- Tests should cover:
  - Happy path scenarios
  - Edge cases
  - Error conditions
- Use in-memory implementations for unit testing
- Keep tests simple and focused

## Coding Standards

### General C# Style
- Use C# 12+ features where appropriate
- Use nullable reference types (`#nullable enable`)
- Use `required` keyword for mandatory properties
- Prefer `record` types for DTOs and value objects
- Use meaningful, descriptive names
- Keep methods small and focused (single responsibility)

### Domain Layer
```csharp
// Domain entities should:
// 1. Validate invariants in constructor
// 2. Be immutable (readonly fields/properties)
// 3. Provide With* methods for updates
// 4. Throw ArgumentException for invalid data

public class WorkoutType
{
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public int? EstimatedDurationMinutes { get; }
    public Intensity Intensity { get; }

    public WorkoutType(string id, string name, string? description, 
        int? estimatedDurationMinutes, Intensity intensity)
    {
        // Validate and throw if invalid
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        // ... more validation
        
        Id = id;
        Name = name.Trim();
        Description = description?.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Intensity = intensity;
    }

    public WorkoutType WithUpdatedDetails(string name, string? description,
        int? estimatedDurationMinutes, Intensity intensity)
    {
        return new WorkoutType(Id, name, description, 
            estimatedDurationMinutes, intensity);
    }
}
```

### Application Layer
```csharp
// Use record for DTOs
public record WorkoutTypeDto(
    string Id,
    string Name,
    string? Description,
    int? EstimatedDurationMinutes,
    string Intensity
);

// Use record for Commands
public record CreateWorkoutTypeCommand(
    string Name,
    string? Description,
    int? EstimatedDurationMinutes,
    Intensity Intensity
);

// Repository interfaces define data access contracts
public interface IWorkoutTypeRepository
{
    Task<WorkoutType?> GetByIdAsync(string id);
    Task<IReadOnlyList<WorkoutType>> GetAllAsync();
    Task AddAsync(WorkoutType workoutType);
    Task UpdateAsync(WorkoutType workoutType);
    Task DeleteAsync(string id);
}

// Services orchestrate use cases
public class WorkoutTypeService
{
    private readonly IWorkoutTypeRepository _repository;
    private readonly IIdGenerator _idGenerator;

    public WorkoutTypeService(
        IWorkoutTypeRepository repository,
        IIdGenerator idGenerator)
    {
        _repository = repository;
        _idGenerator = idGenerator;
    }

    // Business logic here
}
```

### Infrastructure Layer
```csharp
// Document models represent Cosmos DB structure
public class WorkoutTypeDocument
{
    public string id { get; set; } = string.Empty;  // lowercase for Cosmos
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string Intensity { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = "WorkoutTypes";
}

// Repository implementations handle data mapping
public class CosmosWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly Container _container;
    
    // Map between Document and Domain entity
    private static WorkoutType ToEntity(WorkoutTypeDocument doc) { /*...*/ }
    private static WorkoutTypeDocument ToDocument(WorkoutType entity) { /*...*/ }
}
```

### API Layer
```csharp
// Use minimal APIs with proper HTTP status codes
app.MapGet("/api/workout-types", async (WorkoutTypeService service) =>
{
    var result = await service.GetAllAsync();
    return Results.Ok(result);
});

app.MapPost("/api/workout-types", async (
    CreateWorkoutTypeCommand command,
    WorkoutTypeService service) =>
{
    try
    {
        var result = await service.CreateAsync(command);
        return Results.Created($"/api/workout-types/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
```

### Blazor UI
```csharp
// Keep components simple and focused
@page "/workout-types"
@inject HttpClient Http

<h3>Workout Types</h3>

@if (workoutTypes == null)
{
    <p>Loading...</p>
}
else
{
    // Display list and forms
}

@code {
    private List<WorkoutTypeDto>? workoutTypes;

    protected override async Task OnInitializedAsync()
    {
        workoutTypes = await Http.GetFromJsonAsync<List<WorkoutTypeDto>>(
            "/api/workout-types");
    }
}
```

## Error Handling

### Domain Layer
- Throw `ArgumentException` for invalid constructor arguments
- Throw `ArgumentNullException` for null required parameters

### Application Layer
- Throw `InvalidOperationException` for business rule violations (e.g., duplicate name)
- Throw `KeyNotFoundException` when resource not found by ID
- Let validation exceptions bubble up from Domain layer

### API Layer
Map exceptions to HTTP status codes:
- `ArgumentException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 409 Conflict
- Unhandled exceptions → 500 Internal Server Error (let framework handle)

## Testing Guidelines

### Unit Test Structure
```csharp
public class WorkoutTypeServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_ReturnsCreatedDto()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        var idGenerator = new FixedIdGenerator("test-id");
        var service = new WorkoutTypeService(repository, idGenerator);
        var command = new CreateWorkoutTypeCommand(
            "Push-ups", "Upper body exercise", 10, Intensity.Moderate);

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Push-ups", result.Name);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        // Add existing workout type
        await repository.AddAsync(new WorkoutType(
            "id1", "Push-ups", null, null, Intensity.Moderate));
        
        var service = new WorkoutTypeService(
            repository, new FixedIdGenerator("id2"));
        var command = new CreateWorkoutTypeCommand(
            "Push-Ups", null, null, Intensity.Moderate); // Case insensitive

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(command));
    }
}
```

### In-Memory Test Implementations
```csharp
public class InMemoryWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly List<WorkoutType> _workoutTypes = new();

    public Task<WorkoutType?> GetByIdAsync(string id) =>
        Task.FromResult(_workoutTypes.FirstOrDefault(w => w.Id == id));

    public Task<IReadOnlyList<WorkoutType>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<WorkoutType>>(_workoutTypes.ToList());

    // ... other methods
}

public class FixedIdGenerator : IIdGenerator
{
    private readonly string _id;
    public FixedIdGenerator(string id) => _id = id;
    public string GenerateId() => _id;
}
```

## Cosmos DB Guidelines

### Database Configuration
- Database: `family-fitness`
- Containers use partition keys appropriate to their access patterns
- For MVP, use simple partition keys (e.g., constant value `"WorkoutTypes"`)
- Document `id` field should be lowercase (Cosmos convention)

### Container Setup
```csharp
// In repository or startup
var database = client.GetDatabase("family-fitness");
var container = database.GetContainer("workout-types");
```

### Querying
```csharp
// Use LINQ when possible
var query = _container.GetItemLinqQueryable<WorkoutTypeDocument>()
    .Where(d => d.PartitionKey == "WorkoutTypes");

// For single item reads, use ReadItemAsync for efficiency
var response = await _container.ReadItemAsync<WorkoutTypeDocument>(
    id, new PartitionKey("WorkoutTypes"));
```

## Aspire Configuration

### AppHost Setup
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator();

// Add API with Cosmos reference
var api = builder.AddProject<Projects.FamilyFitness_Api>("api")
    .WithReference(cosmos);

// Add Blazor with API reference
builder.AddProject<Projects.FamilyFitness_Blazor>("blazor")
    .WithReference(api);
```

## Common Pitfalls to Avoid

1. **Don't** add dependencies from Domain to Application or Infrastructure
2. **Don't** put business logic in API controllers/endpoints
3. **Don't** put data access code in Application layer
4. **Don't** let Blazor directly access repositories or services (use HTTP)
5. **Don't** forget to trim string inputs in Domain entities
6. **Don't** use property setters in Domain entities (use constructors and With* methods)
7. **Don't** skip validation in Domain constructors
8. **Don't** catch exceptions just to log and rethrow (let them bubble)
9. **Don't** add features "just in case" - wait for actual requirements

## When Making Changes

### Before Adding New Features
1. Check if existing code can be extended rather than creating new abstractions
2. Verify the feature is actually needed (not speculative)
3. Write tests first (TDD)
4. Keep changes minimal and focused

### Before Adding Dependencies
1. Is there a way to do this with existing dependencies?
2. Is this dependency well-maintained and trustworthy?
3. Does the benefit outweigh the cost of additional complexity?

### Code Review Checklist
- [ ] Tests written and passing
- [ ] Clean Architecture boundaries respected
- [ ] Domain entities enforce invariants
- [ ] Proper error handling with appropriate exceptions
- [ ] HTTP status codes correct in API
- [ ] No unnecessary complexity or abstractions
- [ ] Code is readable and well-named
- [ ] No hardcoded values that should be configuration

## Resources

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Azure Cosmos DB .NET SDK](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/sdk-dotnet-v3)
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
