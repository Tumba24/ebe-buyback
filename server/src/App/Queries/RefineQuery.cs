using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record RefinedQuery(IEnumerable<BuybackItem> Items) : IRequest<RefinedQueryResult>;

public record UnrefinedItem(string ItemTypeName, int Volume);

public record RefinedQueryResult(IEnumerable<BuybackItem> Items, bool OK, string errorMessage);

internal class RefinedQueryHandler : IRequestHandler<RefinedQuery, RefinedQueryResult>
{
    private readonly IItemTypeRepository _itemTypeRepository;
    private readonly IRefinedContractItemAggregateRepository _refinementRepository;

    public RefinedQueryHandler(
        IItemTypeRepository itemTypeRepository,
        IRefinedContractItemAggregateRepository refinementRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _refinementRepository = refinementRepository;
    }
    
    public async Task<RefinedQueryResult> Handle(RefinedQuery query, CancellationToken token)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();
        token.ThrowIfCancellationRequested();

        var refinedItems = new List<BuybackItem>();

        foreach (var item in query.Items)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
            {
                return new RefinedQueryResult(
                    refinedItems, 
                    false, 
                    $"Invalid item type '{item.ItemTypeName}'. Item type not recognized.");
            }

            var aggregate = await _refinementRepository.Get(itemType.Id);
            token.ThrowIfCancellationRequested();

            aggregate.Refine(new ContractItem(itemType, item.Volume));

            var errorEvents = aggregate.DomainEvents
                .Select(e => e as IErrorEvent)
                .Where(e => e != null);

            if (errorEvents.Any())
                return new RefinedQueryResult(refinedItems, false, string.Join("\n", errorEvents));

            var refinedEvents = aggregate.DomainEvents
                .Select(e => e as MaterialRefinedEvent)
                .Where(e => e != null);

            if (refinedEvents.Any())
            {
                foreach (var refinedEvent in refinedEvents)
                {
                    if (refinedEvent is null)
                        continue;
                    
                    refinedItems.Add(new BuybackItem(refinedEvent.Item.Name, refinedEvent.Volume));
                }
            }
            else
            {
                refinedItems.Add(new BuybackItem(item.ItemTypeName, item.Volume));
            }
        }

        return new RefinedQueryResult(refinedItems, true, string.Empty);
    }
}