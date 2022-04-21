namespace EveBuyback.Domain;

public interface IRefinedContractItemAggregateRepository
{
    Task<RefinedContractItemAggregate> Get(IEnumerable<int> itemTypeIds);
}