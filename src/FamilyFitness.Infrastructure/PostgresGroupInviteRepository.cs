using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresGroupInviteRepository : IGroupInviteRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresGroupInviteRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<GroupInvite?> GetByIdAsync(Guid id)
    {
        return await _context.GroupInvites.FindAsync(id);
    }

    public async Task<GroupInvite?> GetByTokenAsync(string token)
    {
        return await _context.GroupInvites
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<IReadOnlyList<GroupInvite>> GetByGroupIdAsync(Guid groupId)
    {
        return await _context.GroupInvites
            .Where(i => i.GroupId == groupId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GroupInvite>> GetActiveByGroupIdAsync(Guid groupId)
    {
        return await _context.GroupInvites
            .Where(i => i.GroupId == groupId && i.IsActive)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(GroupInvite invite)
    {
        await _context.GroupInvites.AddAsync(invite);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(GroupInvite invite)
    {
        _context.GroupInvites.Update(invite);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var invite = await _context.GroupInvites.FindAsync(id);
        if (invite != null)
        {
            _context.GroupInvites.Remove(invite);
            await _context.SaveChangesAsync();
        }
    }
}
