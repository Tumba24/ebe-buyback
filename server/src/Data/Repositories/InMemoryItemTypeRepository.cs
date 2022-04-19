using System.Dynamic;
using EveBuyback.Domain;
using YamlDotNet.Serialization.NamingConventions;

namespace Evebuyback.Data;

public class InMemoryItemTypeRepository : IItemTypeRepository
{
    private static readonly Lazy<IDictionary<string, ItemType>> _lookupByName 
        = new Lazy<IDictionary<string, ItemType>>(GetItemTypeLookupByName, true);

    private static readonly Lazy<IDictionary<int, ItemType>> _lookupById 
        = new Lazy<IDictionary<int, ItemType>>(GetItemTypeLookupById, true);

    public Task<IDictionary<string, ItemType>> GetLookupByName() => Task.FromResult(_lookupByName.Value);

    public Task<IDictionary<int, ItemType>> GetLookupById() => Task.FromResult(_lookupById.Value);

    private static IDictionary<int, ItemType> GetItemTypeLookupById()
    {
        IDictionary<int, ItemType> itemTypeLookup 
            = new Dictionary<int, ItemType>();

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
                int itemTypeId = Int32.Parse(kvp.Key);
                
                var itemProps = (IDictionary<object, object>)kvp.Value;
                var nameProps = (IDictionary<object, object>)itemProps["name"];
                var enName = nameProps["en"] as string;

                if (enName is null) throw new InvalidOperationException("Failed to find en name.");

                enName = enName.Trim();

                var portionSize = Convert.ToInt32(itemProps["portionSize"]);

                itemTypeLookup.TryAdd(itemTypeId, new ItemType(itemTypeId, enName, portionSize));
            }
        }

        return itemTypeLookup;
    }

    private static IDictionary<string, ItemType> GetItemTypeLookupByName()
    {
        var lookupByName = new Dictionary<string, ItemType>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var item in _lookupById.Value.Values)
            lookupByName.TryAdd(item.Name, item);

        return lookupByName;
    }
}