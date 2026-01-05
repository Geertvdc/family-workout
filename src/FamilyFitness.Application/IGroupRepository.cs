using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Group>> GetAllAsync();
    Task AddAsync(Group group);
    Task UpdateAsync(Group group);
    Task DeleteAsync(Guid id);
}
