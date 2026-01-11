namespace FamilyFitness.Application;

public record GroupInviteDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Token,
    DateTime CreatedAt,
    bool IsActive
);

public record CreateInviteCommand(
    Guid GroupId
);

public record AcceptInviteResult(
    bool Success,
    string? ErrorMessage,
    GroupDto? Group
);
