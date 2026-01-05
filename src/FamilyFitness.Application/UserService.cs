using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> CreateAsync(CreateUserCommand command)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(command.Username))
        {
            throw new ArgumentException("Username is required.", nameof(command.Username));
        }

        if (command.Username.Length > 100)
        {
            throw new ArgumentException("Username must be 100 characters or less.", nameof(command.Username));
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException("Email is required.", nameof(command.Email));
        }

        if (command.Email.Length > 255)
        {
            throw new ArgumentException("Email must be 255 characters or less.", nameof(command.Email));
        }

        // Check for duplicate username
        var existingByUsername = await _repository.GetByUsernameAsync(command.Username);
        if (existingByUsername != null)
        {
            throw new InvalidOperationException($"A user with username '{command.Username}' already exists.");
        }

        // Check for duplicate email
        var existingByEmail = await _repository.GetByEmailAsync(command.Email);
        if (existingByEmail != null)
        {
            throw new InvalidOperationException($"A user with email '{command.Email}' already exists.");
        }

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = command.Username.Trim(),
            Email = command.Email.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(user);

        return ToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        var users = await _repository.GetAllAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found.");
        }

        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(UpdateUserCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"User with ID '{command.Id}' not found.");
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(command.Username))
        {
            throw new ArgumentException("Username is required.", nameof(command.Username));
        }

        if (command.Username.Length > 100)
        {
            throw new ArgumentException("Username must be 100 characters or less.", nameof(command.Username));
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException("Email is required.", nameof(command.Email));
        }

        if (command.Email.Length > 255)
        {
            throw new ArgumentException("Email must be 255 characters or less.", nameof(command.Email));
        }

        // Check for duplicate username (excluding current user)
        var existingByUsername = await _repository.GetByUsernameAsync(command.Username);
        if (existingByUsername != null && existingByUsername.Id != command.Id)
        {
            throw new InvalidOperationException($"A user with username '{command.Username}' already exists.");
        }

        // Check for duplicate email (excluding current user)
        var existingByEmail = await _repository.GetByEmailAsync(command.Email);
        if (existingByEmail != null && existingByEmail.Id != command.Id)
        {
            throw new InvalidOperationException($"A user with email '{command.Email}' already exists.");
        }

        // Update user
        existing.Username = command.Username.Trim();
        existing.Email = command.Email.Trim();

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.CreatedAt
        );
    }
}
