using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(
    string StationName, 
    IEnumerable<BuybackItem> Items, 
    bool ShouldCalculateBuybackAfterRefinement, 
    decimal BuybackTaxPercentage) : IRequest<BackendQueryResult>;

public record BuybackItem(string ItemTypeName, int Volume);

public record BackendQueryResult(decimal BuybackAmount, bool OK, string errorMessage);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, BackendQueryResult>
{
    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    private readonly IItemTypeRepository _itemTypeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IRefinementRepository _refinementRepository;
    private readonly InMemoryStationOrderSummaryAggregateRepository _stationOrderSummaryRepository;

    public BuybackQueryHandler(
        IItemTypeRepository itemTypeRepository,
        IOrderRepository orderRepository,
        IRefinementRepository refinementRepository,
        IStationOrderSummaryAggregateRepository stationOrderSummaryRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _orderRepository = orderRepository;
        _refinementRepository = refinementRepository;
        _stationOrderSummaryRepository = (InMemoryStationOrderSummaryAggregateRepository)stationOrderSummaryRepository;
    }

    public async Task<BackendQueryResult> Handle(BuybackQuery query, CancellationToken token)
    {
        if (!_stationLookup.TryGetValue(query.StationName, out var station))
            return new BackendQueryResult(0, false, "Invalid station. Station not recognized.");

        var buybackItems = await GetBuybackItems(query);
        token.ThrowIfCancellationRequested();

        var aggregate = await _stationOrderSummaryRepository.Get(station);
        token.ThrowIfCancellationRequested();

        await RefreshOrderSummaries(station, buybackItems, aggregate, token);
        token.ThrowIfCancellationRequested();

        await _stationOrderSummaryRepository.Save(aggregate);
        token.ThrowIfCancellationRequested();

        var buybackAmount = 0.0m;
        var itemTypeLookup = await _itemTypeRepository.GetLookupById();
        token.ThrowIfCancellationRequested();

        foreach (var item in buybackItems)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeId, out var itemType))
                continue;

            var orderSummary = await _stationOrderSummaryRepository.GetOrderSummary(station, itemType.Name);
            buybackAmount += (orderSummary.Price * item.Volume);
        }

        var tax = buybackAmount * (query.BuybackTaxPercentage / 100);


        return new BackendQueryResult(buybackAmount - tax, true, string.Empty); 
    }

    private async Task<IEnumerable<Domain.BuybackItem>> GetBuybackItems(BuybackQuery query)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByName();

        var buybackItems = new List<Domain.BuybackItem>();

        foreach (var item in query.Items)
        {
            if (itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
                buybackItems.Add(new Domain.BuybackItem(itemType.Id, item.Volume));
        }

        if (query.ShouldCalculateBuybackAfterRefinement)
        {
            var reifinedItems = new List<Domain.BuybackItem>();
            foreach (var item in buybackItems)
                reifinedItems.AddRange(await _refinementRepository.GetRefinedItems(item));

            return reifinedItems;
        }

        return buybackItems;
    }

    private async Task RefreshOrderSummaries(
        Station station,
        IEnumerable<Domain.BuybackItem> items,
        StationOrderSummaryAggregate aggregate,
        CancellationToken token)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupById();

        var currentDateTime = DateTime.UtcNow;

        foreach (var item in items)
        {
            if (itemTypeLookup.TryGetValue(item.ItemTypeId, out var itemType))
                aggregate.RefreshOrderSummary(itemType.Name, item.Volume, currentDateTime);
        }

        var invalidEvents = aggregate.DomainEvents?
            .Where(e => e is InvalidOrderSummaryNoticedEvent)?
            .Select(e => e as InvalidOrderSummaryNoticedEvent) ?? Enumerable.Empty<InvalidOrderSummaryNoticedEvent>();

        foreach (var invalidEvent in invalidEvents)
        {
            if (invalidEvent == null)
                throw new InvalidOperationException();

            var orders = await _orderRepository.GetOrders(station, invalidEvent.Item.Id, currentDateTime, token);

            token.ThrowIfCancellationRequested();
            
            aggregate.UpdateOrderSummary(
                invalidEvent.Item,
                invalidEvent.Volume,
                orders,
                currentDateTime);
        }
    }
}