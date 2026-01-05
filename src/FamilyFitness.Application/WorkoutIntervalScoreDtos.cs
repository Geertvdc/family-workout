namespace FamilyFitness.Application;

public record WorkoutIntervalScoreDto(
    Guid Id,
    Guid ParticipantId,
    int RoundNumber,
    int StationIndex,
    string WorkoutTypeId,
    int Score,
    decimal? Weight,
    DateTime RecordedAt
);

public record CreateWorkoutIntervalScoreCommand(
    Guid ParticipantId,
    int RoundNumber,
    int StationIndex,
    string WorkoutTypeId,
    int Score,
    decimal? Weight
);

public record UpdateWorkoutIntervalScoreCommand(
    Guid Id,
    int Score,
    decimal? Weight
);
