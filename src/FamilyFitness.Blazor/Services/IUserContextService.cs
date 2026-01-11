using FamilyFitness.Application;

namespace FamilyFitness.Blazor.Services;

/// <summary>
/// Service for managing user context and group selection in the Blazor application
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync();
    
    /// <summary>
    /// Gets all groups that the current user owns or is a member of
    /// </summary>
    Task<List<GroupDto>> GetUserGroupsAsync();
    
    /// <summary>
    /// Sets the active group for the current session (not persisted across browser restarts)
    /// </summary>
    void SetSessionGroupId(Guid? groupId);
    
    /// <summary>
    /// Gets the currently selected group for this session
    /// </summary>
    Guid? GetSessionGroupId();
    
    /// <summary>
    /// Validates that the current user has access to the specified group
    /// </summary>
    Task<bool> CanAccessGroupAsync(Guid groupId);
    
    /// <summary>
    /// Gets members of the specified group (if user has access)
    /// </summary>
    Task<List<UserDto>> GetGroupMembersAsync(Guid groupId);
    
    /// <summary>
    /// Clears cached data - useful when user context changes
    /// </summary>
    void ClearCache();
}