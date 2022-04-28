using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record OrderSummaryRefreshCommand(
    string StationName, 
    IEnumerable<OrderSummaryRefreshCommandItem> Items) : IRequest<OrderSummaryRefreshCommandResult>;

public record OrderSummaryRefreshCommandItem(string ItemTypeName, int Volume);

public record OrderSummaryRefreshCommandResult(bool OK, string ErrorMessage);

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

        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();
        token.ThrowIfCancellationRequested();

        if (!TryGetContractItems(command, itemTypeLookup, out var contractItems, out var errorMessage))
            return new OrderSummaryRefreshCommandResult(false, errorMessage ?? "Failed to get contract items.");

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

    private bool TryGetContractItems(
        OrderSummaryRefreshCommand command,
        IDictionary<string, ItemType> itemTypeLookup,
        out List<ContractItem> contractItems,
        out string? errorMessage)
    {
        contractItems = new List<ContractItem>();

        foreach (var item in command.Items)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
            {
                errorMessage = $"Invalid item: '{item.ItemTypeName}'. Item not recognized.";
                return false;
            }

            contractItems.Add(new ContractItem(itemType.Name, item.Volume));
        }

        errorMessage = null;
        return true;
    }

    private async Task<IEnumerable<object>> RefreshOrderSummaries(
        Station station,
        IEnumerable<ContractItem> contractItems,
        StationOrderSummaryAggregate orderSummaryAggregate,
        CancellationToken token)
    {
        var currentDateTime = DateTime.UtcNow;

        foreach (var contractItem in contractItems)
            orderSummaryAggregate.RefreshOrderSummary(contractItem, currentDateTime);

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