namespace FamilyFitness.Application;

public record GroupMembershipDto(
    Guid Id,
    Guid GroupId,
    Guid UserId,
    DateTime JoinedAt,
    string? Role
);

public record CreateGroupMembershipCommand(
    Guid GroupId,
    Guid UserId,
    string? Role
);

public record UpdateGroupMembershipCommand(
    Guid Id,
    string? Role
);
