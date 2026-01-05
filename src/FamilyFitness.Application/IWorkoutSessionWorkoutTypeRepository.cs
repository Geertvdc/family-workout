using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IWorkoutSessionWorkoutTypeRepository
{
    Task<WorkoutSessionWorkoutType?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetAllAsync();
    Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetByWorkoutSessionIdAsync(Guid workoutSessionId);
    Task AddAsync(WorkoutSessionWorkoutType workoutSessionWorkoutType);
    Task UpdateAsync(WorkoutSessionWorkoutType workoutSessionWorkoutType);
    Task DeleteAsync(Guid id);
}
