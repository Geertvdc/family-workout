using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IWorkoutSessionRepository
{
    Task<WorkoutSession?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<WorkoutSession>> GetAllAsync();
    Task<IReadOnlyList<WorkoutSession>> GetByGroupIdAsync(Guid groupId);
    Task<IReadOnlyList<WorkoutSession>> GetByCreatorIdAsync(Guid creatorId);
    Task AddAsync(WorkoutSession session);
    Task UpdateAsync(WorkoutSession session);
    Task DeleteAsync(Guid id);
}
