using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IGroupInviteRepository
{
    Task<GroupInvite?> GetByIdAsync(Guid id);
    Task<GroupInvite?> GetByTokenAsync(string token);
    Task<IReadOnlyList<GroupInvite>> GetByGroupIdAsync(Guid groupId);
    Task<IReadOnlyList<GroupInvite>> GetActiveByGroupIdAsync(Guid groupId);
    Task AddAsync(GroupInvite invite);
    Task UpdateAsync(GroupInvite invite);
    Task DeleteAsync(Guid id);
}
