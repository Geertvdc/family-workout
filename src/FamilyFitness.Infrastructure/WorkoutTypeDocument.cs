namespace FamilyFitness.Infrastructure;

public class WorkoutTypeDocument
{
    public string id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string Intensity { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = "WorkoutTypes";
}
