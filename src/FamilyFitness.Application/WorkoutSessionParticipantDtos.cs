namespace FamilyFitness.Application;

public record WorkoutSessionParticipantDto(
    Guid Id,
    Guid WorkoutSessionId,
    Guid UserId,
    int ParticipantIndex,
    DateTime JoinedAt
);

public record CreateWorkoutSessionParticipantCommand(
    Guid WorkoutSessionId,
    Guid UserId,
    int ParticipantIndex
);

public record UpdateWorkoutSessionParticipantCommand(
    Guid Id,
    int ParticipantIndex
);
