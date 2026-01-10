# Quick Start Guide

## Running the FamilyFitness App Locally

**Important**: The FamilyFitness app requires Azure Entra External ID authentication. Follow the **Authentication Setup** section below before running the application.

### Authentication Setup (Required)

The app uses Azure Entra External ID (CIAM) for authentication. You need to configure user secrets for the Blazor app:

1. **Configure Blazor client secret**:
   ```bash
   cd src/FamilyFitness.Blazor
   dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET_FROM_AZURE"
   ```

2. **Verify configuration**: The following settings are already configured:
   - **Blazor Client ID**: `3d9bde47-ee26-443f-9593-1ebb936982b2`
   - **API Client ID**: `2b8a282a-98b0-4162-9553-4c5b8882bdcc` 
   - **Authority**: `https://ffworkoutoftheday.ciamlogin.com/12094d72-73f9-4374-8d1e-8181315429a1/v2.0`
   - **Redirect URI**: `https://localhost:7002/signin-oidc`
   - **API Scope**: `api://2b8a282a-98b0-4162-9553-4c5b8882bdcc/user_access`

3. **Test sign-in**: Once running, navigate to https://localhost:7002 and click "Sign in". You can sign in with Google or create a new account.

### Using .NET Aspire (Recommended)

.NET Aspire orchestrates all services automatically and works on all platforms.

**First-time setup:**
```bash
# Install Aspire CLI globally (one-time setup)
dotnet tool install -g aspire.cli
```

**Run the application:**
```bash
# Navigate to project root
cd /path/to/family-workout

# Configure authentication (see above)
cd src/FamilyFitness.Blazor
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"

# Return to project root and run
cd ../..
aspire run aspire/FamilyFitness.AppHost/FamilyFitness.AppHost.csproj
```

This single command will:
1. Start PostgreSQL in a Docker container (requires Docker)
2. Start the API with proper connection strings and JWT authentication
3. Start the Blazor UI with OIDC authentication
4. Display the Aspire dashboard URL in the console output

From the Aspire dashboard you can:
- View logs from all services
- Monitor resource usage
- Access direct links to API and Blazor UI

**Requirements**: 
- Docker must be running
- Aspire CLI installed (`dotnet tool install -g aspire.cli`)
- Azure Entra client secret configured (see Authentication Setup above)

**Note**: PostgreSQL works on all platforms including ARM Macs (Apple Silicon), Intel Macs, Windows, and Linux.

---

### Manual Setup (Alternative Method)

If you prefer to run services individually:

**Prerequisites**: Complete the **Authentication Setup** section above first.

### 1. Start PostgreSQL

**Using Docker:**
```bash
docker run -d \
  --name family-fitness-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=family_fitness \
  -p 5432:5432 \
  postgres:17
```

**Using locally installed PostgreSQL:**
- Ensure PostgreSQL is running
- Create a database named `family_fitness`
- Update connection string in `src/FamilyFitness.Api/appsettings.Development.json` if needed

### 2. Run the API

```bash
cd src/FamilyFitness.Api
dotnet run
```

The API will start at:
- HTTPS: https://localhost:7163 (configured for this auth setup)
- HTTP: http://localhost:5163

**Note**: All API endpoints now require authentication. Use the Blazor UI to authenticate first.

### 3. Run the Blazor App

In a new terminal:

```bash
cd src/FamilyFitness.Blazor

# Ensure client secret is configured
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"

dotnet run
```

The Blazor app will start at:
- HTTPS: https://localhost:7002
- HTTP: http://localhost:5002

### 4. Access the Application

1. **Open your browser** and navigate to: https://localhost:7002
2. **Sign in** using the "Sign in" link in the navigation menu
3. **Authenticate** with Google or create a new account via Azure Entra External ID
4. **Explore the app** - all API endpoints are now protected and require authentication

### 5. Test the API Directly

The API now requires authentication. After signing in through the Blazor app, you can test the API:

- **Get current user**: https://localhost:7163/api/me
- **OpenAPI spec**: https://localhost:7163/openapi/v1.json (in Development)

**Note**: Direct API access requires a valid JWT token from Azure Entra External ID.

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
