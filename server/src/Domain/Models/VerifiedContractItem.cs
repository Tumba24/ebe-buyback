namespace EveBuyback.Domain;

public record VerifiedContractItem(
    ItemType Item,
    int Volume) : ContractItem(Item.Name, Volume);