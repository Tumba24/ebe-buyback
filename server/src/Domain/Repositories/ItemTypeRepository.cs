namespace EveBuyback.Domain;

public interface IItemTypeRepository
{
    Task<IDictionary<int, ItemType>> GetLookupById();
    Task<IDictionary<string, ItemType>> GetLookupByName();
}