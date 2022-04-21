using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record OrderSummaryRefreshCommand(
    string StationName, 
    IEnumerable<OrderSummaryRefreshCommandItem> Items) : IRequest<OrderSummaryRefreshCommandResult>;

public record OrderSummaryRefreshCommandItem(string ItemTypeName, int Volume);

public record OrderSummaryRefreshCommandResult(bool OK, string errorMessage);

internal class OrderSummaryRefreshCommandHandler : IRequestHandler<OrderSummaryRefreshCommand, OrderSummaryRefreshCommandResult>
{
    private readonly IItemTypeRepository _itemTypeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly InMemoryStationOrderSummaryAggregateRepository _stationOrderSummaryRepository;
    private readonly IStationRepository _stationRepository;

    public OrderSummaryRefreshCommandHandler(
        IItemTypeRepository itemTypeRepository,
        IOrderRepository orderRepository,
        IStationOrderSummaryAggregateRepository stationOrderSummaryRepository,
        IStationRepository stationRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _orderRepository = orderRepository;
        _stationOrderSummaryRepository = (InMemoryStationOrderSummaryAggregateRepository)stationOrderSummaryRepository;
        _stationRepository = stationRepository;
    }

    public async Task<OrderSummaryRefreshCommandResult> Handle(
        OrderSummaryRefreshCommand command, 
        CancellationToken token)
    {
        var station = await _stationRepository.Get(command.StationName);
        if (station == null)
            return new OrderSummaryRefreshCommandResult(false, "Invalid station. Station not recognized.");

        var contractItems = await GetContractItems(command);
        token.ThrowIfCancellationRequested();

        var orderSummaryAggregate = await _stationOrderSummaryRepository.Get(station);
        token.ThrowIfCancellationRequested();

        var domainEvents = await RefreshOrderSummaries(station, contractItems, orderSummaryAggregate, token);
        token.ThrowIfCancellationRequested();

        var errorEvents = domainEvents
            .Select(e => e as IErrorEvent)
            .Where(e => e != null);

        if (errorEvents.Any())
            return new OrderSummaryRefreshCommandResult(false, string.Join("\n", errorEvents));

        await _stationOrderSummaryRepository.Save(orderSummaryAggregate);
        token.ThrowIfCancellationRequested();

        return new OrderSummaryRefreshCommandResult(true, string.Empty);
    }

    private async Task<IEnumerable<ContractItem>> GetContractItems(OrderSummaryRefreshCommand command)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();

        var contractcontractItems = new List<ContractItem>();

        foreach (var item in command.Items)
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