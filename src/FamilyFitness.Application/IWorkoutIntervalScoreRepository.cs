using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IWorkoutIntervalScoreRepository
{
    Task<WorkoutIntervalScore?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<WorkoutIntervalScore>> GetAllAsync();
    Task<IReadOnlyList<WorkoutIntervalScore>> GetByParticipantIdAsync(Guid participantId);
    Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutTypeIdAsync(string workoutTypeId);
    Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutSessionIdAsync(Guid workoutSessionId);
    Task AddAsync(WorkoutIntervalScore score);
    Task UpdateAsync(WorkoutIntervalScore score);
    Task DeleteAsync(Guid id);
}
