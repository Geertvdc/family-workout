using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}
