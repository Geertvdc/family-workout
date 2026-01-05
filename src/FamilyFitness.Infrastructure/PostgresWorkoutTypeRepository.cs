using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresWorkoutTypeRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutType?> GetByIdAsync(string id)
    {
        var entity = await _context.WorkoutTypes.FindAsync(id);
        return entity == null ? null : ToEntity(entity);
    }

    public async Task<IReadOnlyList<WorkoutType>> GetAllAsync()
    {
        var entities = await _context.WorkoutTypes.ToListAsync();
        return entities.Select(ToEntity).ToList();
    }

    public async Task AddAsync(WorkoutType workoutType)
    {
        var entity = ToDbEntity(workoutType);
        await _context.WorkoutTypes.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkoutType workoutType)
    {
        var entity = ToDbEntity(workoutType);
        _context.WorkoutTypes.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.WorkoutTypes.FindAsync(id);
        if (entity != null)
        {
            _context.WorkoutTypes.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static WorkoutType ToEntity(WorkoutTypeEntity dbEntity)
    {
        return new WorkoutType(
            dbEntity.Id,
            dbEntity.Name,
            dbEntity.Description,
            dbEntity.EstimatedDurationMinutes,
            Enum.Parse<Intensity>(dbEntity.Intensity)
        );
    }

    private static WorkoutTypeEntity ToDbEntity(WorkoutType entity)
    {
        return new WorkoutTypeEntity
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
            Intensity = entity.Intensity.ToString()
        };
    }
}
