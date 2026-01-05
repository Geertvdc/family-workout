using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.EntityFrameworkCore;

namespace FamilyFitness.Infrastructure;

public class PostgresGroupMembershipRepository : IGroupMembershipRepository
{
    private readonly FamilyFitnessDbContext _context;

    public PostgresGroupMembershipRepository(FamilyFitnessDbContext context)
    {
        _context = context;
    }

    public async Task<GroupMembership?> GetByIdAsync(Guid id)
    {
        return await _context.GroupMemberships.FindAsync(id);
    }

    public async Task<IReadOnlyList<GroupMembership>> GetAllAsync()
    {
        return await _context.GroupMemberships.ToListAsync();
    }

    public async Task<IReadOnlyList<GroupMembership>> GetByGroupIdAsync(Guid groupId)
    {
        return await _context.GroupMemberships
            .Where(gm => gm.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<GroupMembership>> GetByUserIdAsync(Guid userId)
    {
        return await _context.GroupMemberships
            .Where(gm => gm.UserId == userId)
            .ToListAsync();
    }

    public async Task<GroupMembership?> GetByGroupAndUserAsync(Guid groupId, Guid userId)
    {
        return await _context.GroupMemberships
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task AddAsync(GroupMembership membership)
    {
        await _context.GroupMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(GroupMembership membership)
    {
        _context.GroupMemberships.Update(membership);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var membership = await _context.GroupMemberships.FindAsync(id);
        if (membership != null)
        {
            _context.GroupMemberships.Remove(membership);
            await _context.SaveChangesAsync();
        }
    }
}
