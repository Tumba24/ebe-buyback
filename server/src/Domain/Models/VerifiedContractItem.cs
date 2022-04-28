namespace EveBuyback.Domain;

public record VerifiedContractItem(
    ItemType Item,
    long Volume) : ContractItem(Item.Name, Volume);