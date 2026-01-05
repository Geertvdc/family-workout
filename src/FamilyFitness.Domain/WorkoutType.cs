namespace FamilyFitness.Domain;

public class WorkoutType
{
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }

    public WorkoutType(
        string id,
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required", nameof(id));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Id = id;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public WorkoutType WithUpdatedDetails(
        string name,
        string? description)
    {
        return new WorkoutType(Id, name, description);
    }
}
