namespace FamilyFitness.Application;

public record WorkoutTypeDto(
    string Id,
    string Name,
    string? Description
);

public record CreateWorkoutTypeCommand(
    string Name,
    string? Description
);

public record UpdateWorkoutTypeCommand(
    string Id,
    string Name,
    string? Description
);
