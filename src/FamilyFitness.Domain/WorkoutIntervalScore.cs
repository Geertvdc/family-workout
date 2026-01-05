namespace FamilyFitness.Domain;

public class WorkoutIntervalScore
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public int RoundNumber { get; set; } // 1-3
    public int StationIndex { get; set; } // 1-4
    public int Score { get; set; } // Allows 0
    public decimal? Weight { get; set; }
    public DateTime RecordedAt { get; set; }
    
    // Navigation properties
    public WorkoutSessionParticipant Participant { get; set; } = null!;
}
