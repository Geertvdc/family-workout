using FamilyFitness.Application;
using FamilyFitness.Domain;
using Microsoft.Azure.Cosmos;

namespace FamilyFitness.Infrastructure;

public class CosmosWorkoutTypeRepository : IWorkoutTypeRepository
{
    private readonly Container _container;
    private const string PartitionKeyValue = "WorkoutTypes";

    public CosmosWorkoutTypeRepository(Container container)
    {
        _container = container;
    }

    public async Task<WorkoutType?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<WorkoutTypeDocument>(
                id,
                new PartitionKey(PartitionKeyValue)
            );
            return ToEntity(response.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<WorkoutType>> GetAllAsync()
    {
        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c WHERE c.PartitionKey = @partitionKey")
            .WithParameter("@partitionKey", PartitionKeyValue);

        var query = _container.GetItemQueryIterator<WorkoutTypeDocument>(queryDefinition);

        var results = new List<WorkoutType>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.Select(ToEntity));
        }

        return results;
    }

    public async Task AddAsync(WorkoutType workoutType)
    {
        var document = ToDocument(workoutType);
        await _container.CreateItemAsync(document, new PartitionKey(PartitionKeyValue));
    }

    public async Task UpdateAsync(WorkoutType workoutType)
    {
        var document = ToDocument(workoutType);
        await _container.ReplaceItemAsync(
            document,
            document.id,
            new PartitionKey(PartitionKeyValue)
        );
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<WorkoutTypeDocument>(
            id,
            new PartitionKey(PartitionKeyValue)
        );
    }

    private static WorkoutType ToEntity(WorkoutTypeDocument document)
    {
        return new WorkoutType(
            document.id,
            document.Name,
            document.Description,
            document.EstimatedDurationMinutes,
            Enum.Parse<Intensity>(document.Intensity)
        );
    }

    private static WorkoutTypeDocument ToDocument(WorkoutType entity)
    {
        return new WorkoutTypeDocument
        {
            id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
            Intensity = entity.Intensity.ToString(),
            PartitionKey = PartitionKeyValue
        };
    }
}
