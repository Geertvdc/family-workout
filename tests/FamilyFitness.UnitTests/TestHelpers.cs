using FamilyFitness.Application;
using FamilyFitness.Domain;

namespace FamilyFitness.UnitTests;

public class InMemoryWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly List<WorkoutType> _workoutTypes = new();

    public Task<WorkoutType?> GetByIdAsync(string id)
    {
        var workoutType = _workoutTypes.FirstOrDefault(wt => wt.Id == id);
        return Task.FromResult(workoutType);
    }

    public Task<IReadOnlyList<WorkoutType>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutType>>(_workoutTypes.ToList());
    }

    public Task AddAsync(WorkoutType workoutType)
    {
        _workoutTypes.Add(workoutType);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutType workoutType)
    {
        var index = _workoutTypes.FindIndex(wt => wt.Id == workoutType.Id);
        if (index >= 0)
        {
            _workoutTypes[index] = workoutType;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        var workoutType = _workoutTypes.FirstOrDefault(wt => wt.Id == id);
        if (workoutType != null)
        {
            _workoutTypes.Remove(workoutType);
        }
        return Task.CompletedTask;
    }
}

public class FixedIdGenerator : IIdGenerator
{
    private readonly string _id;

    public FixedIdGenerator(string id)
    {
        _id = id;
    }

    public string GenerateId() => _id;
}

public class InMemoryWorkoutSessionRepository : IWorkoutSessionRepository
{
    private readonly List<WorkoutSession> _sessions = new();

    public Task<WorkoutSession?> GetByIdAsync(Guid id)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<WorkoutSession>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutSession>>(_sessions.ToList());
    }

    public Task<IReadOnlyList<WorkoutSession>> GetByGroupIdAsync(Guid groupId)
    {
        var sessions = _sessions.Where(s => s.GroupId == groupId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutSession>>(sessions);
    }

    public Task<IReadOnlyList<WorkoutSession>> GetByCreatorIdAsync(Guid creatorId)
    {
        var sessions = _sessions.Where(s => s.CreatorId == creatorId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutSession>>(sessions);
    }

    public Task<WorkoutSession?> GetActiveSessionByGroupIdAsync(Guid groupId)
    {
        var session = _sessions
            .Where(s => s.GroupId == groupId && s.Status == WorkoutSessionStatus.Active)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
        return Task.FromResult(session);
    }

    public Task AddAsync(WorkoutSession session)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutSession session)
    {
        var index = _sessions.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
        {
            _sessions[index] = session;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == id);
        if (session != null)
        {
            _sessions.Remove(session);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryGroupRepository : IGroupRepository
{
    private readonly List<Group> _groups = new();
    private readonly List<GroupMembership> _memberships = new();

    public Task<Group?> GetByIdAsync(Guid id)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id);
        return Task.FromResult(group);
    }

    public Task<IReadOnlyList<Group>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Group>>(_groups.ToList());
    }

    public Task<IReadOnlyList<Group>> GetByUserMembershipAsync(Guid userId)
    {
        var groupIds = _memberships.Where(m => m.UserId == userId).Select(m => m.GroupId).ToHashSet();
        var groups = _groups.Where(g => groupIds.Contains(g.Id)).ToList();
        return Task.FromResult<IReadOnlyList<Group>>(groups);
    }

    public Task<IReadOnlyList<Group>> GetByOwnerIdAsync(Guid ownerId)
    {
        var groups = _groups.Where(g => g.OwnerId == ownerId).ToList();
        return Task.FromResult<IReadOnlyList<Group>>(groups);
    }

    public Task AddAsync(Group group)
    {
        _groups.Add(group);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Group group)
    {
        var index = _groups.FindIndex(g => g.Id == group.Id);
        if (index >= 0)
        {
            _groups[index] = group;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id);
        if (group != null)
        {
            _groups.Remove(group);
        }
        return Task.CompletedTask;
    }

    // Helper method for tests to add memberships
    public void AddMembership(GroupMembership membership)
    {
        _memberships.Add(membership);
    }
}

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<User>>(_users.ToList());
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEntraObjectIdAsync(string entraObjectId)
    {
        var user = _users.FirstOrDefault(u => u.EntraObjectId != null && u.EntraObjectId.Equals(entraObjectId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index >= 0)
        {
            _users[index] = user;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _users.Remove(user);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryWorkoutSessionWorkoutTypeRepository : IWorkoutSessionWorkoutTypeRepository
{
    private readonly List<WorkoutSessionWorkoutType> _items = new();

    public Task<WorkoutSessionWorkoutType?> GetByIdAsync(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutSessionWorkoutType>>(_items.ToList());
    }

    public Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        var items = _items.Where(i => i.WorkoutSessionId == workoutSessionId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutSessionWorkoutType>>(items);
    }

    public Task AddAsync(WorkoutSessionWorkoutType item)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutSessionWorkoutType item)
    {
        var index = _items.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
        {
            _items[index] = item;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            _items.Remove(item);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryWorkoutSessionParticipantRepository : IWorkoutSessionParticipantRepository
{
    private readonly List<WorkoutSessionParticipant> _participants = new();

    public Task<WorkoutSessionParticipant?> GetByIdAsync(Guid id)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(participant);
    }

    public Task<IReadOnlyList<WorkoutSessionParticipant>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutSessionParticipant>>(_participants.ToList());
    }

    public Task<IReadOnlyList<WorkoutSessionParticipant>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        var participants = _participants.Where(p => p.WorkoutSessionId == workoutSessionId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutSessionParticipant>>(participants);
    }

    public Task<IReadOnlyList<WorkoutSessionParticipant>> GetByUserIdAsync(Guid userId)
    {
        var participants = _participants.Where(p => p.UserId == userId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutSessionParticipant>>(participants);
    }

    public Task AddAsync(WorkoutSessionParticipant participant)
    {
        _participants.Add(participant);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutSessionParticipant participant)
    {
        var index = _participants.FindIndex(p => p.Id == participant.Id);
        if (index >= 0)
        {
            _participants[index] = participant;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == id);
        if (participant != null)
        {
            _participants.Remove(participant);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryWorkoutIntervalScoreRepository : IWorkoutIntervalScoreRepository
{
    private readonly List<WorkoutIntervalScore> _scores = new();

    public Task<WorkoutIntervalScore?> GetByIdAsync(Guid id)
    {
        var score = _scores.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(score);
    }

    public Task<IReadOnlyList<WorkoutIntervalScore>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<WorkoutIntervalScore>>(_scores.ToList());
    }

    public Task<IReadOnlyList<WorkoutIntervalScore>> GetByParticipantIdAsync(Guid participantId)
    {
        var scores = _scores.Where(s => s.ParticipantId == participantId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutIntervalScore>>(scores);
    }

    public Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutTypeIdAsync(string workoutTypeId)
    {
        var scores = _scores.Where(s => s.WorkoutTypeId == workoutTypeId).ToList();
        return Task.FromResult<IReadOnlyList<WorkoutIntervalScore>>(scores);
    }

    public Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        // For testing purposes, we'll need to mock this since we don't have the full relationship
        // In a real implementation, this would join with WorkoutSessionParticipant
        var scores = new List<WorkoutIntervalScore>();
        return Task.FromResult<IReadOnlyList<WorkoutIntervalScore>>(scores);
    }

    public Task AddAsync(WorkoutIntervalScore score)
    {
        _scores.Add(score);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WorkoutIntervalScore score)
    {
        var index = _scores.FindIndex(s => s.Id == score.Id);
        if (index >= 0)
        {
            _scores[index] = score;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        var score = _scores.FirstOrDefault(s => s.Id == id);
        if (score != null)
        {
            _scores.Remove(score);
        }
        return Task.CompletedTask;
    }
}
