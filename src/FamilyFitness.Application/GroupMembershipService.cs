using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class GroupMembershipService
{
    private readonly IGroupMembershipRepository _repository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;

    public GroupMembershipService(
        IGroupMembershipRepository repository,
        IGroupRepository groupRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
    }

    public async Task<GroupMembershipDto> CreateAsync(CreateGroupMembershipCommand command)
    {
        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(command.GroupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{command.GroupId}' not found.");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{command.UserId}' not found.");
        }

        // Validate role
        if (command.Role != null && command.Role.Length > 50)
        {
            throw new ArgumentException("Role must be 50 characters or less.", nameof(command.Role));
        }

        // Check for duplicate membership
        var existing = await _repository.GetByGroupAndUserAsync(command.GroupId, command.UserId);
        if (existing != null)
        {
            throw new InvalidOperationException($"User is already a member of this group.");
        }

        // Create membership
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = command.GroupId,
            UserId = command.UserId,
            JoinedAt = DateTime.UtcNow,
            Role = command.Role?.Trim()
        };

        await _repository.AddAsync(membership);

        return ToDto(membership);
    }

    public async Task<IReadOnlyList<GroupMembershipDto>> GetAllAsync()
    {
        var memberships = await _repository.GetAllAsync();
        return memberships.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<GroupMembershipDto>> GetByGroupIdAsync(Guid groupId)
    {
        var memberships = await _repository.GetByGroupIdAsync(groupId);
        return memberships.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<GroupMembershipDto>> GetByUserIdAsync(Guid userId)
    {
        var memberships = await _repository.GetByUserIdAsync(userId);
        return memberships.Select(ToDto).ToList();
    }

    public async Task<GroupMembershipDto> GetByIdAsync(Guid id)
    {
        var membership = await _repository.GetByIdAsync(id);
        if (membership == null)
        {
            throw new KeyNotFoundException($"Group membership with ID '{id}' not found.");
        }

        return ToDto(membership);
    }

    public async Task<GroupMembershipDto> UpdateAsync(UpdateGroupMembershipCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group membership with ID '{command.Id}' not found.");
        }

        // Validate role
        if (command.Role != null && command.Role.Length > 50)
        {
            throw new ArgumentException("Role must be 50 characters or less.", nameof(command.Role));
        }

        // Update membership
        existing.Role = command.Role?.Trim();

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group membership with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static GroupMembershipDto ToDto(GroupMembership membership)
    {
        return new GroupMembershipDto(
            membership.Id,
            membership.GroupId,
            membership.UserId,
            membership.JoinedAt,
            membership.Role
        );
    }
}
