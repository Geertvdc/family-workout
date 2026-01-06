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

// Commands for session control
public record StartSessionCommand(Guid SessionId);

public record CancelSessionCommand(Guid SessionId);

public record CompleteSessionCommand(Guid SessionId);

// DTOs for WOD runtime data
public record SessionAssignmentDto(
    Guid SessionId,
    WorkoutSessionStatus Status,
    List<ParticipantAssignmentDto> Participants,
    List<StationDto> Stations
);

public record ParticipantAssignmentDto(
    Guid ParticipantId,
    Guid UserId,
    string UserName,
    int ParticipantIndex
);

public record StationDto(
    int StationIndex,
    string WorkoutTypeId,
    string WorkoutTypeName,
    string? WorkoutTypeDescription
);

public record SessionIntervalDto(
    int RoundNumber,
    int StationIndex
);
