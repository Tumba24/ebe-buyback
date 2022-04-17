namespace EveBuyback.Domain;

public class StationOrderSummaryAggregate
{
    private readonly IList<object> _domainEvents = new List<object>();
    private readonly IDictionary<int, OrderSummary> _orderSummaryLookup;
    private readonly IDictionary<string, int> _itemTypeIdLookup;
    private readonly Station _station;
    private readonly IList<OrderSummary> _updatedOrderSummaries = new List<OrderSummary>();

    public StationOrderSummaryAggregate(
        IDictionary<string, int> itemTypeIdLookup,
        IDictionary<int, OrderSummary> orderSummaryLookup,
        Station station)
    {
        _itemTypeIdLookup = itemTypeIdLookup ?? throw new ArgumentNullException(nameof(itemTypeIdLookup));
        _orderSummaryLookup = orderSummaryLookup ?? throw new ArgumentNullException(nameof(orderSummaryLookup));
        _station = station ?? throw new ArgumentNullException(nameof(station));
    }

    public IEnumerable<object> DomainEvents => _domainEvents.ToArray();
    public IEnumerable<OrderSummary> UpdatedOrderSummaries => _updatedOrderSummaries.ToArray();

    public void RefreshOrderSummary(string itemTypeName, int volume, DateTime currentDateTime)
    {
        if (itemTypeName is null) throw new ArgumentNullException(itemTypeName);

        if (!_itemTypeIdLookup.TryGetValue(itemTypeName, out var itemTypeId))
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.InvalidItemTypeName(itemTypeName));
            return;
        }

        if (_orderSummaryLookup.TryGetValue(itemTypeId, out var summary) &&
            summary.ExpirationDateTime > currentDateTime && 
            summary.VolumeRemaining >= volume)
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.OldSummaryIsStillValid(summary));
            return;
        }

        _domainEvents.Add(new InvalidOrderSummaryNoticedEvent(itemTypeId, itemTypeName));
    }

    public void UpdateOrderSummary(int itemTypeId, string itemTypeName, int volume, IEnumerable<Order> orders, DateTime currentDateTime)
    {
        orders = orders?
            .Where(o => 
                o.LocationId == _station.LocationId &&
                o.IsBuyOrder == true &&
                o.IssuedOnDateTime.AddDays(o.Duration) >= currentDateTime.AddDays(1) &&
                o.MinVolume < volume)?
            .OrderByDescending(o => o.Price) ?? Enumerable.Empty<Order>();

        if (!orders.Any())
        {
            UpdateOrderSummary(new OrderSummary(false, true, 0, itemTypeId, itemTypeName, 0, currentDateTime.AddSeconds(2)));
            return;
        }

        decimal maxPrice = 0;
        var volumeToFill = volume;
        int totalOrderVolumeRemaining = 0;
        DateTime firstOrderExpirationDateTime = currentDateTime.AddSeconds(2);

        foreach (var order in orders)
        {
            maxPrice = order.Price;

            volumeToFill -= volumeToFill > order.VolumeRemaining ? 
                order.VolumeRemaining : 
                volumeToFill;

            totalOrderVolumeRemaining += order.VolumeRemaining;

            var orderExpirationDate = order.IssuedOnDateTime.AddDays(order.Duration);

            firstOrderExpirationDateTime = orderExpirationDate < firstOrderExpirationDateTime ? 
                orderExpirationDate : 
                firstOrderExpirationDateTime;
        }

        UpdateOrderSummary(new OrderSummary(
            ShouldBeUsedForBuybackCalculations: true,
            IsBuyOrder: true,
            Price: maxPrice,
            ItemTypeId: itemTypeId,
            ItemTypeName: itemTypeName,
            VolumeRemaining: totalOrderVolumeRemaining,
            ExpirationDateTime : firstOrderExpirationDateTime
        ));
    }

    private void UpdateOrderSummary(OrderSummary orderSummary)
    {
        _updatedOrderSummaries.Add(orderSummary);
        _domainEvents.Add(new OrderSummaryUpdatedEvent(orderSummary));
    }
}