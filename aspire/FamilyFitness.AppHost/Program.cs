var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB emulator
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(container =>
    {
        container.WithLifetime(ContainerLifetime.Persistent);
    });

// Add the API project with Cosmos DB reference
var api = builder.AddProject("api", "../../src/FamilyFitness.Api/FamilyFitness.Api.csproj")
    .WithReference(cosmos);

// Add the Blazor project with API reference
builder.AddProject("blazor", "../../src/FamilyFitness.Blazor/FamilyFitness.Blazor.csproj")
    .WithEnvironment("ApiUrl", api.GetEndpoint("https"));

builder.Build().Run();
