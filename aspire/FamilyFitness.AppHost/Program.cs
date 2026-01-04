var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB emulator (Note: Cosmos DB emulator doesn't support ARM/Apple Silicon)
// For ARM Macs, use Azure Cosmos DB cloud service or configure a connection string in API's appsettings
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(container =>
    {
        container.WithLifetime(ContainerLifetime.Persistent);
        // Note: The emulator image is x64 only. ARM Mac users should:
        // 1. Use Azure Cosmos DB cloud service, OR
        // 2. Comment out this emulator and configure connection string in API appsettings.Development.json
    });

// Add the API project with Cosmos DB reference
var api = builder.AddProject("api", "../../src/FamilyFitness.Api/FamilyFitness.Api.csproj")
    .WithReference(cosmos);

// Add the Blazor project with API reference
builder.AddProject("blazor", "../../src/FamilyFitness.Blazor/FamilyFitness.Blazor.csproj")
    .WithEnvironment("ApiUrl", api.GetEndpoint("https"));

builder.Build().Run();
