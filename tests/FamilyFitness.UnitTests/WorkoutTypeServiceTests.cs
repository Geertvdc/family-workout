using FamilyFitness.Application;
using FamilyFitness.Domain;

namespace FamilyFitness.UnitTests;

public class WorkoutTypeServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_ReturnsCreatedDto()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        var idGenerator = new FixedIdGenerator("test-id");
        var service = new WorkoutTypeService(repository, idGenerator);
        var command = new CreateWorkoutTypeCommand(
            "Push-ups",
            "Upper body exercise"
        );

        // Act
        var result = await service.CreateAsync(command);

        // Assert
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Push-ups", result.Name);
        Assert.Equal("Upper body exercise", result.Description);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType(
            "id1",
            "Push-ups",
            null
        ));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id2"));
        var command = new CreateWorkoutTypeCommand(
            "push-ups", // Different case
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(command)
        );
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id1"));
        var command = new UpdateWorkoutTypeCommand(
            "non-existing-id",
            "Some Name",
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAsync(command)
        );
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllWorkoutTypes()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", null));
        await repository.AddAsync(new WorkoutType("id2", "Squats", "Leg exercise"));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id3"));

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, wt => wt.Name == "Push-ups");
        Assert.Contains(result, wt => wt.Name == "Squats");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", "Description"));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id2"));

        // Act
        var result = await service.GetByIdAsync("id1");

        // Assert
        Assert.Equal("id1", result.Id);
        Assert.Equal("Push-ups", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id1"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync("non-existing")
        );
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesWorkoutType()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", null));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id2"));

        // Act
        await service.DeleteAsync("id1");

        // Assert
        var all = await repository.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id1"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.DeleteAsync("non-existing")
        );
    }

    [Fact]
    public async Task UpdateAsync_ValidCommand_ReturnsUpdatedDto()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", "Old desc"));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id2"));
        var command = new UpdateWorkoutTypeCommand(
            "id1",
            "Modified Push-ups",
            "New description"
        );

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.Equal("id1", result.Id);
        Assert.Equal("Modified Push-ups", result.Name);
        Assert.Equal("New description", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameDifferentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", null));
        await repository.AddAsync(new WorkoutType("id2", "Squats", null));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id3"));
        var command = new UpdateWorkoutTypeCommand(
            "id2",
            "Push-ups", // Name already used by id1
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(command)
        );
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SameNameSameId_Succeeds()
    {
        // Arrange
        var repository = new InMemoryWorkoutTypeRepository();
        await repository.AddAsync(new WorkoutType("id1", "Push-ups", "Old desc"));

        var service = new WorkoutTypeService(repository, new FixedIdGenerator("id2"));
        var command = new UpdateWorkoutTypeCommand(
            "id1",
            "Push-ups", // Same name, same id - should be allowed
            "New description"
        );

        // Act
        var result = await service.UpdateAsync(command);

        // Assert
        Assert.Equal("Push-ups", result.Name);
        Assert.Equal("New description", result.Description);
    }
}
