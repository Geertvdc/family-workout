using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresGroupRepository : IGroupRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresGroupRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<Group?> GetByIdAsync(Guid id)
    {
        return await _context.Groups.FindAsync(id);
    }

    public async Task<IReadOnlyList<Group>> GetAllAsync()
    {
        return await _context.Groups.ToListAsync();
    }

    public async Task<IReadOnlyList<Group>> GetByUserMembershipAsync(Guid userId)
    {
        return await _context.Groups
            .Where(g => g.GroupMemberships.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Group>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Groups
            .Where(g => g.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task AddAsync(Group group)
    {
        await _context.Groups.AddAsync(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }
}
