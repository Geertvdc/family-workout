using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresWorkoutIntervalScoreRepository : IWorkoutIntervalScoreRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutIntervalScoreRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutIntervalScore?> GetByIdAsync(Guid id)
    {
        return await _context.WorkoutIntervalScores.FindAsync(id);
    }

    public async Task<IReadOnlyList<WorkoutIntervalScore>> GetAllAsync()
    {
        return await _context.WorkoutIntervalScores
            .OrderByDescending(s => s.RecordedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScore>> GetByParticipantIdAsync(Guid participantId)
    {
        return await _context.WorkoutIntervalScores
            .Where(s => s.ParticipantId == participantId)
            .OrderBy(s => s.RoundNumber)
            .ThenBy(s => s.StationIndex)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutTypeIdAsync(string workoutTypeId)
    {
        return await _context.WorkoutIntervalScores
            .Where(s => s.WorkoutTypeId == workoutTypeId)
            .OrderByDescending(s => s.RecordedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WorkoutIntervalScore>> GetByWorkoutSessionIdAsync(Guid workoutSessionId)
    {
        return await _context.WorkoutIntervalScores
            .Where(s => s.Participant.WorkoutSessionId == workoutSessionId)
            .OrderBy(s => s.Participant.ParticipantIndex)
            .ThenBy(s => s.RoundNumber)
            .ThenBy(s => s.StationIndex)
            .ToListAsync();
    }

    public async Task AddAsync(WorkoutIntervalScore score)
    {
        await _context.WorkoutIntervalScores.AddAsync(score);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutIntervalScore score)
    {
        _context.WorkoutIntervalScores.Update(score);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var score = await _context.WorkoutIntervalScores.FindAsync(id);
        if (score != null)
        {
            _context.WorkoutIntervalScores.Remove(score);
            await _context.SaveChangesAsync();
        }
    }
}
