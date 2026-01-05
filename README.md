# FamilyFitness App

A family workout tracking application built with .NET 10, Blazor, and PostgreSQL.

## Architecture

This project follows Clean Architecture principles with the following layers:

- **Domain** (`FamilyFitness.Domain`): Core business entities and domain logic
- **Application** (`FamilyFitness.Application`): Use cases, business workflows, and repository interfaces
- **Infrastructure** (`FamilyFitness.Infrastructure`): Technical implementations (PostgreSQL with Entity Framework Core, etc.)
- **API** (`FamilyFitness.Api`): RESTful HTTP endpoints
- **Blazor** (`FamilyFitness.Blazor`): User interface

## Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL when using Aspire)
- Or PostgreSQL installed locally
- Aspire CLI (install with: `dotnet tool install -g aspire.cli`)

## Running Locally

### Option 1: Using .NET Aspire (Recommended)

.NET Aspire provides integrated orchestration for the entire application stack.

**First-time setup:**
```bash
# Install Aspire CLI globally
dotnet tool install -g aspire.cli

# Navigate to the project root
cd /path/to/family-workout
```

**Run the application:**
```bash
# Use aspire CLI to run the AppHost
aspire run aspire/FamilyFitness.AppHost/FamilyFitness.AppHost.csproj
```

This will:
- Start PostgreSQL in a Docker container
- Start the API project with the correct connection string
- Start the Blazor project with the correct API URL
- Open the Aspire dashboard where you can monitor all services

**Note**: Requires Docker to be running.

### Option 2: Run Services Manually

If you prefer to run services individually:

```bash
# Start PostgreSQL with Docker:
docker run -d \
  --name family-fitness-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=family_fitness \
  -p 5432:5432 \
  postgres:17

# Or use locally installed PostgreSQL
# Make sure a database named 'family_fitness' exists

# Run the API
cd src/FamilyFitness.Api
dotnet run

# In another terminal, run the Blazor app
cd src/FamilyFitness.Blazor
dotnet run
```

### Connection String Configuration

The API expects a PostgreSQL connection string named "postgres" in configuration. For local development:

```json
{
  "ConnectionStrings": {
    "postgres": "Host=localhost;Port=5432;Database=family_fitness;Username=postgres;Password=postgres"
  }
}
```

## Project Structure

```
family-workout/
├── .dev/
│   └── coding-agents/          # Documentation for AI coding agents
│       ├── architecture-overview.md
│       ├── coding-agent-instructions.md
│       └── tasks/
├── src/
│   ├── FamilyFitness.Domain/
│   ├── FamilyFitness.Application/
│   ├── FamilyFitness.Infrastructure/
│   ├── FamilyFitness.Api/
│   └── FamilyFitness.Blazor/
├── aspire/
│   └── FamilyFitness.AppHost/  # Aspire orchestration (requires Aspire CLI: dotnet tool install -g aspire.cli)
└── tests/
    ├── FamilyFitness.UnitTests/
    ├── FamilyFitness.IntegrationTests/
    └── FamilyFitness.EndToEndTests/
```

## Building and Testing

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/FamilyFitness.UnitTests/FamilyFitness.UnitTests.csproj
```

## API Endpoints

The API runs on `https://localhost:7001` (or configured port) and provides complete CRUD operations for all domain entities:

### Core Entities
- **Users** - `/api/users` - User management (username, email)
- **Groups** - `/api/groups` - Group/family management
- **Group Memberships** - `/api/group-memberships` - User-to-group relationships with roles

### Workout Management
- **Workout Types** - `/api/workout-types` - Exercise type definitions (e.g., Pushups, Situps)
- **Workout Sessions** - `/api/workout-sessions` - Scheduled workout events
- **Session Workout Types** - `/api/workout-session-workout-types` - 4 stations per session
- **Session Participants** - `/api/workout-session-participants` - User participation tracking
- **Interval Scores** - `/api/workout-interval-scores` - Performance scores (3 rounds × 4 stations)

### API Features
- Full CRUD operations (Create, Read, Update, Delete)
- Nested endpoints for related data (e.g., `/api/groups/{id}/memberships`)
- Consistent error handling with descriptive messages
- Input validation at service layer
- **Note**: No authentication required (will be added in future iteration)

For complete API documentation, see [docs/api-endpoints.md](docs/api-endpoints.md).

## Blazor UI

The Blazor app runs on `https://localhost:7002` (or configured port) and provides simple CRUD interfaces for testing:

### Available Pages
- `/users` - User management
- `/groups` - Group management
- `/group-memberships` - Manage user-group relationships
- `/workout-types` - Manage workout/exercise types
- `/workout-sessions` - Create and manage workout sessions
- `/workout-session-workout-types` - Assign exercises to session stations
- `/workout-session-participants` - Add participants to sessions
- `/workout-interval-scores` - Record performance scores

All pages follow a consistent pattern:
- Create new entities with forms
- View all entities in tables
- Delete entities with confirmation
- Real-time feedback for operations

**Note**: The UI is intentionally simple for testing purposes. UI polish and user experience improvements will be added in future iterations.

## Database Setup

When using .NET Aspire, PostgreSQL is automatically configured and the database schema is created. 

When running manually, the API will automatically create the database schema on first run using Entity Framework Core's `EnsureCreatedAsync()` in development mode.

For production environments, use EF Core migrations:
```bash
cd src/FamilyFitness.Api
dotnet ef migrations add InitialCreate --project ../FamilyFitness.Infrastructure
dotnet ef database update
```

## Development Principles

- **Test-Driven Development (TDD)**: Write tests first, then implementation
- **Clean Architecture**: Respect layer boundaries and dependencies
- **Keep It Simple**: Don't add complexity until needed
- **Immutable Domain Entities**: Use constructor validation and `With*` methods for updates
- **Local Development with Aspire**: Use .NET Aspire CLI for integrated local development experience
- **Cross-Platform**: PostgreSQL works on all platforms (Windows, macOS including ARM, Linux)

## Aspire Setup Notes

In .NET 10, Aspire moved from a workload to standalone NuGet packages and CLI tools:
- The project uses `Aspire.AppHost.Sdk/13.1.0` for the AppHost
- Requires `dotnet tool install -g aspire.cli` to run
- Use `aspire run` command instead of `dotnet run` for the AppHost
- Dashboard and DCP (Developer Control Plane) binaries are automatically downloaded as NuGet packages
- PostgreSQL container works on all architectures (x64 and ARM)

## Contributing

See `.dev/coding-agents/` for architectural guidelines and coding standards.
