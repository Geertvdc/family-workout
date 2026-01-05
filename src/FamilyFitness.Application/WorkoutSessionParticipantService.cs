using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutSessionParticipantService
{
    private readonly IWorkoutSessionParticipantRepository _repository;
    private readonly IWorkoutSessionRepository _workoutSessionRepository;
    private readonly IUserRepository _userRepository;

    public WorkoutSessionParticipantService(
        IWorkoutSessionParticipantRepository repository,
        IWorkoutSessionRepository workoutSessionRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _workoutSessionRepository = workoutSessionRepository;
        _userRepository = userRepository;
    }

    public async Task<WorkoutSessionParticipantDto> CreateAsync(CreateWorkoutSessionParticipantCommand command)
    {
        // Validate workout session exists
        var session = await _workoutSessionRepository.GetByIdAsync(command.WorkoutSessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.WorkoutSessionId}' not found.");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{command.UserId}' not found.");
        }

        // Validate participant index
        if (command.ParticipantIndex < 1)
        {
            throw new ArgumentException("Participant index must be greater than 0.", nameof(command.ParticipantIndex));
        }

        // Create participant
        var participant = new WorkoutSessionParticipant
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = command.WorkoutSessionId,
            UserId = command.UserId,
            ParticipantIndex = command.ParticipantIndex,
            JoinedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(participant);

        return ToDto(participant);
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipantDto>> GetAllAsync()
    {
        var participants = await _repository.GetAllAsync();
        return participants.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipantDto>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        var participants = await _repository.GetByWorkoutSessionIdAsync(workoutSessionId);
        return participants.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipantDto>> GetByUserIdAsync(Guid userId)
    {
        var participants = await _repository.GetByUserIdAsync(userId);
        return participants.Select(ToDto).ToList();
    }

    public async Task<WorkoutSessionParticipantDto> GetByIdAsync(Guid id)
    {
        var participant = await _repository.GetByIdAsync(id);
        if (participant == null)
        {
            throw new KeyNotFoundException($"Workout session participant with ID '{id}' not found.");
        }

        return ToDto(participant);
    }

    public async Task<WorkoutSessionParticipantDto> UpdateAsync(UpdateWorkoutSessionParticipantCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session participant with ID '{command.Id}' not found.");
        }

        // Validate participant index
        if (command.ParticipantIndex < 1)
        {
            throw new ArgumentException("Participant index must be greater than 0.", nameof(command.ParticipantIndex));
        }

        // Update participant
        existing.ParticipantIndex = command.ParticipantIndex;

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session participant with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static WorkoutSessionParticipantDto ToDto(WorkoutSessionParticipant participant)
    {
        return new WorkoutSessionParticipantDto(
            participant.Id,
            participant.WorkoutSessionId,
            participant.UserId,
            participant.ParticipantIndex,
            participant.JoinedAt
        );
    }
}
