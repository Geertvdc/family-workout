using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresWorkoutSessionParticipantRepository : IWorkoutSessionParticipantRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutSessionParticipantRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutSessionParticipant?> GetByIdAsync(Guid id)
    {
        return await _context.WorkoutSessionParticipants.FindAsync(id);
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipant>> GetAllAsync()
    {
        return await _context.WorkoutSessionParticipants
            .OrderBy(p => p.ParticipantIndex)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipant>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        return await _context.WorkoutSessionParticipants
            .Where(p => p.WorkoutSessionId == workoutSessionId)
            .OrderBy(p => p.ParticipantIndex)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSessionParticipant>> GetByUserIdAsync(Guid userId)
    {
        return await _context.WorkoutSessionParticipants
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(WorkoutSessionParticipant participant)
    {
        await _context.WorkoutSessionParticipants.AddAsync(participant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutSessionParticipant participant)
    {
        _context.WorkoutSessionParticipants.Update(participant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var participant = await _context.WorkoutSessionParticipants.FindAsync(id);
        if (participant != null)
        {
            _context.WorkoutSessionParticipants.Remove(participant);
            await _context.SaveChangesAsync();
        }
    }
}
