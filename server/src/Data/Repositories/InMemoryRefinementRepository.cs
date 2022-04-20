using System.Dynamic;
using EveBuyback.Domain;
using YamlDotNet.Serialization.NamingConventions;

namespace Evebuyback.Data;

public class InMemoryRefinementRepository : IRefinementRepository
{
    private static readonly Lazy<IDictionary<int, IEnumerable<BuybackItem>>> _refinementLookup 
        = new Lazy<IDictionary<int, IEnumerable<BuybackItem>>>(GetRefinementLookup, true);

    private readonly IItemTypeRepository _itemTypeRepository;

    public InMemoryRefinementRepository(IItemTypeRepository itemTypeRepository)
    {
        _itemTypeRepository = itemTypeRepository;
    }

    public async Task<IEnumerable<BuybackItem>> GetRefinedItems(BuybackItem item)
    {
        var buybackItems = new List<BuybackItem>();
        var itemTypeLookup = await _itemTypeRepository.GetLookupById();

        if (_refinementLookup.Value.TryGetValue(item.ItemTypeId, out var refinedItems) &&
            itemTypeLookup.TryGetValue(item.ItemTypeId, out var itemType))
        {
            var adjustedRefinedItems = refinedItems
                .Select(i =>
                {
                    var refinedVolume = item.Volume * (i.Volume / itemType.PortionSize);
                    return new BuybackItem(i.ItemTypeId, refinedVolume);
                });

            foreach (var adjustedRefinedItem in adjustedRefinedItems)
            {
                var furtherRefinedItems = await GetRefinedItems(adjustedRefinedItem);
                buybackItems.AddRange(furtherRefinedItems);
            }
        }
        else
        {
            buybackItems.Add(item);
        }

        return buybackItems;
    }

    private static IDictionary<int, IEnumerable<BuybackItem>> GetRefinementLookup()
    {
        IDictionary<int, IEnumerable<BuybackItem>> refinementLookup 
            = new Dictionary<int, IEnumerable<BuybackItem>>();

        var assembly = typeof(InMemoryStationOrderSummaryAggregateRepository).Assembly;

        using (var stream = assembly.GetManifestResourceStream("EveBuyback.Data.Resources.typeMaterials.yaml"))
        using (var reader = new StreamReader(stream ?? new MemoryStream()))
        {
            if (stream is null) throw new InvalidOperationException("Failed to get type materials resource stream.");

            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var materials = deserializer.Deserialize<Dictionary<string, ItemTypeMaterialData>>(reader);

            if (materials is null) throw new InvalidOperationException("Failed to deserialize type materials.");

            foreach ((string itemTypeIdStr, ItemTypeMaterialData materialData) in materials)
            {
                int itemTypeId = Int32.Parse(itemTypeIdStr);
                var refinedItems = new List<BuybackItem>();

                foreach (var material in materialData?.Materials ?? Enumerable.Empty<ItemTypeMaterialItemData>())
                    refinedItems.Add(new BuybackItem(material.MaterialTypeID, material.Quantity));

                refinementLookup.Add(itemTypeId, refinedItems);
            }
        }

        return refinementLookup;
    }
}