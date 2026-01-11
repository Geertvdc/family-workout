using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class GroupInviteService
{
    private readonly IGroupInviteRepository _inviteRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMembershipRepository _membershipRepository;

    public GroupInviteService(
        IGroupInviteRepository inviteRepository,
        IGroupRepository groupRepository,
        IGroupMembershipRepository membershipRepository)
    {
        _inviteRepository = inviteRepository;
        _groupRepository = groupRepository;
        _membershipRepository = membershipRepository;
    }

    public async Task<GroupInviteDto> CreateInviteAsync(CreateInviteCommand command, Guid requestingUserId)
    {
        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(command.GroupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{command.GroupId}' not found.");
        }

        // Only owner can create invites
        if (group.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the group owner can create invite links.");
        }

        // Generate a unique token (URL-safe GUID)
        var token = Guid.NewGuid().ToString("N"); // 32-char hex string without dashes

        var invite = new GroupInvite
        {
            Id = Guid.NewGuid(),
            GroupId = command.GroupId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _inviteRepository.AddAsync(invite);

        return ToDto(invite, group.Name);
    }

    public async Task<AcceptInviteResult> AcceptInviteAsync(string token, Guid userId)
    {
        // Find invite by token
        var invite = await _inviteRepository.GetByTokenAsync(token);
        if (invite == null)
        {
            return new AcceptInviteResult(false, "Invalid invite link.", null);
        }

        if (!invite.IsActive)
        {
            return new AcceptInviteResult(false, "This invite link is no longer active.", null);
        }

        // Check if user is already a member
        var existingMembership = await _membershipRepository.GetByGroupAndUserAsync(invite.GroupId, userId);
        if (existingMembership != null)
        {
            // User is already a member, just return success with group info
            var existingGroup = await _groupRepository.GetByIdAsync(invite.GroupId);
            return new AcceptInviteResult(
                true,
                null,
                new GroupDto(
                    existingGroup!.Id,
                    existingGroup.Name,
                    existingGroup.Description,
                    existingGroup.OwnerId,
                    existingGroup.OwnerId == userId,
                    existingGroup.CreatedAt
                )
            );
        }

        // Get group info
        var group = await _groupRepository.GetByIdAsync(invite.GroupId);
        if (group == null)
        {
            return new AcceptInviteResult(false, "The group no longer exists.", null);
        }

        // Add user as member
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = invite.GroupId,
            UserId = userId,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };

        await _membershipRepository.AddAsync(membership);

        return new AcceptInviteResult(
            true,
            null,
            new GroupDto(
                group.Id,
                group.Name,
                group.Description,
                group.OwnerId,
                group.OwnerId == userId,
                group.CreatedAt
            )
        );
    }

    public async Task<IReadOnlyList<GroupInviteDto>> GetGroupInvitesAsync(Guid groupId, Guid requestingUserId)
    {
        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{groupId}' not found.");
        }

        // Only owner can view invites
        if (group.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the group owner can view invite links.");
        }

        var invites = await _inviteRepository.GetActiveByGroupIdAsync(groupId);
        return invites.Select(i => ToDto(i, group.Name)).ToList();
    }

    public async Task<GroupInviteDto?> GetInviteByTokenAsync(string token)
    {
        var invite = await _inviteRepository.GetByTokenAsync(token);
        if (invite == null || !invite.IsActive)
        {
            return null;
        }

        return ToDto(invite, invite.Group?.Name ?? "Unknown");
    }

    public async Task RevokeInviteAsync(Guid inviteId, Guid requestingUserId)
    {
        var invite = await _inviteRepository.GetByIdAsync(inviteId);
        if (invite == null)
        {
            throw new KeyNotFoundException($"Invite with ID '{inviteId}' not found.");
        }

        // Get group to check ownership
        var group = await _groupRepository.GetByIdAsync(invite.GroupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group not found.");
        }

        // Only owner can revoke invites
        if (group.OwnerId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the group owner can revoke invite links.");
        }

        invite.IsActive = false;
        await _inviteRepository.UpdateAsync(invite);
    }

    private static GroupInviteDto ToDto(GroupInvite invite, string groupName)
    {
        return new GroupInviteDto(
            invite.Id,
            invite.GroupId,
            groupName,
            invite.Token,
            invite.CreatedAt,
            invite.IsActive
        );
    }
}
