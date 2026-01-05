namespace FamilyFitness.Application;

public record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt
);

public record CreateGroupCommand(
    string Name,
    string? Description
);

public record UpdateGroupCommand(
    Guid Id,
    string Name,
    string? Description
);
