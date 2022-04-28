using NUnit.Framework;
using TechTalk.SpecFlow;

namespace EveBuyback.Domain.Specs;

[Binding]
public class StationOrderSummaryStepDefinitions
{
    private readonly List<ItemType> _itemTypes = new List<ItemType>();
    private readonly Lazy<StationOrderSummaryAggregate> _lazyAggregate;
    private readonly List<Order> _orders = new List<Order>();
    private readonly List<OrderSummary> _orderSummaries = new List<OrderSummary>();
    private Station? _station;

    private StationOrderSummaryAggregate Aggregate => _lazyAggregate.Value;

    public StationOrderSummaryStepDefinitions()
    {
        _lazyAggregate = new Lazy<StationOrderSummaryAggregate>(() =>
        {
            if (_station == null)
                throw new InvalidOperationException("Station is required");

            return new StationOrderSummaryAggregate(
                itemTypeLookup: _itemTypes.ToDictionary(
                    i => i.Name,
                    i => i
                ),
                orderSummaryLookup: _orderSummaries.ToDictionary(
                    s => s.Item.Id,
                    s => s
                ),
                station: _station
            );
        });
    }

    [Given("item type '(.*)' - '(.*)'")]
    public void GivenItemType(int itemTypeId, string itemTypeName) => _itemTypes
        .Add(new ItemType(itemTypeId, itemTypeName, 100));

    [Given("order:")]
    public void GivenOrder(Table table)
    {
        if (_station == null)
            throw new InvalidOperationException("Station is required");

        var lookup = table.ToDictionary();

        var item = GetItem(lookup["Item"]);

        _orders.Add(new Order(
            Duration: Convert.ToInt32(lookup["DurationInDays"]),
            IsBuyOrder: Convert.ToBoolean(lookup["IsBuyOrder"]),
            IssuedOnDateTime: Convert.ToDateTime(lookup["IssuedOnDateTime"]),
            LocationId: _station.LocationId,
            MinVolume: Convert.ToInt32(lookup["MinVolume"]),
            OrderId: 1,
            Price: Convert.ToDecimal(lookup["Price"]),
            SystemId: 1,
            ItemTypeId: item.Id,
            VolumeRemaining: Convert.ToInt32(lookup["VolumeRemaining"]),
            VolumeTotal: Convert.ToInt32(lookup["VolumeRemaining"]),
            ExpiresOnDateTime: Convert.ToDateTime(lookup["ExpiresOnDateTime"])
        ));
    }

    [Given("order summary:")]
    public void GivenOrderSummary(Table table)
    {
        var lookup = table.ToDictionary();

        var item = GetItem(lookup["Item"]);

        _orderSummaries.Add(new OrderSummary(
            ShouldBeUsedForBuybackCalculations: Convert.ToBoolean(lookup["ShouldBeUsedForBuybackCalculations"]),
            IsBuyOrder: true,
            Price: Convert.ToDecimal(lookup["Price"]),
            Item: item,
            VolumeRemaining: Convert.ToInt32(lookup["VolumeRemaining"]),
            MinVolume: Convert.ToInt32(lookup["MinVolume"]),
            LowerVolumeWithBetterPricing: null,
            ExpirationDateTime: Convert.ToDateTime(lookup["ExpirationDateTime"])
        ));
    }

    [Given("station:")]
    public void GivenStation(Table table)
    {
        var lookup = table.ToDictionary();

        _station = new Station(
            RegionId: Convert.ToInt32(lookup["RegionId"]),
            LocationId: Convert.ToInt64(lookup["LocationId"]),
            Name: lookup["Name"]
        );
    }

    [When("refreshing order summary for item '(.*)' and a volume of '(.*)' at '(.*)'")]
    public void WhenRefreshingOrderSummary(string itemTypeName, int volume, DateTime currentDateTime) =>
        Aggregate.RefreshOrderSummary(new ContractItem(itemTypeName, volume), currentDateTime);

    [When("updating order summary for item '(.*)' and a volume of '(.*)' at '(.*)'")]
    public void WhenUpdatingOrderSummary(string itemTypeName, int volume, DateTime currentDateTime)
    {
        var item = GetItem(itemTypeName);

        Aggregate.UpdateOrderSummary(item, volume, _orders, currentDateTime);
    }

    [Then("refresh aborted because summary is still valid")]
    public void ThenRefreshAbortedBecauseSummaryIsStillValid()
    {
        bool wasAborted = Aggregate.DomainEvents
            .Any(e => e is OrderSummaryRefreshAbortedEvent.OldSummaryIsStillValid);

        Assert.AreEqual(true, wasAborted);

        bool isInvalid = Aggregate.DomainEvents
            .Any(e => e is InvalidOrderSummaryNoticedEvent);

        Assert.AreEqual(false, isInvalid);
    }

    [Then("refresh aborted because item is not valid")]
    public void ThenRefreshAbortedBecauseItemIsNotValid()
    {
        bool wasAborted = Aggregate.DomainEvents
            .Any(e => e is OrderSummaryRefreshAbortedEvent.InvalidItemTypeName);

        Assert.AreEqual(true, wasAborted);

        bool isInvalid = Aggregate.DomainEvents
            .Any(e => e is InvalidOrderSummaryNoticedEvent);

        Assert.AreEqual(false, isInvalid);
    }

    [Then("refresh marked current summary version as invalid")]
    public void ThenRefreshMarkedCurrentSummaryVersionAsInvalid()
    {
        bool isInvalid = Aggregate.DomainEvents
            .Any(e => e is InvalidOrderSummaryNoticedEvent);

        Assert.AreEqual(true, isInvalid);
    }

    [Then("updated order summary is:")]
    public void ThenUpdatedOrderSummaryIs(Table table)
    {
        var lookup = table.ToDictionary();

        Assert.IsNotNull(Aggregate.UpdatedOrderSummaries.FirstOrDefault(s =>
            s.Item.Name.Equals(lookup["Item"], StringComparison.InvariantCultureIgnoreCase) &&
            s.ExpirationDateTime == Convert.ToDateTime(lookup["ExpirationDateTime"]) &&
            s.Price == Convert.ToDecimal(lookup["Price"]) &&
            s.VolumeRemaining == Convert.ToInt32(lookup["VolumeRemaining"]) &&
            s.ShouldBeUsedForBuybackCalculations == Convert.ToBoolean(lookup["ShouldBeUsedForBuybackCalculations"])));
    }

    private ItemType GetItem(string itemName) => _itemTypes
        .Single(i => i.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));
}