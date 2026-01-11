namespace FamilyFitness.Domain;

public class GroupInvite
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Group Group { get; set; } = null!;
}
