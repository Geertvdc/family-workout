using System.Net;

namespace FamilyFitness.IntegrationTests;

public class AuthTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWorkoutTypes_WithAuthentication_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/workout-types");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAuthentication_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("testuser@example.com", content);
    }

    [Fact]
    public async Task GetUsers_WithAuthentication_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}