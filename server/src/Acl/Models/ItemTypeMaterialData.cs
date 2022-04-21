namespace Evebuyback.Acl;

internal class ItemTypeMaterialData
{
    public List<ItemTypeMaterialItemData>? Materials { get; set; }
}

internal class ItemTypeMaterialItemData
{
    public int MaterialTypeID { get; set; }
    public int Quantity { get; set; }
}