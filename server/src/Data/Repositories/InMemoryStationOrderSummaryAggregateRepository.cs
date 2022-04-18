using System.Collections.Concurrent;
using System.Dynamic;
using EveBuyback.Domain;
using YamlDotNet.Serialization.NamingConventions;

namespace Evebuyback.Data;

public class InMemoryStationOrderSummaryAggregateRepository : IStationOrderSummaryAggregateRepository
{
    private static readonly Lazy<IDictionary<string, int>> _itemTypeIdLookup 
        = new Lazy<IDictionary<string, int>>(GetItemTypeIdLookup, true);

    private static readonly ConcurrentDictionary<string, StationOrderSummaryAggregate> _aggregateLookup
        = new ConcurrentDictionary<string, StationOrderSummaryAggregate>(StringComparer.InvariantCultureIgnoreCase);

    public async Task<StationOrderSummaryAggregate> Get(Station station)
    {
        if (_aggregateLookup.TryGetValue(station.Name, out var aggregate))
            return new StationOrderSummaryAggregate(aggregate);

        var itemTypeIdLookup = _itemTypeIdLookup.Value;

        aggregate = new StationOrderSummaryAggregate(
            itemTypeIdLookup: itemTypeIdLookup,
            orderSummaryLookup: new Dictionary<int, OrderSummary>(),
            station: station
        );

        if (_aggregateLookup.TryAdd(station.Name, aggregate))
            return aggregate;

        return await Get(station);
    }

    public Task Save(StationOrderSummaryAggregate aggregate)
    {
        throw new NotImplementedException();
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