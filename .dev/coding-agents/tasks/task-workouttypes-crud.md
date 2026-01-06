# Task: Workout Types CRUD

## Objective
Implement a complete vertical slice for managing Workout Types in the FamilyFitness application, following Clean Architecture and TDD principles.

## Background
Workout Types represent different kinds of exercises that can be performed (e.g., Push-ups, Squats, Jumping Jacks). This is the first entity in the system and establishes the pattern for future entities.

## Requirements

### Domain Model: WorkoutType

Create `WorkoutType` entity in `FamilyFitness.Domain` with:

**Properties:**
- `Id` (string) - Unique identifier
- `Name` (string, required) - Name of the workout type, automatically trimmed
- `Description` (string?, optional) - Description of the workout, automatically trimmed if provided

**Invariants (enforced in constructor):**
- `Id` cannot be null or whitespace
- `Name` cannot be null or whitespace
- `Name` and `Description` are automatically trimmed

**Methods:**
- Constructor: Validates all invariants
- `WithUpdatedDetails(name, description)` - Returns new instance with updated values (immutable pattern)

**Example:**
```csharp
var workoutType = new WorkoutType(
    id: "1",
    name: "Push-ups",
    description: "Upper body strength exercise"
);

var updated = workoutType.WithUpdatedDetails(
    name: "Modified Push-ups",
    description: "Easier variation"
);
```

### Application Layer

**Interfaces (`FamilyFitness.Application`):**

```csharp
public interface IWorkoutTypeRepository
{
    Task<WorkoutType?> GetByIdAsync(string id);
    Task<IReadOnlyList<WorkoutType>> GetAllAsync();
    Task AddAsync(WorkoutType workoutType);
    Task UpdateAsync(WorkoutType workoutType);
    Task DeleteAsync(string id);
}

public interface IIdGenerator
{
    string GenerateId();
}
```

**DTOs:**

```csharp
public record WorkoutTypeDto(
    string Id,
    string Name,
    string? Description
);

public record CreateWorkoutTypeCommand(
    string Name,
    string? Description
);

public record UpdateWorkoutTypeCommand(
    string Id,
    string Name,
    string? Description
);
```

**Service (`WorkoutTypeService`):**

```csharp
public class WorkoutTypeService
{
    public async Task<WorkoutTypeDto> CreateAsync(CreateWorkoutTypeCommand command);
    public async Task<IReadOnlyList<WorkoutTypeDto>> GetAllAsync();
    public async Task<WorkoutTypeDto> GetByIdAsync(string id);
    public async Task<WorkoutTypeDto> UpdateAsync(UpdateWorkoutTypeCommand command);
    public async Task DeleteAsync(string id);
}
```

**Business Rules (in WorkoutTypeService):**
- `CreateAsync`: Name must be unique (case-insensitive comparison). Throw `InvalidOperationException` if duplicate.
- `CreateAsync`: Use `IIdGenerator` to create new ID.
- `GetByIdAsync`: Throw `KeyNotFoundException` if not found.
- `UpdateAsync`: Name must be unique unless it's the same entity. Throw `InvalidOperationException` if duplicate.
- `UpdateAsync`: Throw `KeyNotFoundException` if ID not found.
- `DeleteAsync`: Throw `KeyNotFoundException` if ID not found.

### Infrastructure Layer

**`GuidIdGenerator : IIdGenerator`**
```csharp
public class GuidIdGenerator : IIdGenerator
{
    public string GenerateId() => Guid.NewGuid().ToString();
}
```

**`WorkoutTypeEntity`** - EF Core entity representation:
```csharp
public class WorkoutTypeEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

**`FamilyFitnessDbContext`** - Entity configuration:
```csharp
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
```

**`PostgresWorkoutTypeRepository : IWorkoutTypeRepository`**
- Uses Entity Framework Core
- Database: `family_fitness`
- Table: `workout_types`
- Maps between `WorkoutType` (domain) and `WorkoutTypeEntity` (EF Core)

### API Layer

**Endpoints** (`FamilyFitness.Api`):

```
GET    /api/workout-types          → 200 OK with list of WorkoutTypeDto
GET    /api/workout-types/{id}     → 200 OK with WorkoutTypeDto | 404 Not Found
POST   /api/workout-types          → 201 Created with WorkoutTypeDto | 400 Bad Request | 409 Conflict
PUT    /api/workout-types/{id}     → 200 OK with WorkoutTypeDto | 400 Bad Request | 404 Not Found | 409 Conflict
DELETE /api/workout-types/{id}     → 204 No Content | 404 Not Found
```

**Error Handling:**
- `ArgumentException` → 400 Bad Request
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` → 409 Conflict

### Blazor UI

**Page: `/workout-types`** (`FamilyFitness.Blazor`):

Features:
1. **List View**: Display all workout types in a table
   - Show: Name, Description
   - Include Delete button for each item

2. **Create Form**: Form to create new workout type
   - Fields: Name (required), Description (optional)
   - Submit button calls `POST /api/workout-types`
   - Clear form after successful creation
   - Show error message on conflict or validation error

3. **User Feedback**:
   - Show loading indicator while fetching
   - Show success message after create/delete
   - Show error messages from API

### Testing

**Unit Tests** (`FamilyFitness.UnitTests`):

Required tests:
1. `WorkoutTypeService_CreateAsync_ValidCommand_ReturnsDto`
   - Happy path: Create workout type with valid data
   - Verify ID is generated
   - Verify data is saved to repository

2. `WorkoutTypeService_CreateAsync_DuplicateName_ThrowsInvalidOperationException`
   - Create workout type with name "Push-ups"
   - Try to create another with name "push-ups" (different case)
   - Should throw InvalidOperationException

3. `WorkoutTypeService_UpdateAsync_NonExistingId_ThrowsKeyNotFoundException`
   - Call UpdateAsync with ID that doesn't exist
   - Should throw KeyNotFoundException

**Test Helpers:**
```csharp
// In-memory repository for testing
public class InMemoryWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly List<WorkoutType> _items = new();
    // Implement interface methods using in-memory list
}

// Fixed ID generator for testing
public class FixedIdGenerator : IIdGenerator
{
    private readonly string _id;
    public FixedIdGenerator(string id) => _id = id;
    public string GenerateId() => _id;
}
```

## Implementation Steps

1. **Setup Project Structure** (if not already done)
   - Create all projects in solution
   - Add project references

2. **Domain Layer (TDD)**
   - Write tests for WorkoutType validation
   - Implement WorkoutType entity
   - Implement Intensity enum

3. **Application Layer (TDD)**
   - Create interfaces (IWorkoutTypeRepository, IIdGenerator)
   - Create DTOs and Commands
   - Write unit tests for WorkoutTypeService
   - Implement WorkoutTypeService

4. **Infrastructure Layer**
   - Implement GuidIdGenerator
   - Implement WorkoutTypeEntity
   - Configure entity in FamilyFitnessDbContext
   - Implement PostgresWorkoutTypeRepository
   - Create and apply EF Core migration

5. **API Layer**
   - Implement minimal API endpoints
   - Add error handling
   - Configure dependency injection

6. **Blazor UI**
   - Create WorkoutTypes.razor page
   - Implement list view
   - Implement create form
   - Implement delete functionality
   - Add HttpClient configuration

7. **Aspire Configuration**
   - Configure PostgreSQL resource
   - Wire API and Blazor projects
   - Set connection strings

8. **Integration Testing** (manual for now)
   - Start Aspire app
   - Verify API endpoints work via Swagger/HTTP
   - Verify Blazor UI works end-to-end

## Success Criteria

- [ ] Solution builds without errors
- [ ] All unit tests pass
- [ ] Can run application via Aspire
- [ ] API endpoints are accessible and functional
- [ ] Blazor page displays, creates, and deletes workout types
- [ ] Data persists in PostgreSQL database
- [ ] Duplicate name validation works (case-insensitive)
- [ ] Appropriate HTTP status codes returned
- [ ] Clean Architecture boundaries respected

## Non-Goals (Out of Scope)

- Edit/Update functionality in Blazor UI (API exists, but UI not required yet)
- Authentication or authorization
- Advanced validation (e.g., name length limits)
- Pagination
- Search or filtering
- Integration tests (automated)
- End-to-end tests (automated)

## Notes

- This task establishes the pattern for all future entities
- Keep implementation simple and focused
- Follow TDD: write tests first
- Respect Clean Architecture boundaries
- Use immutable patterns in domain entities
- Ensure proper error handling and status codes
