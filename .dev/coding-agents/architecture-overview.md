# FamilyFitness App - Architecture Overview

## Technology Stack

- **Backend**: .NET 10 (API + Business Layer)
- **Frontend**: Blazor
- **Architecture Pattern**: Clean Architecture
- **Development Approach**: Test-Driven Development (TDD)
- **API Style**: REST
- **Database**: PostgreSQL with Entity Framework Core
- **Local Development**: .NET Aspire
- **Design Principle**: Keep it simple - don't add anything we don't need

## Clean Architecture Layers

### 1. Domain Layer (`FamilyFitness.Domain`)
**Responsibility**: Core business entities and domain logic

- Contains enterprise-wide business rules and entities
- No dependencies on other layers or external frameworks
- Entities enforce their own invariants through constructors and methods
- Immutable-style updates using `With*` methods
- **No I/O operations** - pure domain logic only

**Example Entities**:
- `WorkoutType`: Represents a type of workout exercise

### 2. Application Layer (`FamilyFitness.Application`)
**Responsibility**: Application-specific business logic and use cases

- Orchestrates domain entities to fulfill use cases
- Defines repository interfaces (abstractions)
- Defines service interfaces (abstractions)
- Contains DTOs, Commands, and Queries
- Implements application services
- Depends only on Domain layer
- **No infrastructure concerns** (no database, no HTTP)

**Key Components**:
- **Services**: Business logic orchestration (e.g., `WorkoutTypeService`)
- **Repository Interfaces**: Data access contracts (e.g., `IWorkoutTypeRepository`)
- **DTOs/Commands**: Data transfer objects for API/UI communication
- **Service Interfaces**: Utility abstractions (e.g., `IIdGenerator`)

### 3. Infrastructure Layer (`FamilyFitness.Infrastructure`)
**Responsibility**: External concerns and technical implementations

- Implements repository interfaces defined in Application layer
- Database access (PostgreSQL via Entity Framework Core)
- External services
- File system access
- Depends on Application and Domain layers

**Key Components**:
- `PostgresWorkoutTypeRepository`: PostgreSQL implementation of `IWorkoutTypeRepository`
- `GuidIdGenerator`: GUID-based implementation of `IIdGenerator`
- `FamilyFitnessDbContext`: EF Core DbContext for database configuration
- Entity configurations using Fluent API
- Database migrations for schema management

### 4. API Layer (`FamilyFitness.Api`)
**Responsibility**: RESTful HTTP endpoints

- Minimal API endpoints
- Request/response handling
- HTTP status code mapping
- Depends on Application and Infrastructure layers
- Uses Dependency Injection to get Application services

**Endpoints Pattern**:
```
GET    /api/{resource}      - List all
GET    /api/{resource}/{id} - Get by ID
POST   /api/{resource}      - Create
PUT    /api/{resource}/{id} - Update
DELETE /api/{resource}/{id} - Delete
```

**HTTP Status Codes**:
- `200 OK`: Successful GET/PUT
- `201 Created`: Successful POST
- `204 No Content`: Successful DELETE
- `400 Bad Request`: Validation errors
- `404 Not Found`: Resource not found
- `409 Conflict`: Business rule violation (e.g., duplicate name)

### 5. Blazor Layer (`FamilyFitness.Blazor`)
**Responsibility**: User interface

- Razor components and pages
- Calls API layer via HTTP
- No direct access to Application or Infrastructure layers
- Simple, functional UI focused on user tasks

### 6. Aspire AppHost (`FamilyFitness.AppHost`)
**Responsibility**: Local development orchestration

- Configures and orchestrates all services for local development
- Provisions PostgreSQL database container
- Manages service-to-service communication
- Provides connection strings and configuration

## Data Flow

```
User Request
    ↓
Blazor UI (HTTP Client)
    ↓
API Layer (Minimal APIs)
    ↓
Application Layer (Services)
    ↓
Domain Layer (Entities with Business Rules)
    ↓
Application Layer (Repository Interface)
    ↓
Infrastructure Layer (EF Core Repository Implementation)
    ↓
PostgreSQL Database
```

## PostgreSQL Database Design

### Database Structure
- **Database Name**: `family_fitness`
- **Tables**:
  - `workout_types`: Stores workout type definitions
  - `users`: User accounts
  - `groups`: Family/group management
  - `group_memberships`: User-to-group relationships with roles
  - `workout_sessions`: Scheduled workout events
  - `workout_session_workout_types`: Exercise assignments to session stations (4 per session)
  - `workout_session_participants`: User participation tracking
  - `workout_interval_scores`: Performance scores (3 rounds × 4 stations)

### Entity Framework Core Pattern
Infrastructure layer uses:
- **DbContext** (`FamilyFitnessDbContext`): Central database configuration
- **Fluent API**: Entity configuration (table names, keys, relationships, constraints)
- **Migrations**: Schema versioning and deployment
- **Repository Pattern**: Data access abstraction over EF Core

### Key Database Features
- **Foreign Keys**: Enforced referential integrity between entities
- **Unique Constraints**: Prevent duplicate usernames, emails, group memberships
- **Check Constraints**: Validate ranges (e.g., StationIndex 1-4, RoundNumber 1-3)
- **Indexes**: Optimized queries on frequently accessed columns
- **Cascade Deletes**: Automatic cleanup of related records

## Testing Strategy

### Unit Tests (`FamilyFitness.UnitTests`)
- Test Application layer services in isolation
- Use in-memory implementations of interfaces
- Fast, no external dependencies
- Focus on business logic and edge cases

### Integration Tests (`FamilyFitness.IntegrationTests`)
- Test API endpoints with real infrastructure
- Use PostgreSQL test database or container
- Test full request/response cycles
- Verify database constraints and relationships

### End-to-End Tests (`FamilyFitness.EndToEndTests`)
- Test complete user workflows
- May include Blazor UI testing
- Test system behavior from user perspective

## Key Design Decisions

1. **Immutable Entities**: Domain entities use `With*` methods for updates instead of property setters
2. **PostgreSQL + EF Core**: Relational database for strong consistency, ACID transactions, and familiar SQL patterns
3. **Minimal APIs**: Using .NET minimal APIs for simplicity
4. **TDD Approach**: Write tests first, then implementation
5. **No Over-Engineering**: Start simple, add complexity only when needed
6. **Snake Case Tables**: Using PostgreSQL naming convention (e.g., `workout_types`)
7. **Fluent API Configuration**: Entity configuration in DbContext rather than attributes

## Dependency Rules

- Domain layer has **no dependencies** on any other layer
- Application layer depends only on **Domain**
- Infrastructure layer depends on **Application** and **Domain**
- API layer depends on **Application** and **Infrastructure**
- Blazor layer depends on **nothing** (calls API via HTTP)
- Tests can depend on layers they test

## Future Considerations

- Add caching layer if needed
- Add authentication/authorization
- Add logging and monitoring
- Add more sophisticated error handling
- Consider CQRS pattern for complex queries
- Optimize database indexes based on query patterns
- Consider read replicas for scaling
