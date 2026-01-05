namespace FamilyFitness.Domain;

public class GroupMembership
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? Role { get; set; }
    
    // Navigation properties
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
