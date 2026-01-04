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
- **For x64 systems**: Docker (for Cosmos DB emulator when using Aspire)
- **For ARM Macs (Apple Silicon)**: Azure Cosmos DB account (emulator not supported on ARM)
- **For Windows**: Azure Cosmos DB Emulator
- Aspire CLI (install with: `dotnet tool install -g aspire.cli`)

## Running Locally

### Option 1: Using .NET Aspire (Recommended for x64 systems)

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
- Start the Cosmos DB emulator in a Docker container
- Start the API project with the correct connection string
- Start the Blazor project with the correct API URL
- Open the Aspire dashboard where you can monitor all services

**⚠️ Important for ARM Mac Users (Apple Silicon):**

The Cosmos DB emulator Docker image doesn't support ARM architecture. You have two options:

**Option A: Use Azure Cosmos DB Cloud Service (Recommended)**
1. Create a free Azure Cosmos DB account at https://portal.azure.com
2. Use the ARM-compatible Program.cs:
   ```bash
   cd aspire/FamilyFitness.AppHost
   # Backup original and use ARM template
   cp Program.cs Program.cs.x64
   cp Program.ARM.cs.template Program.cs
   ```
3. Update `src/FamilyFitness.Api/appsettings.Development.json` with your Azure Cosmos DB connection string:
   ```json
   {
     "ConnectionStrings": {
       "cosmos": "AccountEndpoint=https://YOUR-ACCOUNT.documents.azure.com:443/;AccountKey=YOUR-KEY;"
     }
   }
   ```
4. Run: `aspire run aspire/FamilyFitness.AppHost/FamilyFitness.AppHost.csproj`

**Option B: Run Services Manually (without Aspire)**
See "Option 2: Run Services Manually" below and use Azure Cosmos DB connection string.

**Note**: Requires Docker to be running for x64 systems with Cosmos DB emulator.

### Option 2: Run Services Manually

If you prefer to run services individually or are on ARM Mac:

```bash
# For ARM Mac: Use Azure Cosmos DB cloud service
# Configure connection string in src/FamilyFitness.Api/appsettings.Development.json

# For x64 systems with Docker:
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# For Windows: Azure Cosmos DB Emulator
# Install and start from https://aka.ms/cosmosdb-emulator

# Run the API
cd src/FamilyFitness.Api
dotnet run

# In another terminal, run the Blazor app
cd src/FamilyFitness.Blazor
dotnet run
```

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

When using .NET Aspire, the Cosmos DB emulator is automatically configured. Otherwise, on first run you'll need to create the Cosmos DB database and container:

1. Connect to your Cosmos DB emulator or instance
2. Create a database named `family-fitness`
3. Create a container named `workout-types` with partition key `/PartitionKey`

Or use the Data Explorer in the Cosmos DB Emulator UI.

## Development Principles

- **Test-Driven Development (TDD)**: Write tests first, then implementation
- **Clean Architecture**: Respect layer boundaries and dependencies
- **Keep It Simple**: Don't add complexity until needed
- **Immutable Domain Entities**: Use constructor validation and `With*` methods for updates
- **Local Development with Aspire**: Use .NET Aspire CLI for integrated local development experience

## Aspire Setup Notes

In .NET 10, Aspire moved from a workload to standalone NuGet packages and CLI tools:
- The project uses `Aspire.AppHost.Sdk/13.1.0` for the AppHost
- Requires `dotnet tool install -g aspire.cli` to run
- Use `aspire run` command instead of `dotnet run` for the AppHost
- Dashboard and DCP (Developer Control Plane) binaries are automatically downloaded as NuGet packages

## Contributing

See `.dev/coding-agents/` for architectural guidelines and coding standards.
