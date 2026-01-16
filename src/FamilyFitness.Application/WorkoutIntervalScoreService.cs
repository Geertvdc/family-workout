using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutIntervalScoreService
{
    private readonly IWorkoutIntervalScoreRepository _repository;
    private readonly IWorkoutSessionParticipantRepository _participantRepository;

    public WorkoutIntervalScoreService(
        IWorkoutIntervalScoreRepository repository,
        IWorkoutSessionParticipantRepository participantRepository)
    {
        _repository = repository;
        _participantRepository = participantRepository;
    }

    public async Task<WorkoutIntervalScoreDto> CreateAsync(CreateWorkoutIntervalScoreCommand command)
    {
        // Validate participant exists
        var participant = await _participantRepository.GetByIdAsync(command.ParticipantId);
        if (participant == null)
        {
            throw new KeyNotFoundException($"Workout session participant with ID '{command.ParticipantId}' not found.");
        }

        // Validate round number
        if (command.RoundNumber < 1 || command.RoundNumber > 3)
        {
            throw new ArgumentException("Round number must be between 1 and 3.", nameof(command.RoundNumber));
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

        // Validate score
        if (command.Score < 0)
        {
            throw new ArgumentException("Score must be 0 or greater.", nameof(command.Score));
        }

        // Create score
        var score = new WorkoutIntervalScore
        {
            Id = Guid.NewGuid(),
            ParticipantId = command.ParticipantId,
            RoundNumber = command.RoundNumber,
            StationIndex = command.StationIndex,
            WorkoutTypeId = command.WorkoutTypeId.Trim(),
            Score = command.Score,
            Weight = command.Weight,
            RecordedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(score);

        return ToDto(score);
    }

    public async Task<IReadOnlyList<WorkoutIntervalScoreDto>> GetAllAsync()
    {
        var scores = await _repository.GetAllAsync();
        return scores.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScoreDto>> GetByParticipantIdAsync(Guid participantId)
    {
        var scores = await _repository.GetByParticipantIdAsync(participantId);
        return scores.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScoreDto>> GetByWorkoutTypeIdAsync(string workoutTypeId)
    {
        var scores = await _repository.GetByWorkoutTypeIdAsync(workoutTypeId);
        return scores.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScoreDto>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        var scores = await _repository.GetByWorkoutSessionIdAsync(workoutSessionId);
        return scores.Select(ToDto).ToList();
    }

    public async Task<WorkoutIntervalScoreDto> GetByIdAsync(Guid id)
    {
        var score = await _repository.GetByIdAsync(id);
        if (score == null)
        {
            throw new KeyNotFoundException($"Workout interval score with ID '{id}' not found.");
        }

        return ToDto(score);
    }

    public async Task<WorkoutIntervalScoreDto> UpdateAsync(UpdateWorkoutIntervalScoreCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout interval score with ID '{command.Id}' not found.");
        }

        // Validate score
        if (command.Score < 0)
        {
            throw new ArgumentException("Score must be 0 or greater.", nameof(command.Score));
        }

        // Update score
        // Note: Per data model spec, scores should be immutable in production to maintain data integrity.
        // This update method is provided for testing and initial data correction purposes only.
        // Consider enforcing immutability at the database level with triggers or removing this method in production.
        existing.Score = command.Score;
        existing.Weight = command.Weight;

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout interval score with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static WorkoutIntervalScoreDto ToDto(WorkoutIntervalScore score)
    {
        return new WorkoutIntervalScoreDto(
            score.Id,
            score.ParticipantId,
            score.RoundNumber,
            score.StationIndex,
            score.WorkoutTypeId,
            score.Score,
            score.Weight,
            score.RecordedAt
        );
    }
}
