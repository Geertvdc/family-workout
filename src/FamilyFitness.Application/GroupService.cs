using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class GroupService
{
    private readonly IGroupRepository _repository;
    private readonly IGroupMembershipRepository _membershipRepository;

    public GroupService(IGroupRepository repository, IGroupMembershipRepository membershipRepository)
    {
        _repository = repository;
        _membershipRepository = membershipRepository;
    }

    public async Task<GroupDto> CreateAsync(CreateGroupCommand command, Guid ownerId)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required.", nameof(command.Name));
        }

        if (command.Name.Length > 200)
        {
            throw new ArgumentException("Name must be 200 characters or less.", nameof(command.Name));
        }

        // Validate description
        if (command.Description != null && command.Description.Length > 1000)
        {
            throw new ArgumentException("Description must be 1000 characters or less.", nameof(command.Description));
        }

        // Create group with owner
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = command.Description?.Trim(),
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(group);

        // Automatically add owner as Admin member
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = ownerId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow
        };

        await _membershipRepository.AddAsync(membership);

        return ToDto(group, ownerId);
    }

    public async Task<IReadOnlyList<GroupDto>> GetAllAsync(Guid? requestingUserId = null)
    {
        var groups = await _repository.GetAllAsync();
        return groups.Select(g => ToDto(g, requestingUserId)).ToList();
    }

    public async Task<IReadOnlyList<GroupDto>> GetUserGroupsAsync(Guid userId)
    {
        var groups = await _repository.GetByUserMembershipAsync(userId);
        return groups.Select(g => ToDto(g, userId)).ToList();
    }

    public async Task<GroupDto> GetByIdAsync(Guid id, Guid? requestingUserId = null)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{id}' not found.");
        }

        return ToDto(group, requestingUserId);
    }

    public async Task<GroupDto> UpdateAsync(UpdateGroupCommand command, Guid requestingUserId)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group with ID '{command.Id}' not found.");
        }

        // Check if user is the owner
        if (existing.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the group owner can update the group.");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required.", nameof(command.Name));
        }

        if (command.Name.Length > 200)
        {
            throw new ArgumentException("Name must be 200 characters or less.", nameof(command.Name));
        }

        // Validate description
        if (command.Description != null && command.Description.Length > 1000)
        {
            throw new ArgumentException("Description must be 1000 characters or less.", nameof(command.Description));
        }

        // Update group
        existing.Name = command.Name.Trim();
        existing.Description = command.Description?.Trim();

        await _repository.UpdateAsync(existing);

        return ToDto(existing, requestingUserId);
    }

    public async Task DeleteAsync(Guid id, Guid requestingUserId)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group with ID '{id}' not found.");
        }

        // Check if user is the owner
        if (existing.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the group owner can delete the group.");
        }

        await _repository.DeleteAsync(id);
    }

    public async Task<bool> IsUserGroupOwnerAsync(Guid groupId, Guid userId)
    {
        var group = await _repository.GetByIdAsync(groupId);
        return group != null && group.OwnerId == userId;
    }

    public async Task<bool> IsUserGroupMemberAsync(Guid groupId, Guid userId)
    {
        var membership = await _membershipRepository.GetByGroupAndUserAsync(groupId, userId);
        return membership != null;
    }

    private static GroupDto ToDto(Group group, Guid? requestingUserId = null)
    {
        return new GroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.OwnerId,
            requestingUserId.HasValue && group.OwnerId == requestingUserId.Value,
            group.CreatedAt
        );
    }
}
