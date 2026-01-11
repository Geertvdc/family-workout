using System.Net.Http.Json;
using FamilyFitness.Application;

namespace FamilyFitness.Blazor.Services;

/// <summary>
/// Implementation of IUserContextService that manages user context and group selection
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly HttpClient _httpClient;
    private Guid? _sessionGroupId; // In-memory only, not persisted across browser restarts
    private UserDto? _cachedCurrentUser;
    private List<GroupDto>? _cachedUserGroups;

    public UserContextService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        if (_cachedCurrentUser != null)
            return _cachedCurrentUser;

        try
        {
            _cachedCurrentUser = await _httpClient.GetFromJsonAsync<UserDto>("/api/me");
            return _cachedCurrentUser;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetUserGroupsAsync()
    {
        if (_cachedUserGroups != null)
            return _cachedUserGroups;

        try
        {
            _cachedUserGroups = await _httpClient.GetFromJsonAsync<List<GroupDto>>("/api/users/me/groups") ?? new List<GroupDto>();
            return _cachedUserGroups;
        }
        catch (HttpRequestException)
        {
            return new List<GroupDto>();
        }
    }

    /// <inheritdoc />
    public void SetSessionGroupId(Guid? groupId)
    {
        _sessionGroupId = groupId;
    }

    /// <inheritdoc />
    public Guid? GetSessionGroupId()
    {
        return _sessionGroupId;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessGroupAsync(Guid groupId)
    {
        var userGroups = await GetUserGroupsAsync();
        return userGroups.Any(g => g.Id == groupId);
    }

    /// <inheritdoc />
    public async Task<List<UserDto>> GetGroupMembersAsync(Guid groupId)
    {
        // First validate user has access to this group
        if (!await CanAccessGroupAsync(groupId))
        {
            return new List<UserDto>();
        }

        try
        {
            var members = await _httpClient.GetFromJsonAsync<List<UserDto>>($"/api/groups/{groupId}/members");
            return members ?? new List<UserDto>();
        }
        catch (HttpRequestException)
        {
            return new List<UserDto>();
        }
    }

    /// <summary>
    /// Clears cached data - useful when user context changes
    /// </summary>
    public void ClearCache()
    {
        _cachedCurrentUser = null;
        _cachedUserGroups = null;
        _sessionGroupId = null;
    }
}