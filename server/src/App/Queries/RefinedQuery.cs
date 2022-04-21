using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record RefinedQuery(IEnumerable<RefinedQueryItem> Items, decimal BuybackEfficiencyPercentage) : IRequest<RefinedQueryResult>;

public record RefinedQueryItem(string ItemTypeName, int Volume);

public record RefinedQueryResult(IEnumerable<RefinedQueryItem> Items, bool OK, string errorMessage);

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
        var refinedItems = new List<RefinedQueryItem>();

        var contractItems = await GetContractItems(query);
        token.ThrowIfCancellationRequested();

        var aggregate = await _refinementRepository.Get(contractItems.Select(i => i.Item.Id));
        token.ThrowIfCancellationRequested();

        aggregate.Refine(contractItems, query.BuybackEfficiencyPercentage);

        var errorEvents = aggregate.DomainEvents
            .Select(e => e as IErrorEvent)
            .Where(e => e != null);

        if (errorEvents.Any())
            return new RefinedQueryResult(refinedItems, false, string.Join("\n", errorEvents));

        var refinedEvents = aggregate.DomainEvents
            .Select(e => e as MaterialRefinedEvent)
            .Where(e => e != null);

        foreach (var refinedEvent in refinedEvents)
        {
            if (refinedEvent is null)
                continue;
            
            refinedItems.Add(new RefinedQueryItem(refinedEvent.Item.Name, refinedEvent.Volume));
        }

        var notRefinedEvents = aggregate.DomainEvents
            .Select(e => e as MaterialNotRefinedEvent)
            .Where(e => e != null);

        foreach (var notRefinedEvent in notRefinedEvents)
        {
            if (notRefinedEvent is null)
                continue;
            
            refinedItems.Add(new RefinedQueryItem(notRefinedEvent.Item.ItemTypeName, notRefinedEvent.Item.Volume));
        }

        return new RefinedQueryResult(refinedItems, true, string.Empty);
    }

    private async Task<IEnumerable<VerifiedContractItem>> GetContractItems(RefinedQuery query)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();

        var contractItems = new List<VerifiedContractItem>();

        foreach (var item in query.Items)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
                itemType = new ItemType(0, item.ItemTypeName, 0);

            contractItems.Add(new VerifiedContractItem(itemType, item.Volume));
        }

        return contractItems;
    }
}