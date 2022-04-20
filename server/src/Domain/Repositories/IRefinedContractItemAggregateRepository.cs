namespace EveBuyback.Domain;

public interface IRefinedContractItemAggregateRepository
{
    Task<RefinedContractItemAggregate> Get(int itemTypeId);
}