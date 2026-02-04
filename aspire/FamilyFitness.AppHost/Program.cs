using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL: by default we run a local container.
// If Postgres:UseExternal (or Postgres:UseAzureAdToken) is enabled, we instead use the
// externally provided ConnectionStrings:family-fitness (e.g. Azure Database for PostgreSQL).
var useExternalPostgres = builder.Configuration.GetValue<bool?>("Postgres:UseExternal")
    ?? builder.Configuration.GetValue<bool?>("Postgres:UseAzureAdToken")
    ?? false;

var postgresDb = useExternalPostgres
    ? builder.AddConnectionString("family-fitness")
    : builder.AddPostgres("postgres")
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("family-fitness");

// Add the API project with PostgreSQL reference
var api = builder.AddProject("api", "../../src/FamilyFitness.Api/FamilyFitness.Api.csproj")
    .WithReference(postgresDb)
    .WaitFor(postgresDb);

// Add the Blazor project with API reference
builder.AddProject("blazor", "../../src/FamilyFitness.Blazor/FamilyFitness.Blazor.csproj")
    .WithEnvironment("ApiUrl", api.GetEndpoint("https"));

builder.Build().Run();
