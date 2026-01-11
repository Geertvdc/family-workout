namespace FamilyFitness.Blazor.Models;

// Shared DTOs used across multiple Blazor pages to avoid duplication

public enum WorkoutSessionStatus
{
    Pending = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public bool IsOwner { get; set; }
}

public class WorkoutTypeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class WorkoutSessionDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime SessionDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public WorkoutSessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupInviteDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActive { get; set; }
}

public class GroupMembershipDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? Role { get; set; }
}

public class WorkoutSessionParticipantDto
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public Guid UserId { get; set; }
    public int ParticipantIndex { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class WorkoutSessionWorkoutTypeDto
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public string WorkoutTypeId { get; set; } = string.Empty;
    public int StationIndex { get; set; }
}

public class WorkoutIntervalScoreDto
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public int RoundNumber { get; set; }
    public int StationIndex { get; set; }
    public string WorkoutTypeId { get; set; } = string.Empty;
    public int Score { get; set; }
    public decimal? Weight { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class ErrorResponse
{
    public string error { get; set; } = string.Empty;
}