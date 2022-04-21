namespace EveBuyback.Domain;

public record OrderSummary(
    bool ShouldBeUsedForBuybackCalculations,
    bool IsBuyOrder,
    decimal Price,
    ItemType Item,
    int VolumeRemaining,
    int MinVolume,
    DateTime ExpirationDateTime
);