var builder = DistributedApplication.CreateBuilder(args);

// Add Cosmos DB emulator
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(container =>
    {
        container.WithLifetime(ContainerLifetime.Persistent);
    });

// Add the API project with Cosmos DB reference
var api = builder.AddProject<Projects.FamilyFitness_Api>("api")
    .WithReference(cosmos);

// Add the Blazor project with API reference
builder.AddProject<Projects.FamilyFitness_Blazor>("blazor")
    .WithEnvironment("ApiUrl", api.GetEndpoint("https"));

builder.Build().Run();
