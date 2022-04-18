namespace EveBuyback.Domain;

public interface IStationOrderSummaryAggregateRepository
{
    Task<StationOrderSummaryAggregate> Get(Station station);
    Task Save(StationOrderSummaryAggregate aggregate);
}