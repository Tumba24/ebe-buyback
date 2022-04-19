namespace EveBuyback.Domain;

public interface IRefinementRepository
{
    Task<IEnumerable<BuybackItem>> GetRefinedItems(BuybackItem item);
}