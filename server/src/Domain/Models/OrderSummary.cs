namespace EveBuyback.Domain;

public record OrderSummary(
    bool ShouldBeUsedForBuybackCalculations,
    bool IsBuyOrder,
    decimal Price,
    int ItemTypeId,
    string ItemTypeName,
    int VolumeRemaining,
    DateTime ExpirationDateTime
);