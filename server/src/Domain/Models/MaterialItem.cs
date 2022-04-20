namespace EveBuyback.Domain;

public record MaterialItem(
    int UnrefinedItemTypeId,
    int MaterialItemTypeId,
    int Quantity
);