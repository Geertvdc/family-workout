using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class WorkoutSessionService
{
    private readonly IWorkoutSessionRepository _repository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWorkoutSessionWorkoutTypeRepository _workoutTypeRepository;
    private readonly IWorkoutSessionParticipantRepository _participantRepository;
    private readonly IWorkoutIntervalScoreRepository _scoreRepository;

    public WorkoutSessionService(
        IWorkoutSessionRepository repository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IWorkoutSessionWorkoutTypeRepository workoutTypeRepository,
        IWorkoutSessionParticipantRepository participantRepository,
        IWorkoutIntervalScoreRepository scoreRepository)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _workoutTypeRepository = workoutTypeRepository;
        _participantRepository = participantRepository;
        _scoreRepository = scoreRepository;
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
            SessionDate = command.SessionDate.Kind == DateTimeKind.Utc 
                ? command.SessionDate 
                : DateTime.SpecifyKind(command.SessionDate, DateTimeKind.Utc),
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
        existing.SessionDate = command.SessionDate.Kind == DateTimeKind.Utc 
            ? command.SessionDate 
            : DateTime.SpecifyKind(command.SessionDate, DateTimeKind.Utc);
        existing.StartedAt = command.StartedAt.HasValue 
            ? (command.StartedAt.Value.Kind == DateTimeKind.Utc 
                ? command.StartedAt 
                : DateTime.SpecifyKind(command.StartedAt.Value, DateTimeKind.Utc))
            : null;
        existing.EndedAt = command.EndedAt.HasValue 
            ? (command.EndedAt.Value.Kind == DateTimeKind.Utc 
                ? command.EndedAt 
                : DateTime.SpecifyKind(command.EndedAt.Value, DateTimeKind.Utc))
            : null;
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

    public async Task<WorkoutSessionDto> StartSessionAsync(StartSessionCommand command)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.SessionId}' not found.");
        }

        if (session.Status != WorkoutSessionStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start session with status '{session.Status}'. Only Pending sessions can be started.");
        }

        session.Status = WorkoutSessionStatus.Active;
        session.StartedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);

        return ToDto(session);
    }

    public async Task<WorkoutSessionDto> CancelSessionAsync(CancelSessionCommand command)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.SessionId}' not found.");
        }

        if (session.Status == WorkoutSessionStatus.Completed || session.Status == WorkoutSessionStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel session with status '{session.Status}'.");
        }

        session.Status = WorkoutSessionStatus.Cancelled;
        session.EndedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);

        // Fill missing scores with 0
        await FillMissingScoresAsync(session.Id);

        return ToDto(session);
    }

    public async Task<WorkoutSessionDto> CompleteSessionAsync(CompleteSessionCommand command)
    {
        var session = await _repository.GetByIdAsync(command.SessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{command.SessionId}' not found.");
        }

        if (session.Status != WorkoutSessionStatus.Active)
        {
            throw new InvalidOperationException($"Cannot complete session with status '{session.Status}'. Only Active sessions can be completed.");
        }

        session.Status = WorkoutSessionStatus.Completed;
        session.EndedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);

        // Fill any missing scores with 0
        await FillMissingScoresAsync(session.Id);

        return ToDto(session);
    }

    public async Task<WorkoutSessionDto?> GetActiveSessionByGroupIdAsync(Guid groupId)
    {
        var session = await _repository.GetActiveSessionByGroupIdAsync(groupId);
        return session != null ? ToDto(session) : null;
    }

    public async Task<SessionAssignmentDto> GetSessionAssignmentsAsync(Guid sessionId)
    {
        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Workout session with ID '{sessionId}' not found.");
        }

        var participants = await _participantRepository.GetByWorkoutSessionIdAsync(sessionId);
        var stations = await _workoutTypeRepository.GetByWorkoutSessionIdAsync(sessionId);

        var participantDtos = new List<ParticipantAssignmentDto>();
        foreach (var p in participants.OrderBy(x => x.ParticipantIndex))
        {
            var user = await _userRepository.GetByIdAsync(p.UserId);
            participantDtos.Add(new ParticipantAssignmentDto(
                p.Id,
                p.UserId,
                user?.Username ?? "Unknown",
                p.ParticipantIndex
            ));
        }

        var stationDtos = new List<StationDto>();
        foreach (var s in stations.OrderBy(x => x.StationIndex))
        {
            // Note: WorkoutType is a lookup by string ID, not in repository pattern
            stationDtos.Add(new StationDto(
                s.StationIndex,
                s.WorkoutTypeId,
                s.WorkoutTypeId, // Will be replaced with actual name in API layer
                null
            ));
        }

        return new SessionAssignmentDto(
            session.Id,
            session.Status,
            participantDtos,
            stationDtos
        );
    }

    private async Task FillMissingScoresAsync(Guid sessionId)
    {
        var participants = await _participantRepository.GetByWorkoutSessionIdAsync(sessionId);
        var stations = await _workoutTypeRepository.GetByWorkoutSessionIdAsync(sessionId);

        foreach (var participant in participants)
        {
            var existingScores = await _scoreRepository.GetByParticipantIdAsync(participant.Id);
            
            // For each round (1-3) and each station (1-4)
            for (int round = 1; round <= 3; round++)
            {
                foreach (var station in stations)
                {
                    var scoreExists = existingScores.Any(s => 
                        s.RoundNumber == round && s.StationIndex == station.StationIndex);

                    if (!scoreExists)
                    {
                        var score = new WorkoutIntervalScore
                        {
                            Id = Guid.NewGuid(),
                            ParticipantId = participant.Id,
                            RoundNumber = round,
                            StationIndex = station.StationIndex,
                            WorkoutTypeId = station.WorkoutTypeId,
                            Score = 0,
                            RecordedAt = DateTime.UtcNow
                        };
                        await _scoreRepository.AddAsync(score);
                    }
                }
            }
        }
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
