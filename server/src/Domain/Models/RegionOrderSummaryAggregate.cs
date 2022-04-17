namespace EveBuyback.Domain;

public class RegionOrderSummaryAggregate
{
    private readonly IList<object> _domainEvents = new List<object>();
    private readonly IDictionary<int, string> _orderNameLookup;
    private readonly IDictionary<int, OrderSummary> _orderSummaryLookup;
    private readonly IDictionary<string, int> _orderTypeIdLookup;
    private readonly int _regionId;
    private readonly IList<OrderSummary> _updatedOrderSummaries = new List<OrderSummary>();

    public RegionOrderSummaryAggregate(
        IDictionary<int, string> orderNameLookup,
        IDictionary<int, OrderSummary> orderSummaryLookup,
        IDictionary<string, int> orderTypeIdLookup,
        int regionId)
    {
        _orderNameLookup = orderNameLookup ?? throw new ArgumentNullException(nameof(orderNameLookup));
        _orderSummaryLookup = orderSummaryLookup ?? throw new ArgumentNullException(nameof(orderSummaryLookup));
        _orderTypeIdLookup = orderTypeIdLookup ?? throw new ArgumentNullException(nameof(orderTypeIdLookup));

        _regionId = regionId;
    }

    public IEnumerable<object> DomainEvents => _domainEvents.ToArray();
    public IEnumerable<OrderSummary> UpdatedOrderSummaries => _updatedOrderSummaries.ToArray();

    public void RefreshOrderSummary(string orderTypeName, int volume)
    {
        if (orderTypeName is null) throw new ArgumentNullException(orderTypeName);

        if (!_orderTypeIdLookup.TryGetValue(orderTypeName, out var orderTypeId))
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.InvalidOrderTypeName(orderTypeName));
            return;
        }

        if (_orderSummaryLookup.TryGetValue(orderTypeId, out var summary) && 
            summary.ExpirationDateTime < DateTime.UtcNow && 
            summary.VolumeRemaining <= volume)
        {
            _domainEvents.Add(new OrderSummaryRefreshAbortedEvent.OldSummaryIsStillValid(summary));
            return;
        }

        _domainEvents.Add(new InvalidOrderSummaryNoticedEvent(orderTypeId, orderTypeName));
    }

    public void UpdateOrderSummary(int orderTypeId, string orderTypeName, int volume, IEnumerable<Order> orders)
    {
        orders = orders?
            .Where(o => 
                o.IsBuyOrder == true &&
                o.IssuedOnDateTime.AddDays(o.Duration) >= DateTime.UtcNow.AddDays(1) &&
                o.MinVolume < volume)?
            .OrderByDescending(o => o.Price) ?? Enumerable.Empty<Order>();

        if (!orders.Any())
        {
            UpdateOrderSummary(new OrderSummary(false, true, 0, orderTypeId, orderTypeName, 0, DateTime.UtcNow.AddSeconds(2)));
            return;
        }

        decimal maxPrice = 0;
        var volumeToFill = volume;
        int totalOrderVolumeRemaining = 0;
        DateTime firstOrderExpirationDateTime = DateTime.UtcNow.AddSeconds(2);

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
            IsValid: true,
            IsBuyOrder: true,
            Price: maxPrice,
            OrderTypeId: orderTypeId,
            OrderTypeName: orderTypeName,
            VolumeRemaining: totalOrderVolumeRemaining,
            ExpirationDateTime : firstOrderExpirationDateTime
        ));
    }

    private void UpdateOrderSummary(OrderSummary orderSummary)
    {
        _updatedOrderSummaries.Add(orderSummary);
        _domainEvents.Add(new OrderSummaryUpatedEvent(orderSummary));
    }
}