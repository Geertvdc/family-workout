using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresWorkoutSessionWorkoutTypeRepository : IWorkoutSessionWorkoutTypeRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutSessionWorkoutTypeRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutSessionWorkoutType?> GetByIdAsync(Guid id)
    {
        return await _context.WorkoutSessionWorkoutTypes.FindAsync(id);
    }

    public async Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetAllAsync()
    {
        return await _context.WorkoutSessionWorkoutTypes
            .OrderBy(wswt => wswt.StationIndex)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutSessionWorkoutType>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        return await _context.WorkoutSessionWorkoutTypes
            .Where(wswt => wswt.WorkoutSessionId == workoutSessionId)
            .OrderBy(wswt => wswt.StationIndex)
            .ToListAsync();
    }

    public async Task AddAsync(WorkoutSessionWorkoutType workoutSessionWorkoutType)
    {
        await _context.WorkoutSessionWorkoutTypes.AddAsync(workoutSessionWorkoutType);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutSessionWorkoutType workoutSessionWorkoutType)
    {
        _context.WorkoutSessionWorkoutTypes.Update(workoutSessionWorkoutType);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _context.WorkoutSessionWorkoutTypes.FindAsync(id);
        if (item != null)
        {
            _context.WorkoutSessionWorkoutTypes.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
