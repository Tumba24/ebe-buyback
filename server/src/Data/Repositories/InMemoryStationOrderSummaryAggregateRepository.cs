using System.Collections.Concurrent;
using EveBuyback.Domain;

namespace Evebuyback.Data;

public class InMemoryStationOrderSummaryAggregateRepository : IStationOrderSummaryAggregateRepository
{
    private static readonly ConcurrentDictionary<string, Dictionary<int, OrderSummary>> _orderSummaryCollectionLookup
        = new ConcurrentDictionary<string, Dictionary<int, OrderSummary>>(StringComparer.InvariantCultureIgnoreCase);

    private static readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);

    private readonly IItemTypeRepository _itemTypeRepository;

    public InMemoryStationOrderSummaryAggregateRepository(IItemTypeRepository itemTypeRepository)
    {
        _itemTypeRepository = itemTypeRepository;
    }

    public async Task<StationOrderSummaryAggregate> Get(Station station)
    {
        if (!_orderSummaryCollectionLookup.TryGetValue(station.Name, out var orderSummaryLookup))
            orderSummaryLookup = new Dictionary<int, OrderSummary>();

        var lookupbyName = await _itemTypeRepository.GetLookupByItemTypeName();

        return new StationOrderSummaryAggregate(
            itemTypeLookup: lookupbyName,
            orderSummaryLookup: orderSummaryLookup,
            station: station
        );
    }

    public async Task<OrderSummary> GetOrderSummary(Station station, string itemTypeName)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();

        if (!itemTypeLookup.TryGetValue(itemTypeName, out var itemType))
            throw new ArgumentException("Item type name not recognized.");

        if (!_orderSummaryCollectionLookup.TryGetValue(station.Name, out var orderSummaryLookup))
            throw new ArgumentException("Station not recognized.");

        if (!orderSummaryLookup.TryGetValue(itemType.Id, out var orderSummary))
            throw new InvalidOperationException("Could not find order summary.");

        return orderSummary;
    }

    public async Task Save(StationOrderSummaryAggregate aggregate)
    {
        await _writeSemaphore.WaitAsync();

        try
        {
            if (!_orderSummaryCollectionLookup.TryGetValue(aggregate.Station.Name, out var orderSummaryLookup))
            {
                orderSummaryLookup = new Dictionary<int, OrderSummary>();
                _orderSummaryCollectionLookup.TryAdd(aggregate.Station.Name, orderSummaryLookup);
            }

            foreach (var orderSummary in aggregate.UpdatedOrderSummaries)
                orderSummaryLookup[orderSummary.Item.Id] = orderSummary;
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }
}