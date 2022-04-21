namespace EveBuyback.Domain;

public class RefinedContractItemAggregate
{
    private readonly IList<object> _domainEvents = new List<object>();
    private readonly IDictionary<int, ItemType> _itemTypeLookup;
    private readonly IEnumerable<MaterialItem> _materialItems;

    public RefinedContractItemAggregate(
        IDictionary<int, ItemType> itemTypeLookup,
        IEnumerable<MaterialItem> materialItems)
    {
        _itemTypeLookup = itemTypeLookup;
        _materialItems = materialItems;
    }

    public IEnumerable<object> DomainEvents => _domainEvents.ToArray();

    public void Refine(IEnumerable<VerifiedContractItem> contractItems, decimal buybackEfficiencyPercentage)
    {
        foreach (var contractItem in contractItems)
        {
            var materialItems = _materialItems
                .Where(i => i.UnrefinedItemTypeId == contractItem.Item.Id);

            if (!materialItems.Any())
            {
                _domainEvents.Add(new MaterialNotRefinedEvent(contractItem));
                continue;
            }

            foreach (var materialItem in materialItems)
            {
                if (!_itemTypeLookup.TryGetValue(materialItem.MaterialItemTypeId, out var itemType))
                {
                    _domainEvents.Add(new RefinementAbortedEvent.InvalidMaterialTypeId(materialItem.MaterialItemTypeId));
                    continue;
                }

                var volume = contractItem.Volume * ((decimal)materialItem.Quantity / contractItem.Item.PortionSize);
                volume = volume * (buybackEfficiencyPercentage / 100);

                _domainEvents.Add(new MaterialRefinedEvent(itemType, (int)Math.Floor(volume)));
            }
        }
    }
}