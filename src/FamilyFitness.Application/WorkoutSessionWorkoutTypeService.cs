using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutSessionWorkoutTypeService
{
    private readonly IWorkoutSessionWorkoutTypeRepository _repository;
    private readonly IWorkoutSessionRepository _workoutSessionRepository;

    public WorkoutSessionWorkoutTypeService(
        IWorkoutSessionWorkoutTypeRepository repository,
        IWorkoutSessionRepository workoutSessionRepository)
    {
        _repository = repository;
        _workoutSessionRepository = workoutSessionRepository;
    }

    public async Task<WorkoutSessionWorkoutTypeDto> CreateAsync(CreateWorkoutSessionWorkoutTypeCommand command)
    {
        // Validate workout session exists
        var session = await _workoutSessionRepository.GetByIdAsync(command.WorkoutSessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.WorkoutSessionId}' not found.");
        }

        // Validate station index
        if (command.StationIndex < 1 || command.StationIndex > 4)
        {
            throw new ArgumentException("Station index must be between 1 and 4.", nameof(command.StationIndex));
        }

        // Validate workout type ID
        if (string.IsNullOrWhiteSpace(command.WorkoutTypeId))
        {
            throw new ArgumentException("Workout type ID is required.", nameof(command.WorkoutTypeId));
        }

        // Create workout session workout type
        var workoutSessionWorkoutType = new WorkoutSessionWorkoutType
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = command.WorkoutSessionId,
            WorkoutTypeId = command.WorkoutTypeId.Trim(),
            StationIndex = command.StationIndex
        };

        await _repository.AddAsync(workoutSessionWorkoutType);

        return ToDto(workoutSessionWorkoutType);
    }

    public async Task<IReadOnlyList<WorkoutSessionWorkoutTypeDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionWorkoutTypeDto>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        var items = await _repository.GetByWorkoutSessionIdAsync(workoutSessionId);
        return items.Select(ToDto).ToList();
    }

    public async Task<WorkoutSessionWorkoutTypeDto> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null)
        {
            throw new KeyNotFoundException($"Workout session workout type with ID '{id}' not found.");
        }

        return ToDto(item);
    }

    public async Task<WorkoutSessionWorkoutTypeDto> UpdateAsync(UpdateWorkoutSessionWorkoutTypeCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session workout type with ID '{command.Id}' not found.");
        }

        // Validate station index
        if (command.StationIndex < 1 || command.StationIndex > 4)
        {
            throw new ArgumentException("Station index must be between 1 and 4.", nameof(command.StationIndex));
        }

        // Validate workout type ID
        if (string.IsNullOrWhiteSpace(command.WorkoutTypeId))
        {
            throw new ArgumentException("Workout type ID is required.", nameof(command.WorkoutTypeId));
        }

        // Update
        existing.WorkoutTypeId = command.WorkoutTypeId.Trim();
        existing.StationIndex = command.StationIndex;

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session workout type with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static WorkoutSessionWorkoutTypeDto ToDto(WorkoutSessionWorkoutType item)
    {
        return new WorkoutSessionWorkoutTypeDto(
            item.Id,
            item.WorkoutSessionId,
            item.WorkoutTypeId,
            item.StationIndex
        );
    }
}
