using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(
    string StationName, 
    IEnumerable<BuybackItem> Items,
    decimal BuybackTaxPercentage) : IRequest<BackendQueryResult>;

public record BuybackItem(string ItemTypeName, int Volume);

public record BackendQueryResult(decimal BuybackAmount, bool OK, string errorMessage);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, BackendQueryResult>
{
    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    private readonly IItemTypeRepository _itemTypeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly InMemoryStationOrderSummaryAggregateRepository _stationOrderSummaryRepository;

    public BuybackQueryHandler(
        IItemTypeRepository itemTypeRepository,
        IOrderRepository orderRepository,
        IStationOrderSummaryAggregateRepository stationOrderSummaryRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _orderRepository = orderRepository;
        _stationOrderSummaryRepository = (InMemoryStationOrderSummaryAggregateRepository)stationOrderSummaryRepository;
    }

    public async Task<BackendQueryResult> Handle(BuybackQuery query, CancellationToken token)
    {
        if (!_stationLookup.TryGetValue(query.StationName, out var station))
            return new BackendQueryResult(0, false, "Invalid station. Station not recognized.");

        var contractItems = await GetContractItems(query);
        token.ThrowIfCancellationRequested();

        var orderSummaryAggregate = await _stationOrderSummaryRepository.Get(station);
        token.ThrowIfCancellationRequested();

        var domainEvents = await RefreshOrderSummaries(station, contractItems, orderSummaryAggregate, token);
        token.ThrowIfCancellationRequested();

        var errorEvents = domainEvents
            .Select(e => e as IErrorEvent)
            .Where(e => e != null);

        if (errorEvents.Any())
            return new BackendQueryResult(0, false, string.Join("\n", errorEvents));

        await _stationOrderSummaryRepository.Save(orderSummaryAggregate);
        token.ThrowIfCancellationRequested();

        var buybackAmount = 0.0m;

        foreach (var contractItem in contractItems)
        {
            var orderSummary = await _stationOrderSummaryRepository.GetOrderSummary(station, contractItem.Item.Name);
            buybackAmount += (orderSummary.Price * contractItem.Volume);
        }

        var tax = buybackAmount * (query.BuybackTaxPercentage / 100);


        return new BackendQueryResult(buybackAmount - tax, true, string.Empty); 
    }

    private async Task<IEnumerable<ContractItem>> GetContractItems(BuybackQuery query)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();

        var contractcontractItems = new List<ContractItem>();

        foreach (var item in query.Items)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
                itemType = new ItemType(0, item.ItemTypeName, 0);

            contractcontractItems.Add(new ContractItem(itemType, item.Volume));
        }

        return contractcontractItems;
    }

    private async Task<IEnumerable<object>> RefreshOrderSummaries(
        Station station,
        IEnumerable<ContractItem> contractItems,
        StationOrderSummaryAggregate orderSummaryAggregate,
        CancellationToken token)
    {
        var currentDateTime = DateTime.UtcNow;

        foreach (var contractItem in contractItems)
            orderSummaryAggregate.RefreshOrderSummary(contractItem.Item.Name, contractItem.Volume, currentDateTime);

        var invalidEvents = orderSummaryAggregate.DomainEvents?
            .Where(e => e is InvalidOrderSummaryNoticedEvent)?
            .Select(e => e as InvalidOrderSummaryNoticedEvent) ?? Enumerable.Empty<InvalidOrderSummaryNoticedEvent>();

        foreach (var invalidEvent in invalidEvents)
        {
            if (invalidEvent == null)
                throw new InvalidOperationException();

            var orders = await _orderRepository.GetOrders(station, invalidEvent.Item.Id, currentDateTime, token);

            token.ThrowIfCancellationRequested();
            
            orderSummaryAggregate.UpdateOrderSummary(
                invalidEvent.Item,
                invalidEvent.Volume,
                orders,
                currentDateTime);
        }

        return orderSummaryAggregate?.DomainEvents ?? Enumerable.Empty<object>();
    }
}