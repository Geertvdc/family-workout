# Quick Start Guide

## Running the FamilyFitness App Locally

### Using .NET Aspire (Easiest Method)

.NET Aspire orchestrates all services automatically.

**First-time setup:**
```bash
# Install Aspire CLI globally (one-time setup)
dotnet tool install -g aspire.cli
```

**Run the application:**
```bash
# Navigate to project root
cd /path/to/family-workout

# Run with Aspire CLI
aspire run aspire/FamilyFitness.AppHost/FamilyFitness.AppHost.csproj
```

This single command will:
1. Start the Cosmos DB emulator in a Docker container (requires Docker)
2. Start the API with proper connection strings
3. Start the Blazor UI with proper API URL configuration
4. Display the Aspire dashboard URL in the console output

From the Aspire dashboard you can:
- View logs from all services
- Monitor resource usage
- Access direct links to API and Blazor UI

**Requirements**: 
- Docker must be running for the Cosmos DB emulator
- Aspire CLI installed (`dotnet tool install -g aspire.cli`)

---

### Manual Setup (Alternative Method)

If you prefer to run services individually or don't have Docker:

### 1. Start Cosmos DB Emulator

**Option A: Using Docker (Linux/Mac/Windows with WSL)**
```bash
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=2 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

**Option B: Azure Cosmos DB Emulator (Windows)**
- Download and install from: https://aka.ms/cosmosdb-emulator
- Start the emulator
- It will be available at https://localhost:8081

### 2. Create Database and Container

Using the Cosmos DB Emulator Data Explorer (https://localhost:8081/_explorer/index.html):

1. Create a new database: `family-fitness`
2. Create a new container:
   - Container ID: `workout-types`
   - Partition key: `/PartitionKey`
   - Throughput: 400 RU/s (manual)

Or using Azure Data Studio or any Cosmos DB client.

### 3. Run the API

```bash
cd src/FamilyFitness.Api
dotnet run
```

The API will start at:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5001

### 4. Run the Blazor App

In a new terminal:

```bash
cd src/FamilyFitness.Blazor
dotnet run
```

The Blazor app will start at:
- HTTPS: https://localhost:7002
- HTTP: http://localhost:5002

### 5. Access the Application

Open your browser and navigate to:
- Blazor UI: https://localhost:7002
- Navigate to "Workout Types" in the menu
- Create, view, and delete workout types

### 6. Test the API Directly

You can test the API using the Swagger UI:
- Navigate to: https://localhost:7001/openapi/v1.json
- Or use tools like Postman, curl, or the included `.http` file

Example curl commands:

```bash
# List all workout types
curl -X GET https://localhost:7001/api/workout-types -k

# Create a workout type
curl -X POST https://localhost:7001/api/workout-types -k \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Push-ups",
    "description": "Upper body strength exercise",
    "estimatedDurationMinutes": 5,
    "intensity": 1
  }'

# Get a specific workout type
curl -X GET https://localhost:7001/api/workout-types/{id} -k

# Delete a workout type
curl -X DELETE https://localhost:7001/api/workout-types/{id} -k
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/FamilyFitness.UnitTests/FamilyFitness.UnitTests.csproj

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Building the Solution

```bash
# Build all projects
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean and rebuild
dotnet clean
dotnet build
```

## Troubleshooting

### Cosmos DB Connection Issues

If you see connection errors:
1. Verify the Cosmos DB emulator is running
2. Check the connection string in `src/FamilyFitness.Api/appsettings.Development.json`
3. Make sure the database and container exist

### HTTPS Certificate Issues

If you encounter HTTPS certificate issues:
```bash
dotnet dev-certs https --trust
```

### Port Already in Use

If ports 7001 or 7002 are already in use:
1. Update `launchSettings.json` in the respective project
2. Update the API URL in Blazor's `appsettings.Development.json`

## Next Steps

1. Create more workout types
2. Explore the code structure in `.dev/coding-agents/`
3. Review the architecture documentation
4. Implement additional features (see task-workoutevents.md)
