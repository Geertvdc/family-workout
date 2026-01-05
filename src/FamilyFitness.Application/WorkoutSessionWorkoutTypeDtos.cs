namespace FamilyFitness.Application;

public record WorkoutSessionWorkoutTypeDto(
    Guid Id,
    Guid WorkoutSessionId,
    string WorkoutTypeId,
    int StationIndex
);

public record CreateWorkoutSessionWorkoutTypeCommand(
    Guid WorkoutSessionId,
    string WorkoutTypeId,
    int StationIndex
);

public record UpdateWorkoutSessionWorkoutTypeCommand(
    Guid Id,
    string WorkoutTypeId,
    int StationIndex
);
