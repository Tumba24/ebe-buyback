namespace EveBuyback.Domain;

public record Station(
    int RegionId,
    long LocationId,
    string Name
);