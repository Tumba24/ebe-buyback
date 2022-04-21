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
        var refinedEvents = new List<MaterialRefinedEvent>();

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

                var remainder = contractItem.Volume % contractItem.Item.PortionSize;
                var refineableVolume = contractItem.Volume - remainder;

                var volume = refineableVolume * ((decimal)materialItem.Quantity / contractItem.Item.PortionSize);
                volume = volume * (buybackEfficiencyPercentage / 100);

                refinedEvents.Add(new MaterialRefinedEvent(itemType, (int)Math.Floor(volume)));
            }
        }

        var aggregatedEvents = refinedEvents
            .GroupBy(i => i.Item.Id)
            .Select(g => new MaterialRefinedEvent(g.First().Item, g.Sum(i => i.Volume)));

        foreach (var refinedEvent in aggregatedEvents)
            _domainEvents.Add(refinedEvent);
    }
}