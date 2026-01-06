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

    public WorkoutType(string id, string name, string? description)
    {
        // Validate and throw if invalid
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        
        Id = id;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public WorkoutType WithUpdatedDetails(string name, string? description)
    {
        return new WorkoutType(Id, name, description);
    }
}
```

### Application Layer
```csharp
// Use record for DTOs
public record WorkoutTypeDto(
    string Id,
    string Name,
    string? Description
);

// Use record for Commands
public record CreateWorkoutTypeCommand(
    string Name,
    string? Description
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
// Entity configurations in DbContext using Fluent API
public class FamilyFitnessDbContext : DbContext
{
    public FamilyFitnessDbContext(DbContextOptions<FamilyFitnessDbContext> options) 
        : base(options)
    {
    }

    public DbSet<WorkoutTypeEntity> WorkoutTypes => Set<WorkoutTypeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutTypeEntity>(entity =>
        {
            entity.ToTable("workout_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }
}

// Repository implementations handle data mapping
public class PostgresWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly FamilyFitnessDbContext _context;
    
    public PostgresWorkoutTypeRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutType?> GetByIdAsync(string id)
    {
        var entity = await _context.WorkoutTypes.FindAsync(id);
        return entity == null ? null : ToEntity(entity);
    }
    
    // Map between Entity (EF Core) and Domain entity
    private static WorkoutType ToEntity(WorkoutTypeEntity entity) { /*...*/ }
    private static WorkoutTypeEntity ToDbEntity(WorkoutType domain) { /*...*/ }
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
            "Push-ups", "Upper body exercise");

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
            "id1", "Push-ups", null));
        
        var service = new WorkoutTypeService(
            repository, new FixedIdGenerator("id2"));
        var command = new CreateWorkoutTypeCommand(
            "Push-Ups", null); // Case insensitive

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

## PostgreSQL and Entity Framework Core Guidelines

### Database Configuration
- Database: `family_fitness`
- Tables use snake_case naming convention (e.g., `workout_types`, `group_memberships`)
- All entity configurations done in `FamilyFitnessDbContext` using Fluent API
- Foreign keys and constraints defined explicitly
- Indexes added for frequently queried columns

### DbContext Setup
```csharp
// In Program.cs (API project)
var connectionString = builder.Configuration.GetConnectionString("family-fitness") 
    ?? throw new InvalidOperationException("PostgreSQL connection string not found");

builder.Services.AddDbContext<FamilyFitnessDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### Entity Configuration Pattern
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.ToTable("users");
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Username)
            .HasMaxLength(100)
            .IsRequired();
        
        entity.HasIndex(e => e.Username)
            .IsUnique();
        
        // Relationships
        entity.HasMany(u => u.GroupMemberships)
            .WithOne(gm => gm.User)
            .HasForeignKey(gm => gm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

### Using Migrations
```bash
# Create a new migration
cd src/FamilyFitness.Api
dotnet ef migrations add MigrationName --project ../FamilyFitness.Infrastructure

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove --project ../FamilyFitness.Infrastructure
```

### Repository Implementation
```csharp
public class PostgresWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutTypeRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkoutType>> GetAllAsync()
    {
        var entities = await _context.WorkoutTypes.ToListAsync();
        return entities.Select(ToEntity).ToList();
    }

    public async Task AddAsync(WorkoutType workoutType)
    {
        var entity = ToDbEntity(workoutType);
        _context.WorkoutTypes.Add(entity);
        await _context.SaveChangesAsync();
    }

    // Mapping methods
    private static WorkoutType ToEntity(WorkoutTypeEntity entity) =>
        new WorkoutType(entity.Id, entity.Name, entity.Description);

    private static WorkoutTypeEntity ToDbEntity(WorkoutType domain) =>
        new WorkoutTypeEntity 
        { 
            Id = domain.Id, 
            Name = domain.Name, 
            Description = domain.Description 
        };
}
```

## Aspire Configuration

### AppHost Setup
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var postgresDb = postgres.AddDatabase("family-fitness");

// Add API with PostgreSQL reference
var api = builder.AddProject<Projects.FamilyFitness_Api>("api")
    .WithReference(postgresDb);

// Add Blazor with API reference
builder.AddProject<Projects.FamilyFitness_Blazor>("blazor")
    .WithReference(api);

builder.Build().Run();
```

### Connection String Configuration
Aspire automatically injects the connection string named `"family-fitness"` into the API project. Access it via:
```csharp
var connectionString = builder.Configuration.GetConnectionString("family-fitness");
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
