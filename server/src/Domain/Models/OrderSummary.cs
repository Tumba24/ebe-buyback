namespace EveBuyback.Domain;

public record OrderSummary(
    bool IsValid,
    bool IsBuyOrder,
    decimal Price,
    int OrderTypeId,
    string OrderTypeName,
    int VolumeRemaining,
    DateTime ExpirationDateTime
);