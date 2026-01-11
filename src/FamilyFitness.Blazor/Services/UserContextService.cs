using System.Net.Http.Json;
using FamilyFitness.Application;

namespace FamilyFitness.Blazor.Services;

/// <summary>
/// Implementation of IUserContextService that manages user context and group selection
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly HttpClient _httpClient;
    private readonly object _lockObject = new object();
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
        lock (_lockObject)
        {
            if (_cachedCurrentUser != null)
                return _cachedCurrentUser;
        }

        try
        {
            var user = await _httpClient.GetFromJsonAsync<UserDto>("/api/me");
            lock (_lockObject)
            {
                _cachedCurrentUser = user;
            }
            return user;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<GroupDto>> GetUserGroupsAsync()
    {
        lock (_lockObject)
        {
            if (_cachedUserGroups != null)
                return _cachedUserGroups;
        }

        try
        {
            var groups = await _httpClient.GetFromJsonAsync<List<GroupDto>>("/api/users/me/groups") ?? new List<GroupDto>();
            lock (_lockObject)
            {
                _cachedUserGroups = groups;
            }
            return groups;
        }
        catch (HttpRequestException)
        {
            return new List<GroupDto>();
        }
    }

    /// <inheritdoc />
    public void SetSessionGroupId(Guid? groupId)
    {
        lock (_lockObject)
        {
            _sessionGroupId = groupId;
        }
    }

    /// <inheritdoc />
    public Guid? GetSessionGroupId()
    {
        lock (_lockObject)
        {
            return _sessionGroupId;
        }
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
        lock (_lockObject)
        {
            _cachedCurrentUser = null;
            _cachedUserGroups = null;
            _sessionGroupId = null;
        }
    }
}