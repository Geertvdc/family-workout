using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Group>> GetAllAsync();
    Task<IReadOnlyList<Group>> GetByUserMembershipAsync(Guid userId);
    Task<IReadOnlyList<Group>> GetByOwnerIdAsync(Guid ownerId);
    Task AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(Guid id);
}
