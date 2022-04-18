namespace EveBuyback.Domain;

public record Order(
    int Duration,
    bool IsBuyOrder,
    DateTime IssuedOnDateTime,
    long LocationId,
    int MinVolume,
    long OrderId,
    decimal Price,
    int SystemId,
    int ItemTypeId,
    int VolumeRemaining,
    int VolumeTotal,
    DateTime ExpiresOnDateTime
);