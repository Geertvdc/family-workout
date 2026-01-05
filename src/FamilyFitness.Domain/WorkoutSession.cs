namespace FamilyFitness.Domain;

public class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime SessionDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public WorkoutSessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Group Group { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public ICollection<WorkoutSessionWorkoutType> WorkoutSessionWorkoutTypes { get; set; } = new List<WorkoutSessionWorkoutType>();
    public ICollection<WorkoutSessionParticipant> Participants { get; set; } = new List<WorkoutSessionParticipant>();
}
