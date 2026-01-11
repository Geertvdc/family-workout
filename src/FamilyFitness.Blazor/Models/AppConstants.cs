namespace FamilyFitness.Blazor.Models;

/// <summary>
/// Application constants used across the Blazor application
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Default group ID for MVP - matches seeded data
    /// </summary>
    public static readonly Guid DefaultGroupId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    
    /// <summary>
    /// Default creator/user ID for MVP - matches seeded data
    /// </summary>
    public static readonly Guid DefaultCreatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
}