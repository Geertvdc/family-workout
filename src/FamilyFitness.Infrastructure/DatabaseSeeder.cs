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

        // Create users with more realistic-looking test GUIDs
        var geert = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username = "geert",
            Email = "geert@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var patty = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Username = "patty",
            Email = "patty@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var lauren = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Username = "lauren",
            Email = "lauren@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        var amber = new User
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Username = "amber",
            Email = "amber@vdcruijsen.net",
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(geert, patty, lauren, amber);

        // Create group with a distinct test GUID
        var cruijsjes = new Group
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "cruijsjes",
            Description = "The Cruijsen family",
            CreatedAt = DateTime.UtcNow
        };

        await context.Groups.AddAsync(cruijsjes);

        // Save users and groups before creating relationships
        await context.SaveChangesAsync();

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
