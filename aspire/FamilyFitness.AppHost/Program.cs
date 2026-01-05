var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("family-fitness");

// Add the API project with PostgreSQL reference
var api = builder.AddProject("api", "../../src/FamilyFitness.Api/FamilyFitness.Api.csproj")
    .WithReference(postgres)
    .WaitFor(postgres);

// Add the Blazor project with API reference
builder.AddProject("blazor", "../../src/FamilyFitness.Blazor/FamilyFitness.Blazor.csproj")
    .WithEnvironment("ApiUrl", api.GetEndpoint("https"));

builder.Build().Run();
