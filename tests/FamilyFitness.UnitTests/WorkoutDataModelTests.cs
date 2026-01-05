using FamilyFitness.Domain;

namespace FamilyFitness.UnitTests;

public class WorkoutDataModelTests
{
    [Fact]
    public void User_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("test@example.com", user.Email);
        Assert.NotNull(user.GroupMemberships);
        Assert.NotNull(user.WorkoutSessionParticipants);
    }

    [Fact]
    public void Group_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = "Family Group",
            Description = "Our family workout group",
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, group.Id);
        Assert.Equal("Family Group", group.Name);
        Assert.Equal("Our family workout group", group.Description);
        Assert.NotNull(group.GroupMemberships);
        Assert.NotNull(group.WorkoutSessions);
    }

    [Fact]
    public void WorkoutSession_CanBeCreated_WithAllStatuses()
    {
        // Arrange & Act
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            CreatorId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow.Date,
            Status = WorkoutSessionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(WorkoutSessionStatus.Pending, session.Status);
        Assert.NotNull(session.WorkoutSessionWorkoutTypes);
        Assert.NotNull(session.Participants);

        // Test all status values
        session.Status = WorkoutSessionStatus.Active;
        Assert.Equal(WorkoutSessionStatus.Active, session.Status);
        
        session.Status = WorkoutSessionStatus.Completed;
        Assert.Equal(WorkoutSessionStatus.Completed, session.Status);
        
        session.Status = WorkoutSessionStatus.Cancelled;
        Assert.Equal(WorkoutSessionStatus.Cancelled, session.Status);
    }

    [Fact]
    public void WorkoutIntervalScore_CanHave_ZeroScore()
    {
        // Arrange & Act
        var score = new WorkoutIntervalScore
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            RoundNumber = 1,
            StationIndex = 1,
            WorkoutTypeId = "pushups",
            Score = 0, // Zero is valid
            RecordedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(0, score.Score);
        Assert.Equal("pushups", score.WorkoutTypeId);
    }

    [Fact]
    public void WorkoutSessionWorkoutType_HasValidStationRange()
    {
        // Arrange
        var workoutType = new WorkoutSessionWorkoutType
        {
            Id = Guid.NewGuid(),
            WorkoutSessionId = Guid.NewGuid(),
            WorkoutTypeId = "pushups"
        };

        // Act & Assert - Valid station indexes 1-4
        for (int i = 1; i <= 4; i++)
        {
            workoutType.StationIndex = i;
            Assert.InRange(workoutType.StationIndex, 1, 4);
        }
    }

    [Fact]
    public void WorkoutIntervalScore_HasValidRoundRange()
    {
        // Arrange
        var score = new WorkoutIntervalScore
        {
            Id = Guid.NewGuid(),
            ParticipantId = Guid.NewGuid(),
            StationIndex = 1,
            WorkoutTypeId = "situps",
            Score = 10,
            RecordedAt = DateTime.UtcNow
        };

        // Act & Assert - Valid round numbers 1-3
        for (int i = 1; i <= 3; i++)
        {
            score.RoundNumber = i;
            Assert.InRange(score.RoundNumber, 1, 3);
        }
    }

    [Fact]
    public void GroupMembership_CanBeCreated_WithOptionalRole()
    {
        // Arrange & Act
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            JoinedAt = DateTime.UtcNow,
            Role = "admin"
        };

        // Assert
        Assert.Equal("admin", membership.Role);

        // Null role should also be valid
        membership.Role = null;
        Assert.Null(membership.Role);
    }

    [Fact]
    public void WorkoutSessionStatus_HasCorrectEnumValues()
    {
        // Assert enum values
        Assert.Equal(0, (int)WorkoutSessionStatus.Pending);
        Assert.Equal(1, (int)WorkoutSessionStatus.Active);
        Assert.Equal(2, (int)WorkoutSessionStatus.Completed);
        Assert.Equal(3, (int)WorkoutSessionStatus.Cancelled);
    }

    [Fact]
    public void WorkoutIntervalScore_IncludesWorkoutTypeId_ForProgressionTracking()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var pushupScores = new List<WorkoutIntervalScore>();

        // Act - Create multiple scores for the same workout type
        for (int round = 1; round <= 3; round++)
        {
            var score = new WorkoutIntervalScore
            {
                Id = Guid.NewGuid(),
                ParticipantId = participantId,
                RoundNumber = round,
                StationIndex = round, // Different stations due to rotation
                WorkoutTypeId = "pushups", // Same workout type
                Score = 10 + round * 5,
                RecordedAt = DateTime.UtcNow.AddMinutes(round * 5)
            };
            pushupScores.Add(score);
        }

        // Assert - Can easily filter scores by workout type
        var allPushupScores = pushupScores.Where(s => s.WorkoutTypeId == "pushups").ToList();
        Assert.Equal(3, allPushupScores.Count);
        Assert.All(allPushupScores, s => Assert.Equal("pushups", s.WorkoutTypeId));
        Assert.All(allPushupScores, s => Assert.Equal(participantId, s.ParticipantId));
        
        // Can track progression over rounds
        Assert.Equal(15, allPushupScores[0].Score);
        Assert.Equal(20, allPushupScores[1].Score);
        Assert.Equal(25, allPushupScores[2].Score);
    }
}
