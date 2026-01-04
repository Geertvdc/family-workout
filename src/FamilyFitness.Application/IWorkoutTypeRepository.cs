using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IWorkoutTypeRepository
{
    Task<WorkoutType?> GetByIdAsync(string id);
    Task<IReadOnlyList<WorkoutType>> GetAllAsync();
    Task AddAsync(WorkoutType workoutType);
    Task UpdateAsync(WorkoutType workoutType);
    Task DeleteAsync(string id);
}
