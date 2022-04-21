using EveBuyback.Domain;
using YamlDotNet.Serialization.NamingConventions;

namespace Evebuyback.Acl;

public class InMemoryRefinedContractItemAggregateRepository : IRefinedContractItemAggregateRepository
{
    private static readonly Lazy<IDictionary<int, IEnumerable<ItemTypeMaterialItemData>>> _refinementLookup 
        = new Lazy<IDictionary<int, IEnumerable<ItemTypeMaterialItemData>>>(GetMaterialItemDataLookup, true);

    private readonly IItemTypeRepository _itemTypeRepository;

    public InMemoryRefinedContractItemAggregateRepository(IItemTypeRepository itemTypeRepository)
    {
        _itemTypeRepository = itemTypeRepository;
    }

    public async Task<RefinedContractItemAggregate> Get(int itemTypeId)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeId();

        return new RefinedContractItemAggregate(
            itemTypeLookup,
            await GetMaterialItems(itemTypeId));
    }

    private async Task<IEnumerable<MaterialItem>> GetMaterialItems(int itemTypeId)
    {
        if (!_refinementLookup.Value.TryGetValue(itemTypeId, out var materials))
            return Enumerable.Empty<MaterialItem>();
        
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeId();

        return materials.Select(i =>
        {
            if (!itemTypeLookup.TryGetValue(i.MaterialTypeID, out var itemType))
                itemType = new ItemType(i.MaterialTypeID, string.Empty, 0);

            return new MaterialItem(itemTypeId, i.MaterialTypeID, i.Quantity);
        });
    }

    private static IDictionary<int, IEnumerable<ItemTypeMaterialItemData>> GetMaterialItemDataLookup()
    {
        IDictionary<int, IEnumerable<ItemTypeMaterialItemData>> refinementLookup 
            = new Dictionary<int, IEnumerable<ItemTypeMaterialItemData>>();

        var assembly = typeof(InMemoryRefinedContractItemAggregateRepository).Assembly;

        using (var stream = assembly.GetManifestResourceStream("EveBuyback.Acl.Resources.typeMaterials.yaml"))
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
                var materialDataItems = materialData?.Materials ?? new List<ItemTypeMaterialItemData>();
                refinementLookup.Add(itemTypeId, materialDataItems);
            }
        }

        return refinementLookup;
    }
}