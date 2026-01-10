using FamilyFitness.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyFitness.IntegrationTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FamilyFitnessDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add InMemory database for testing
            services.AddDbContext<FamilyFitnessDbContext>(options =>
            {
                options.UseInMemoryDatabase("FamilyFitnessTesting");
            });

            // Replace authentication with test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });

            // Override policy evaluator to bypass authorization for testing
            services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FamilyFitnessDbContext>();
            context.Database.EnsureDeleted();
        }
        base.Dispose(disposing);
    }
}