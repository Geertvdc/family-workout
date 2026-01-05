namespace FamilyFitness.Domain;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
}
