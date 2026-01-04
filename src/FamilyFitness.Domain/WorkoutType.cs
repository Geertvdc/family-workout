namespace FamilyFitness.Domain;

public class WorkoutType
{
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public int? EstimatedDurationMinutes { get; }
    public Intensity Intensity { get; }

    public WorkoutType(
        string id,
        string name,
        string? description,
        int? estimatedDurationMinutes,
        Intensity intensity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required", nameof(id));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        
        if (estimatedDurationMinutes.HasValue && estimatedDurationMinutes.Value <= 0)
            throw new ArgumentException("EstimatedDurationMinutes must be greater than 0 when provided", nameof(estimatedDurationMinutes));

        Id = id;
        Name = name.Trim();
        Description = description?.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        Intensity = intensity;
    }

    public WorkoutType WithUpdatedDetails(
        string name,
        string? description,
        int? estimatedDurationMinutes,
        Intensity intensity)
    {
        return new WorkoutType(Id, name, description, estimatedDurationMinutes, intensity);
    }
}
