using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutSessionService
{
    private readonly IWorkoutSessionRepository _repository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;

    public WorkoutSessionService(
        IWorkoutSessionRepository repository,
        IGroupRepository groupRepository,
        IUserRepository userRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
    }

    public async Task<WorkoutSessionDto> CreateAsync(CreateWorkoutSessionCommand command)
    {
        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(command.GroupId);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{command.GroupId}' not found.");
        }

        // Validate creator exists
        var creator = await _userRepository.GetByIdAsync(command.CreatorId);
        if (creator == null)
        {
            throw new KeyNotFoundException($"User with ID '{command.CreatorId}' not found.");
        }

        // Create session
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            GroupId = command.GroupId,
            CreatorId = command.CreatorId,
            SessionDate = command.SessionDate,
            Status = WorkoutSessionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(session);

        return ToDto(session);
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetAllAsync()
    {
        var sessions = await _repository.GetAllAsync();
        return sessions.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByGroupIdAsync(Guid groupId)
    {
        var sessions = await _repository.GetByGroupIdAsync(groupId);
        return sessions.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByCreatorIdAsync(Guid creatorId)
    {
        var sessions = await _repository.GetByCreatorIdAsync(creatorId);
        return sessions.Select(ToDto).ToList();
    }

    public async Task<WorkoutSessionDto> GetByIdAsync(Guid id)
    {
        var session = await _repository.GetByIdAsync(id);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{id}' not found.");
        }

        return ToDto(session);
    }

    public async Task<WorkoutSessionDto> UpdateAsync(UpdateWorkoutSessionCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.Id}' not found.");
        }

        // Update session
        existing.SessionDate = command.SessionDate;
        existing.StartedAt = command.StartedAt;
        existing.EndedAt = command.EndedAt;
        existing.Status = command.Status;

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static WorkoutSessionDto ToDto(WorkoutSession session)
    {
        return new WorkoutSessionDto(
            session.Id,
            session.GroupId,
            session.CreatorId,
            session.SessionDate,
            session.StartedAt,
            session.EndedAt,
            session.Status,
            session.CreatedAt
        );
    }
}
