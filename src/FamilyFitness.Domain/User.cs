namespace FamilyFitness.Domain;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
    public ICollection<WorkoutSessionParticipant> WorkoutSessionParticipants { get; set; } = new List<WorkoutSessionParticipant>();
}
