using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresWorkoutSessionRepository : IWorkoutSessionRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutSessionRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutSession?> GetByIdAsync(Guid id)
    {
        return await _context.WorkoutSessions.FindAsync(id);
    }

    public async Task<IReadOnlyList<WorkoutSession>> GetAllAsync()
    {
        return await _context.WorkoutSessions
            .OrderByDescending(ws => ws.SessionDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSession>> GetByGroupIdAsync(Guid groupId)
    {
        return await _context.WorkoutSessions
            .Where(ws => ws.GroupId == groupId)
            .OrderByDescending(ws => ws.SessionDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSession>> GetByCreatorIdAsync(Guid creatorId)
    {
        return await _context.WorkoutSessions
            .Where(ws => ws.CreatorId == creatorId)
            .OrderByDescending(ws => ws.SessionDate)
            .ToListAsync();
    }

    public async Task AddAsync(WorkoutSession session)
    {
        await _context.WorkoutSessions.AddAsync(session);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutSession session)
    {
        _context.WorkoutSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var session = await _context.WorkoutSessions.FindAsync(id);
        if (session != null)
        {
            _context.WorkoutSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}
