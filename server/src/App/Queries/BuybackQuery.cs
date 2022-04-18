using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(string stationName, IEnumerable<BuybackItem> Items) : IRequest<decimal>;

public record BuybackItem(string ItemTypeName, int Volume);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, decimal>
{
    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    private readonly IStationOrderSummaryAggregateRepository _repository;

    public BuybackQueryHandler(IStationOrderSummaryAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task<decimal> Handle(BuybackQuery query, CancellationToken token)
    {
        if (!_stationLookup.TryGetValue(query.stationName, out var station))
            throw new ArgumentException("Invalid station. Sttion not recognized.");

        var aggregate = await _repository.Get(station);

        var currentDateTime = DateTime.UtcNow;

        foreach (var item in query.Items)
            aggregate.RefreshOrderSummary(item.ItemTypeName, item.Volume, currentDateTime);

        var invalidEvents = aggregate.DomainEvents?
            .Where(e => e is InvalidOrderSummaryNoticedEvent)?
            .Select(e => e as InvalidOrderSummaryNoticedEvent) ?? Enumerable.Empty<InvalidOrderSummaryNoticedEvent>();

        foreach (var invalidEvent in invalidEvents)
        {
            if (invalidEvent == null)
                throw new InvalidOperationException();
            
            aggregate.UpdateOrderSummary(
                invalidEvent.Item,
                1000000, 
                new Order[0], 
                currentDateTime);
        }

        await _repository.Save(aggregate);

        return 2.0m;
    }
}