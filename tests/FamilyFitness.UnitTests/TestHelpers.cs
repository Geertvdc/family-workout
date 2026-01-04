using FamilyFitness.Application;
using FamilyFitness.Domain;

namespace FamilyFitness.UnitTests;

public class InMemoryWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly List<WorkoutType> _workoutTypes = new();

    public Task<WorkoutType?> GetByIdAsync(string id)
    {
        var workoutType = _workoutTypes.FirstOrDefault(wt => wt.Id == id);
        return Task.FromResult(workoutType);
    }

    public Task<IReadOnlyList<WorkoutType>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutType>>(_workoutTypes.ToList());
    }

    public Task AddAsync(WorkoutType workoutType)
    {
        _workoutTypes.Add(workoutType);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutType workoutType)
    {
        var index = _workoutTypes.FindIndex(wt => wt.Id == workoutType.Id);
        if (index >= 0)
        {
            _workoutTypes[index] = workoutType;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var workoutType = _workoutTypes.FirstOrDefault(wt => wt.Id == id);
        if (workoutType != null)
        {
            _workoutTypes.Remove(workoutType);
        }
        return Task.CompletedTask;
    }
}

public class FixedIdGenerator : IIdGenerator
{
    private readonly string _id;

    public FixedIdGenerator(string id)
    {
        _id = id;
    }

    public string GenerateId() => _id;
}
