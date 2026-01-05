using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IGroupMembershipRepository
{
    Task<GroupMembership?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<GroupMembership>> GetAllAsync();
    Task<IReadOnlyList<GroupMembership>> GetByGroupIdAsync(Guid groupId);
    Task<IReadOnlyList<GroupMembership>> GetByUserIdAsync(Guid userId);
    Task<GroupMembership?> GetByGroupAndUserAsync(Guid groupId, Guid userId);
    Task AddAsync(GroupMembership membership);
    Task UpdateAsync(GroupMembership membership);
    Task DeleteAsync(Guid id);
}
