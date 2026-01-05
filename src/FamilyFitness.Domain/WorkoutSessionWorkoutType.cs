namespace FamilyFitness.Domain;

public class WorkoutSessionWorkoutType
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public string WorkoutTypeId { get; set; } = string.Empty;
    public int StationIndex { get; set; } // 1-4
    
    // Navigation properties
    public WorkoutSession WorkoutSession { get; set; } = null!;
}
