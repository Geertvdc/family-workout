using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public record WorkoutSessionDto(
    Guid Id,
    Guid GroupId,
    Guid CreatorId,
    DateTime SessionDate,
    DateTime? StartedAt,
    DateTime? EndedAt,
    WorkoutSessionStatus Status,
    DateTime CreatedAt
);

public record CreateWorkoutSessionCommand(
    Guid GroupId,
    Guid CreatorId,
    DateTime SessionDate
);

public record UpdateWorkoutSessionCommand(
    Guid Id,
    DateTime SessionDate,
    DateTime? StartedAt,
    DateTime? EndedAt,
    WorkoutSessionStatus Status
);
