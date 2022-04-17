namespace EveBuyback.Domain;

public record Order(
    int Duration,
    bool IsBuyOrder,
    DateTime IssuedOnDateTime,
    string Issued,
    long LocationId,
    int MinVolume,
    long OrderId,
    decimal Price,
    string Range,
    int SystemId,
    int OrderTypeId,
    int VolumeRemaining,
    int VolumeTotal
);