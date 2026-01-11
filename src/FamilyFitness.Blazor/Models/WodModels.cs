namespace FamilyFitness.Blazor.Models;

public class SessionAssignmentDto
{
    public Guid SessionId { get; set; }
    public WorkoutSessionStatus Status { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
    public List<StationDto> Stations { get; set; } = new();
}

public class ParticipantDto
{
    public Guid ParticipantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public int ParticipantIndex { get; set; }
}

public class StationDto
{
    public int StationIndex { get; set; }
    public string WorkoutTypeId { get; set; } = "";
    public string WorkoutTypeName { get; set; } = "";
    public string? WorkoutTypeDescription { get; set; }
}

public enum TimerPhase
{
    Ready,
    Work,
    Cooldown
}
