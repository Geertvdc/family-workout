using FamilyFitness.Domain;

namespace FamilyFitness.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(FamilyFitnessDbContext context)
    {
        // Check if data already exists
        if (context.Users.Any() || context.Groups.Any() || context.WorkoutTypes.Any())
        {
            return; // Database already seeded
        }

        // Create users
        var geert = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Username = "geert",
            Email = "geert@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var patty = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Username = "patty",
            Email = "patty@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var lauren = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            Username = "lauren",
            Email = "lauren@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var amber = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
            Username = "amber",
            Email = "amber@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(geert, patty, lauren, amber);

        // Create group
        var cruijsjes = new Group
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "cruijsjes",
            Description = "The Cruijsen family",
            CreatedAt = DateTime.UtcNow
        };

        await context.Groups.AddAsync(cruijsjes);

        // Create group memberships
        var memberships = new[]
        {
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = cruijsjes.Id,
                UserId = geert.Id,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow
            },
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = cruijsjes.Id,
                UserId = patty.Id,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            },
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = cruijsjes.Id,
                UserId = lauren.Id,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            },
            new GroupMembership
            {
                Id = Guid.NewGuid(),
                GroupId = cruijsjes.Id,
                UserId = amber.Id,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            }
        };

        await context.GroupMemberships.AddRangeAsync(memberships);

        // Create workout types
        var workoutTypes = new[]
        {
            new WorkoutTypeEntity
            {
                Id = "pushups",
                Name = "Pushups",
                Description = "Upper body strength exercise"
            },
            new WorkoutTypeEntity
            {
                Id = "plank",
                Name = "Plank",
                Description = "Core stability exercise"
            },
            new WorkoutTypeEntity
            {
                Id = "squats",
                Name = "Squats",
                Description = "Lower body strength exercise"
            },
            new WorkoutTypeEntity
            {
                Id = "situps",
                Name = "Situps",
                Description = "Core strength exercise"
            },
            new WorkoutTypeEntity
            {
                Id = "trampoline-jumps",
                Name = "Trampoline Jumps",
                Description = "Cardio and leg strength exercise"
            }
        };

        await context.WorkoutTypes.AddRangeAsync(workoutTypes);

        await context.SaveChangesAsync();
    }
}
