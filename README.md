# FamilyFitness App

A family workout tracking application built with .NET 10, Blazor, and Azure Cosmos DB.

## Architecture

This project follows Clean Architecture principles with the following layers:

- **Domain** (`FamilyFitness.Domain`): Core business entities and domain logic
- **Application** (`FamilyFitness.Application`): Use cases, business workflows, and repository interfaces
- **Infrastructure** (`FamilyFitness.Infrastructure`): Technical implementations (Cosmos DB, etc.)
- **API** (`FamilyFitness.Api`): RESTful HTTP endpoints
- **Blazor** (`FamilyFitness.Blazor`): User interface

## Prerequisites

- .NET 10 SDK
- Azure Cosmos DB Emulator (for local development)

## Running Locally

### Option 1: Using Docker Compose (Recommended)

```bash
# Start Cosmos DB emulator
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# Run the API
cd src/FamilyFitness.Api
dotnet run

# In another terminal, run the Blazor app
cd src/FamilyFitness.Blazor
dotnet run
```

### Option 2: Using Azure Cosmos DB Emulator (Windows)

1. Install and start the [Azure Cosmos DB Emulator](https://aka.ms/cosmosdb-emulator)
2. Update connection strings in `appsettings.Development.json` if needed
3. Run the API and Blazor projects as shown above

### Connection String Configuration

The API expects a Cosmos DB connection string named "cosmos" in configuration. For local development with the emulator:

```json
{
  "ConnectionStrings": {
    "cosmos": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
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
│   └── FamilyFitness.AppHost/  # Aspire orchestration (currently blocked by SDK deprecation)
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

The API runs on `https://localhost:7001` (or configured port) and provides:

- `GET /api/workout-types` - List all workout types
- `GET /api/workout-types/{id}` - Get a specific workout type
- `POST /api/workout-types` - Create a new workout type
- `PUT /api/workout-types/{id}` - Update a workout type
- `DELETE /api/workout-types/{id}` - Delete a workout type

## Blazor UI

The Blazor app runs on `https://localhost:7002` (or configured port) and provides:

- `/workout-types` - Manage workout types (list, create, delete)

## Database Setup

On first run, you'll need to create the Cosmos DB database and container:

1. Connect to your Cosmos DB emulator or instance
2. Create a database named `family-fitness`
3. Create a container named `workout-types` with partition key `/PartitionKey`

Or use the Data Explorer in the Cosmos DB Emulator UI.

## Known Issues

### Aspire AppHost

The Aspire AppHost project (`aspire/FamilyFitness.AppHost`) is currently not functional due to .NET SDK 10.0 deprecating the Aspire workload. The Aspire team has moved to NuGet package-based distribution, but the project template and tooling are still in transition.

**Workaround**: Run the API and Blazor projects separately as shown in the "Running Locally" section above.

## Development Principles

- **Test-Driven Development (TDD)**: Write tests first, then implementation
- **Clean Architecture**: Respect layer boundaries and dependencies
- **Keep It Simple**: Don't add complexity until needed
- **Immutable Domain Entities**: Use constructor validation and `With*` methods for updates

## Contributing

See `.dev/coding-agents/` for architectural guidelines and coding standards.
