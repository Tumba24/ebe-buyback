namespace EveBuyback.Domain;

public interface IItemTypeRepository
{
    Task<IDictionary<int, ItemType>> GetLookupByItemTypeId();
    Task<IDictionary<string, ItemType>> GetLookupByItemTypeName();
}