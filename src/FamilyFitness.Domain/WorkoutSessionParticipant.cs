namespace FamilyFitness.Domain;

public class WorkoutSessionParticipant
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public Guid UserId { get; set; }
    public int ParticipantIndex { get; set; } // Order in which they joined
    public DateTime JoinedAt { get; set; }
    
    // Navigation properties
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<WorkoutIntervalScore> IntervalScores { get; set; } = new List<WorkoutIntervalScore>();
}
