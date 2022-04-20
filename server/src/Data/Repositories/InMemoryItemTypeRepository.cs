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
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var itemTypes = deserializer.Deserialize<Dictionary<string, ItemTypeData>>(reader);

            if (itemTypes is null) throw new InvalidOperationException("Failed to deserialize type ids.");

            foreach ((string itemTypeIdStr, ItemTypeData itemType) in itemTypes)
            {
                int itemTypeId = Int32.Parse(itemTypeIdStr);
                var enName = itemType?.Name?.En ?? throw new InvalidOperationException("Invalid type id has no name.");
                enName = enName.Trim();

                itemTypeLookup.TryAdd(itemTypeId, new ItemType(itemTypeId, enName, itemType.PortionSize));
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