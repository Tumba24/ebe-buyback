using System.Collections.Concurrent;
using System.Dynamic;
using EveBuyback.Domain;
using YamlDotNet.Serialization.NamingConventions;

namespace Evebuyback.Data;

public class InMemoryStationOrderSummaryAggregateRepository : IStationOrderSummaryAggregateRepository
{
    private static readonly Lazy<IDictionary<string, int>> _itemTypeIdLookup 
        = new Lazy<IDictionary<string, int>>(GetItemTypeIdLookup, true);

    private static readonly ConcurrentDictionary<string, Dictionary<int, OrderSummary>> _orderSummaryCollectionLookup
        = new ConcurrentDictionary<string, Dictionary<int, OrderSummary>>(StringComparer.InvariantCultureIgnoreCase);

    private static readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);

    public Task<StationOrderSummaryAggregate> Get(Station station)
    {
        if (!_orderSummaryCollectionLookup.TryGetValue(station.Name, out var orderSummaryLookup))
            orderSummaryLookup = new Dictionary<int, OrderSummary>();

        return Task.FromResult(new StationOrderSummaryAggregate(
            itemTypeIdLookup: _itemTypeIdLookup.Value,
            orderSummaryLookup: orderSummaryLookup,
            station: station
        ));
    }

    public Task<OrderSummary> GetOrderSummary(Station station, string itemTypeName)
    {
        if (!_itemTypeIdLookup.Value.TryGetValue(itemTypeName, out var itemTypeId))
            throw new ArgumentException("Item type name not recognized.");

        if (!_orderSummaryCollectionLookup.TryGetValue(station.Name, out var orderSummaryLookup))
            throw new ArgumentException("Station not recognized.");

        if (!orderSummaryLookup.TryGetValue(itemTypeId, out var orderSummary))
            throw new InvalidOperationException("Could not find order summary.");

        return Task.FromResult(orderSummary);
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

    private static IDictionary<string, int> GetItemTypeIdLookup()
    {
        IDictionary<string, int> itemTypeIdLookup 
            = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

        var assembly = typeof(InMemoryStationOrderSummaryAggregateRepository).Assembly;

        using (var stream = assembly.GetManifestResourceStream("EveBuyback.Data.Resources.typeIDs.yaml"))
        using (var reader = new StreamReader(stream ?? new MemoryStream()))
        {
            if (stream is null) throw new InvalidOperationException("Failed to get type id resource stream.");

            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            IDictionary<string, object> typeIds = deserializer.Deserialize<ExpandoObject>(reader) as IDictionary<string, object>;

            if (typeIds is null) throw new InvalidOperationException("Failed to deserialize type ids.");

            foreach (var kvp in typeIds)
            {
                int typeId = Int32.Parse(kvp.Key);
                
                var itemProps = (IDictionary<object, object>)kvp.Value;
                var nameProps = (IDictionary<object, object>)itemProps["name"];
                var enName = nameProps["en"] as string;

                if (enName is null) throw new InvalidOperationException("Failed to find en name.");

                enName = enName.Trim();

                itemTypeIdLookup.TryAdd(enName, typeId);
            }
        }

        return itemTypeIdLookup;
    }
}