using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(string stationName, IEnumerable<BuybackItem> Items, decimal BuybackTaxPercentage) : IRequest<BackendQueryResult>;

public record BuybackItem(string ItemTypeName, int Volume);

public record BackendQueryResult(decimal BuybackAmount, bool OK, string errorMessage);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, BackendQueryResult>
{
    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    private readonly IOrderRepository _orderRepository;
    private readonly InMemoryStationOrderSummaryAggregateRepository _stationOrderSummaryRepository;

    public BuybackQueryHandler(
        IOrderRepository orderRepository,
        IStationOrderSummaryAggregateRepository stationOrderSummaryRepository)
    {
        _orderRepository = orderRepository;
        _stationOrderSummaryRepository = (InMemoryStationOrderSummaryAggregateRepository)stationOrderSummaryRepository;
    }

    public async Task<BackendQueryResult> Handle(BuybackQuery query, CancellationToken token)
    {
        if (!_stationLookup.TryGetValue(query.stationName, out var station))
            return new BackendQueryResult(0, false, "Invalid station. Station not recognized.");

        var aggregate = await _stationOrderSummaryRepository.Get(station);

        await RefreshOrderSummaries(query, token, station, aggregate);
        
        token.ThrowIfCancellationRequested();

        await _stationOrderSummaryRepository.Save(aggregate);

        var buybackAmount = 0.0m;
        foreach (var item in query.Items)
        {
            var orderSummary = await _stationOrderSummaryRepository.GetOrderSummary(station, item.ItemTypeName);
            buybackAmount += (orderSummary.Price * item.Volume);
        }

        var tax = buybackAmount * (query.BuybackTaxPercentage / 100);


        return new BackendQueryResult(buybackAmount - tax, true, string.Empty); 
    }

    private async Task RefreshOrderSummaries(
        BuybackQuery query, 
        CancellationToken token,
        Station station,
        StationOrderSummaryAggregate aggregate)
    {
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

            var orders = await _orderRepository.GetOrders(station, invalidEvent.Item.Id, currentDateTime, token);

            token.ThrowIfCancellationRequested();
            
            aggregate.UpdateOrderSummary(
                invalidEvent.Item,
                invalidEvent.Volume,
                orders,
                currentDateTime);
        }
    }
}