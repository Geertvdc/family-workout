namespace FamilyFitness.Application;

public record UserDto(
    Guid Id,
    string? EntraObjectId,
    string Username,
    string Email,
    DateTime CreatedAt
);

public record CreateUserCommand(
    string? EntraObjectId,
    string Username,
    string Email
);

public record UpdateUserCommand(
    Guid Id,
    string Username,
    string Email
);
