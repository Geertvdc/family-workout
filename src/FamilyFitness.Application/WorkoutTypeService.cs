using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutTypeService
{
    private readonly IWorkoutTypeRepository _repository;
    private readonly IIdGenerator _idGenerator;

    public WorkoutTypeService(IWorkoutTypeRepository repository, IIdGenerator idGenerator)
    {
        _repository = repository;
        _idGenerator = idGenerator;
    }

    public async Task<WorkoutTypeDto> CreateAsync(CreateWorkoutTypeCommand command)
    {
        // Check for duplicate name (case-insensitive)
        var existing = await _repository.GetAllAsync();
        if (existing.Any(wt => wt.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A workout type with the name '{command.Name}' already exists.");
        }

        // Generate ID and create entity
        var id = _idGenerator.GenerateId();
        var workoutType = new WorkoutType(
            id,
            command.Name,
            command.Description
        );

        await _repository.AddAsync(workoutType);

        return ToDto(workoutType);
    }

    public async Task<IReadOnlyList<WorkoutTypeDto>> GetAllAsync()
    {
        var workoutTypes = await _repository.GetAllAsync();
        return workoutTypes.Select(ToDto).ToList();
    }

    public async Task<WorkoutTypeDto> GetByIdAsync(string id)
    {
        var workoutType = await _repository.GetByIdAsync(id);
        if (workoutType == null)
        {
            throw new KeyNotFoundException($"Workout type with ID '{id}' not found.");
        }

        return ToDto(workoutType);
    }

    public async Task<WorkoutTypeDto> UpdateAsync(UpdateWorkoutTypeCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout type with ID '{command.Id}' not found.");
        }

        // Check for duplicate name (case-insensitive), excluding current item
        var all = await _repository.GetAllAsync();
        if (all.Any(wt => wt.Id != command.Id && wt.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A workout type with the name '{command.Name}' already exists.");
        }

        // Update entity
        var updated = existing.WithUpdatedDetails(
            command.Name,
            command.Description
        );

        await _repository.UpdateAsync(updated);

        return ToDto(updated);
    }

    public async Task DeleteAsync(string id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout type with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static WorkoutTypeDto ToDto(WorkoutType workoutType)
    {
        return new WorkoutTypeDto(
            workoutType.Id,
            workoutType.Name,
            workoutType.Description
        );
    }
}
