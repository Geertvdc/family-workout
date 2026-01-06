using FamilyFitness.Application;
using FamilyFitness.Domain;

namespace FamilyFitness.UnitTests;

public class WorkoutSessionServiceTests
{
    [Fact]
    public async Task StartSessionAsync_PendingSession_TransitionsToActive()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);
        var command = new StartSessionCommand(sessionId);

        // Act
        var result = await service.StartSessionAsync(command);

        // Assert
        Assert.Equal(WorkoutSessionStatus.Active, result.Status);
        Assert.NotNull(result.StartedAt);
    }

    [Fact]
    public async Task StartSessionAsync_ActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);
        var command = new StartSessionCommand(sessionId);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartSessionAsync(command)
        );
        Assert.Contains("Only Pending sessions can be started", exception.Message);
    }

    [Fact]
    public async Task CancelSessionAsync_ActiveSession_TransitionsToCancelledAndFillsScores()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        // Add participant and stations
        await participantRepo.AddAsync(new WorkoutSessionParticipant
        {
            Id = participantId,
            WorkoutSessionId = sessionId,
            UserId = userId,
            ParticipantIndex = 1,
            JoinedAt = DateTime.UtcNow
        });

        for (int i = 1; i <= 4; i++)
        {
            await workoutTypeRepo.AddAsync(new WorkoutSessionWorkoutType
            {
                Id = Guid.NewGuid(),
                WorkoutSessionId = sessionId,
                WorkoutTypeId = $"workout{i}",
                StationIndex = i
            });
        }

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);
        var command = new CancelSessionCommand(sessionId);

        // Act
        var result = await service.CancelSessionAsync(command);

        // Assert
        Assert.Equal(WorkoutSessionStatus.Cancelled, result.Status);
        Assert.NotNull(result.EndedAt);

        // Verify scores were filled (3 rounds × 4 stations = 12 scores)
        var scores = await scoreRepo.GetByParticipantIdAsync(participantId);
        Assert.Equal(12, scores.Count);
        Assert.All(scores, s => Assert.Equal(0, s.Score));
    }

    [Fact]
    public async Task CompleteSessionAsync_ActiveSession_TransitionsToCompleted()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);
        var command = new CompleteSessionCommand(sessionId);

        // Act
        var result = await service.CompleteSessionAsync(command);

        // Assert
        Assert.Equal(WorkoutSessionStatus.Completed, result.Status);
        Assert.NotNull(result.EndedAt);
    }

    [Fact]
    public async Task GetActiveSessionByGroupIdAsync_ActiveSessionExists_ReturnsSession()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);

        // Act
        var result = await service.GetActiveSessionByGroupIdAsync(groupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.Id);
        Assert.Equal(WorkoutSessionStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetActiveSessionByGroupIdAsync_NoActiveSession_ReturnsNull()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var groupId = Guid.NewGuid();

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);

        // Act
        var result = await service.GetActiveSessionByGroupIdAsync(groupId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionAssignmentsAsync_ValidSession_ReturnsAssignments()
    {
        // Arrange
        var sessionRepo = new InMemoryWorkoutSessionRepository();
        var groupRepo = new InMemoryGroupRepository();
        var userRepo = new InMemoryUserRepository();
        var workoutTypeRepo = new InMemoryWorkoutSessionWorkoutTypeRepository();
        var participantRepo = new InMemoryWorkoutSessionParticipantRepository();
        var scoreRepo = new InMemoryWorkoutIntervalScoreRepository();

        var sessionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await groupRepo.AddAsync(new Group { Id = groupId, Name = "Test Group" });
        await userRepo.AddAsync(new User { Id = userId, Username = "testuser", Email = "test@test.com" });
        
        var session = new WorkoutSession
        {
            Id = sessionId,
            GroupId = groupId,
            CreatorId = userId,
            SessionDate = DateTime.UtcNow,
            Status = WorkoutSessionStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await sessionRepo.AddAsync(session);

        await participantRepo.AddAsync(new WorkoutSessionParticipant
        {
            Id = participantId,
            WorkoutSessionId = sessionId,
            UserId = userId,
            ParticipantIndex = 1,
            JoinedAt = DateTime.UtcNow
        });

        await workoutTypeRepo.AddAsync(new WorkoutSessionWorkoutType
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = sessionId,
            WorkoutTypeId = "pushups",
            StationIndex = 1
        });

        var service = new WorkoutSessionService(sessionRepo, groupRepo, userRepo, workoutTypeRepo, participantRepo, scoreRepo);

        // Act
        var result = await service.GetSessionAssignmentsAsync(sessionId);

        // Assert
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(WorkoutSessionStatus.Active, result.Status);
        Assert.Single(result.Participants);
        Assert.Equal("testuser", result.Participants[0].UserName);
        Assert.Single(result.Stations);
        Assert.Equal("pushups", result.Stations[0].WorkoutTypeId);
    }
}
