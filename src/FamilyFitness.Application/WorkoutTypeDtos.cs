using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public record WorkoutTypeDto(
    string Id,
    string Name,
    string? Description,
    int? EstimatedDurationMinutes,
    string Intensity
);

public record CreateWorkoutTypeCommand(
    string Name,
    string? Description,
    int? EstimatedDurationMinutes,
    Intensity Intensity
);

public record UpdateWorkoutTypeCommand(
    string Id,
    string Name,
    string? Description,
    int? EstimatedDurationMinutes,
    Intensity Intensity
);
