using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class FamilyFitnessDbContext : DbContext
{
    public FamilyFitnessDbContext(DbContextOptions<FamilyFitnessDbContext> options) 
        : base(options)
    {
    }

    public DbSet<WorkoutTypeEntity> WorkoutTypes => Set<WorkoutTypeEntity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionWorkoutType> WorkoutSessionWorkoutTypes => Set<WorkoutSessionWorkoutType>();
    public DbSet<WorkoutSessionParticipant> WorkoutSessionParticipants => Set<WorkoutSessionParticipant>();
    public DbSet<WorkoutIntervalScore> WorkoutIntervalScores => Set<WorkoutIntervalScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureWorkoutType(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureGroup(modelBuilder);
        ConfigureGroupMembership(modelBuilder);
        ConfigureWorkoutSession(modelBuilder);
        ConfigureWorkoutSessionWorkoutType(modelBuilder);
        ConfigureWorkoutSessionParticipant(modelBuilder);
        ConfigureWorkoutIntervalScore(modelBuilder);
    }

    private void ConfigureWorkoutType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutTypeEntity>(entity =>
        {
            entity.ToTable("workout_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsRequired();
            
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.Username)
                .IsUnique();
            
            entity.HasIndex(e => e.Email)
                .IsUnique();
        });
    }

    private void ConfigureGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });
    }

    private void ConfigureGroupMembership(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroupMembership>(entity =>
        {
            entity.ToTable("group_memberships");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Role)
                .HasMaxLength(50);
            
            entity.Property(e => e.JoinedAt)
                .IsRequired();

            // Unique constraint: a user can only be a member of a group once
            entity.HasIndex(e => new { e.GroupId, e.UserId })
                .IsUnique();

            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany(g => g.GroupMemberships)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureWorkoutSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("workout_sessions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SessionDate)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Index on session date for querying
            entity.HasIndex(e => new { e.GroupId, e.SessionDate });

            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany(g => g.WorkoutSessions)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureWorkoutSessionWorkoutType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutSessionWorkoutType>(entity =>
        {
            entity.ToTable("workout_session_workout_types");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.StationIndex)
                .IsRequired();

            // Unique constraint: each session has exactly one workout type per station
            entity.HasIndex(e => new { e.WorkoutSessionId, e.StationIndex })
                .IsUnique();

            // Check constraint: StationIndex must be between 1 and 4
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_WorkoutSessionWorkoutType_StationIndex",
                "\"StationIndex\" >= 1 AND \"StationIndex\" <= 4"));

            // Relationships
            entity.HasOne(e => e.WorkoutSession)
                .WithMany(ws => ws.WorkoutSessionWorkoutTypes)
                .HasForeignKey(e => e.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureWorkoutSessionParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutSessionParticipant>(entity =>
        {
            entity.ToTable("workout_session_participants");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ParticipantIndex)
                .IsRequired();
            
            entity.Property(e => e.JoinedAt)
                .IsRequired();

            // Unique constraint: one session per user per group per day
            // This is enforced through unique constraint on WorkoutSessionId + UserId
            entity.HasIndex(e => new { e.WorkoutSessionId, e.UserId })
                .IsUnique();

            // Unique constraint: participant index is unique within a session
            entity.HasIndex(e => new { e.WorkoutSessionId, e.ParticipantIndex })
                .IsUnique();

            // Relationships
            entity.HasOne(e => e.WorkoutSession)
                .WithMany(ws => ws.Participants)
                .HasForeignKey(e => e.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.WorkoutSessionParticipants)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureWorkoutIntervalScore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkoutIntervalScore>(entity =>
        {
            entity.ToTable("workout_interval_scores");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RoundNumber)
                .IsRequired();
            
            entity.Property(e => e.StationIndex)
                .IsRequired();
            
            entity.Property(e => e.WorkoutTypeId)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Score)
                .IsRequired();
            
            entity.Property(e => e.Weight)
                .HasPrecision(10, 2);
            
            entity.Property(e => e.RecordedAt)
                .IsRequired();

            // Unique constraint: one score per participant per round per station
            entity.HasIndex(e => new { e.ParticipantId, e.RoundNumber, e.StationIndex })
                .IsUnique();

            // Index for querying scores by workout type for progression tracking
            entity.HasIndex(e => new { e.ParticipantId, e.WorkoutTypeId, e.RecordedAt });

            // Check constraints
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_WorkoutIntervalScore_RoundNumber",
                "\"RoundNumber\" >= 1 AND \"RoundNumber\" <= 3"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_WorkoutIntervalScore_StationIndex",
                "\"StationIndex\" >= 1 AND \"StationIndex\" <= 4"));

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_WorkoutIntervalScore_Score",
                "\"Score\" >= 0"));

            // Relationships
            entity.HasOne(e => e.Participant)
                .WithMany(p => p.IntervalScores)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public class WorkoutTypeEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
