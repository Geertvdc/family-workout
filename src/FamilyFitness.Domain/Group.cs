namespace FamilyFitness.Domain;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    public ICollection<GroupInvite> GroupInvites { get; set; } = new List<GroupInvite>();
}
