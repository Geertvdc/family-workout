using FamilyFitness.Domain;

namespace FamilyFitness.Application;

public class GroupService
{
    private readonly IGroupRepository _repository;

    public GroupService(IGroupRepository repository)
    {
        _repository = repository;
    }

    public async Task<GroupDto> CreateAsync(CreateGroupCommand command)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required.", nameof(command.Name));
        }

        if (command.Name.Length > 200)
        {
            throw new ArgumentException("Name must be 200 characters or less.", nameof(command.Name));
        }

        // Validate description
        if (command.Description != null && command.Description.Length > 1000)
        {
            throw new ArgumentException("Description must be 1000 characters or less.", nameof(command.Description));
        }

        // Create group
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = command.Description?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(group);

        return ToDto(group);
    }

    public async Task<IReadOnlyList<GroupDto>> GetAllAsync()
    {
        var groups = await _repository.GetAllAsync();
        return groups.Select(ToDto).ToList();
    }

    public async Task<GroupDto> GetByIdAsync(Guid id)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
        {
            throw new KeyNotFoundException($"Group with ID '{id}' not found.");
        }

        return ToDto(group);
    }

    public async Task<GroupDto> UpdateAsync(UpdateGroupCommand command)
    {
        // Check if exists
        var existing = await _repository.GetByIdAsync(command.Id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group with ID '{command.Id}' not found.");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required.", nameof(command.Name));
        }

        if (command.Name.Length > 200)
        {
            throw new ArgumentException("Name must be 200 characters or less.", nameof(command.Name));
        }

        // Validate description
        if (command.Description != null && command.Description.Length > 1000)
        {
            throw new ArgumentException("Description must be 1000 characters or less.", nameof(command.Description));
        }

        // Update group
        existing.Name = command.Name.Trim();
        existing.Description = command.Description?.Trim();

        await _repository.UpdateAsync(existing);

        return ToDto(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Group with ID '{id}' not found.");
        }

        await _repository.DeleteAsync(id);
    }

    private static GroupDto ToDto(Group group)
    {
        return new GroupDto(
            group.Id,
            group.Name,
            group.Description,
            group.CreatedAt
        );
    }
}
