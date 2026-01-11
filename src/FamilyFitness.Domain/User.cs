namespace FamilyFitness.Domain;

public class User
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The Azure Entra External ID Object ID (oid claim).
    /// This is the stable identifier for the user across sessions.
    /// </summary>
    public string? EntraObjectId { get; set; }
    
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's actual email address (from Google, etc.), not the .onmicrosoft.com shadow account.
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
    public ICollection<WorkoutSessionParticipant> WorkoutSessionParticipants { get; set; } = new List<WorkoutSessionParticipant>();
}
