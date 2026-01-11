using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FamilyFitness.Application;
using FamilyFitness.Blazor.Services;
using Moq;
using Moq.Protected;

namespace FamilyFitness.UnitTests;

public class UserContextServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly UserContextService _userContextService;

    public UserContextServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:7163")
        };
        _userContextService = new UserContextService(_httpClient);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenApiCallSucceeds()
    {
        // Arrange
        var expectedUser = new UserDto(
            Guid.NewGuid(),
            "test-entra-id",
            "testuser",
            "test@example.com",
            DateTime.UtcNow
        );

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedUser), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/me")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _userContextService.GetCurrentUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Id, result.Id);
        Assert.Equal(expectedUser.Username, result.Username);
        Assert.Equal(expectedUser.Email, result.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNull_WhenApiCallFails()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/me")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        var result = await _userContextService.GetCurrentUserAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserGroupsAsync_ReturnsGroups_WhenApiCallSucceeds()
    {
        // Arrange
        var expectedGroups = new List<GroupDto>
        {
            new(Guid.NewGuid(), "Family Workout", "Our family group", Guid.NewGuid(), true, DateTime.UtcNow),
            new(Guid.NewGuid(), "Friends Workout", "Friends group", Guid.NewGuid(), false, DateTime.UtcNow)
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedGroups), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/users/me/groups")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _userContextService.GetUserGroupsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedGroups[0].Name, result[0].Name);
        Assert.Equal(expectedGroups[1].Name, result[1].Name);
    }

    [Fact]
    public void SetSessionGroupId_StoresGroupId()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        // Act
        _userContextService.SetSessionGroupId(groupId);
        var result = _userContextService.GetSessionGroupId();

        // Assert
        Assert.Equal(groupId, result);
    }

    [Fact]
    public void SetSessionGroupId_AllowsNull()
    {
        // Arrange
        _userContextService.SetSessionGroupId(Guid.NewGuid());

        // Act
        _userContextService.SetSessionGroupId(null);
        var result = _userContextService.GetSessionGroupId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CanAccessGroupAsync_ReturnsTrue_WhenUserIsMemberOfGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var userGroups = new List<GroupDto>
        {
            new(groupId, "Test Group", null, Guid.NewGuid(), false, DateTime.UtcNow)
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(userGroups), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/users/me/groups")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _userContextService.CanAccessGroupAsync(groupId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessGroupAsync_ReturnsFalse_WhenUserIsNotMemberOfGroup()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var differentGroupId = Guid.NewGuid();
        var userGroups = new List<GroupDto>
        {
            new(differentGroupId, "Other Group", null, Guid.NewGuid(), false, DateTime.UtcNow)
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(userGroups), Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/users/me/groups")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(response);

        // Act
        var result = await _userContextService.CanAccessGroupAsync(groupId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetGroupMembersAsync_ReturnsMembers_WhenUserHasAccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var expectedMembers = new List<UserDto>
        {
            new(Guid.NewGuid(), "test1", "user1", "user1@example.com", DateTime.UtcNow),
            new(Guid.NewGuid(), "test2", "user2", "user2@example.com", DateTime.UtcNow)
        };

        // Mock user groups call (for access check)
        var userGroups = new List<GroupDto>
        {
            new(groupId, "Test Group", null, Guid.NewGuid(), false, DateTime.UtcNow)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/users/me/groups")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userGroups), Encoding.UTF8, "application/json")
            });

        // Mock group members call
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/api/groups/{groupId}/members")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedMembers), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _userContextService.GetGroupMembersAsync(groupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(expectedMembers[0].Username, result[0].Username);
        Assert.Equal(expectedMembers[1].Username, result[1].Username);
    }

    [Fact]
    public async Task GetGroupMembersAsync_ReturnsEmptyList_WhenUserDoesNotHaveAccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var differentGroupId = Guid.NewGuid();

        // User doesn't have access to the requested group
        var userGroups = new List<GroupDto>
        {
            new(differentGroupId, "Other Group", null, Guid.NewGuid(), false, DateTime.UtcNow)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/users/me/groups")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(userGroups), Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _userContextService.GetGroupMembersAsync(groupId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}