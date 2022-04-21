namespace EveBuyback.Domain;

public class StationOrderSummaryAggregate
{
    private readonly IList<object> _domainEvents = new List<object>();
    private readonly IDictionary<int, OrderSummary> _orderSummaryLookup;
    private readonly IDictionary<string, ItemType> _itemTypeLookup;
    private readonly IList<OrderSummary> _updatedOrderSummaries = new List<OrderSummary>();

    public StationOrderSummaryAggregate(
        IDictionary<string, ItemType> itemTypeLookup,
        IDictionary<int, OrderSummary> orderSummaryLookup,
        Station station)
    {
        _itemTypeLookup = itemTypeLookup ?? throw new ArgumentNullException(nameof(itemTypeLookup));
        _orderSummaryLookup = orderSummaryLookup ?? throw new ArgumentNullException(nameof(orderSummaryLookup));
        Station = station ?? throw new ArgumentNullException(nameof(station));
    }

    public Station Station { get; }
    public IEnumerable<object> DomainEvents => _domainEvents.ToArray();
    public IEnumerable<OrderSummary> UpdatedOrderSummaries => _updatedOrderSummaries.ToArray();

    public void RefreshOrderSummary(ContractItem contractItem, DateTime currentDateTime)
    {
        if (!_itemTypeLookup.TryGetValue(contractItem.ItemTypeName, out var itemType))
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.InvalidItemTypeName(contractItem.ItemTypeName));
            return;
        }

        if (_orderSummaryLookup.TryGetValue(itemType.Id, out var summary) &&
            summary.ExpirationDateTime > currentDateTime && 
            summary.VolumeRemaining >= contractItem.Volume &&
            summary.MinVolume <= contractItem.Volume)
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.OldSummaryIsStillValid(summary));
            return;
        }

        _domainEvents.Add(new InvalidOrderSummaryNoticedEvent(itemType, contractItem.Volume));
    }

    public void UpdateOrderSummary(ItemType item, int volume, IEnumerable<Order> orders, DateTime currentDateTime)
    {
        orders = orders?
            .Where(o => 
                o.LocationId == Station.LocationId &&
                o.IsBuyOrder == true &&
                o.IssuedOnDateTime.AddDays(o.Duration) >= currentDateTime.AddDays(1) &&
                o.MinVolume <= volume &&
                o.ExpiresOnDateTime > currentDateTime)?
            .OrderByDescending(o => o.Price)
            .OrderByDescending(o => o.ExpiresOnDateTime) ?? Enumerable.Empty<Order>();

        int ordersThatShouldBeSkipped = 0;
        int loopBreaker = 15;
        int loopCount = 0;

        while ((ordersThatShouldBeSkipped = UpdateOrderSummary(item, volume, orders, currentDateTime, ordersThatShouldBeSkipped)) != 0)
        {
            if (loopCount >= loopBreaker)
                break;

            loopCount++;
        }
    }
    private int UpdateOrderSummary(ItemType item, int volume, IEnumerable<Order> orders, DateTime currentDateTime, int ordersToSkip)
    {
        var ordersToConsider = orders.Skip(ordersToSkip);

        if (!ordersToConsider.Any())
        {
            UpdateOrderSummary(new OrderSummary(false, true, 0, item, 0, 1, currentDateTime.AddSeconds(2)));
            return 0;
        }

        decimal maxPrice = 0;
        var volumeToFill = volume;
        int totalOrderVolumeRemaining = 0;
        DateTime firstOrderExpirationDateTime = currentDateTime.AddMinutes(5);
        var orderIndex = 0;
        var maxMinVolume = 1;

        foreach (var order in ordersToConsider)
        {
            if (order.MinVolume > volumeToFill)
            {
                if (orderIndex > 0 && order.MinVolume < volume)
                {
                    return orderIndex;
                }
                
                continue;
            }

            orderIndex++;
            maxPrice = order.Price;
            maxMinVolume = maxMinVolume > order.MinVolume ? maxMinVolume : order.MinVolume;

            volumeToFill -= volumeToFill > order.VolumeRemaining ? 
                order.VolumeRemaining : 
                volumeToFill;

            totalOrderVolumeRemaining += order.VolumeRemaining;

            firstOrderExpirationDateTime = order.ExpiresOnDateTime < firstOrderExpirationDateTime ? 
                order.ExpiresOnDateTime : 
                firstOrderExpirationDateTime;

            if (volumeToFill < 1)
            {
                UpdateOrderSummary(new OrderSummary(
                    ShouldBeUsedForBuybackCalculations: true,
                    IsBuyOrder: true,
                    Price: maxPrice,
                    Item: item,
                    VolumeRemaining: totalOrderVolumeRemaining,
                    MinVolume: maxMinVolume,
                    ExpirationDateTime : firstOrderExpirationDateTime
                ));

                return 0;
            }
        }

        UpdateOrderSummary(new OrderSummary(
            ShouldBeUsedForBuybackCalculations: false,
            IsBuyOrder: true,
            Price: maxPrice,
            Item: item,
            VolumeRemaining: totalOrderVolumeRemaining,
            MinVolume: maxMinVolume,
            ExpirationDateTime : firstOrderExpirationDateTime
        ));

        return 0;
    }

    private void UpdateOrderSummary(OrderSummary orderSummary)
    {
        _updatedOrderSummaries.Add(orderSummary);
        _domainEvents.Add(new OrderSummaryUpdatedEvent(orderSummary));
    }
}