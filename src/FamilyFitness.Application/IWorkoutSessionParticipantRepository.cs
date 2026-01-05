using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IWorkoutSessionParticipantRepository
{
    Task<WorkoutSessionParticipant?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<WorkoutSessionParticipant>> GetAllAsync();
    Task<IReadOnlyList<WorkoutSessionParticipant>> GetByWorkoutSessionIdAsync(Guid workoutSessionId);
    Task<IReadOnlyList<WorkoutSessionParticipant>> GetByUserIdAsync(Guid userId);
    Task AddAsync(WorkoutSessionParticipant participant);
    Task UpdateAsync(WorkoutSessionParticipant participant);
    Task DeleteAsync(Guid id);
}
