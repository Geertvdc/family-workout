using FamilyFitness.Application;

namespace FamilyFitness.Infrastructure;

public class GuidIdGenerator : IIdGenerator
{
    public string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }
}
