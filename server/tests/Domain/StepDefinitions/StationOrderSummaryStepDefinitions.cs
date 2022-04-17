using NUnit.Framework;
using TechTalk.SpecFlow;

namespace EveBuyback.Domain.Specs;

[Binding]
public class StationOrderSummaryStepDefinitions
{
    private readonly List<ItemType> _itemTypes = new List<ItemType>();
    private readonly Lazy<StationOrderSummaryAggregate> _lazyAggregate;
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
                itemTypeIdLookup: _itemTypes.ToDictionary(
                    i => i.Name,
                    i => i.Id
                ),
                orderSummaryLookup: _orderSummaries.ToDictionary(
                    s => s.ItemTypeId,
                    s => s
                ),
                station: _station
            );
        });
    }

    [Given("item type '(.*)' - '(.*)'")]
    public void GivenItemType(int itemTypeId, string itemTypeName) => _itemTypes
        .Add(new ItemType(itemTypeId, itemTypeName));

    [Given("order summary:")]
    public void GivenOrderSummary(Table table)
    {
        var lookup = table.ToDictionary();

        var item = _itemTypes
            .Single(i => i.Name.Equals(lookup["Item"], StringComparison.InvariantCultureIgnoreCase));

        _orderSummaries.Add(new OrderSummary(
            ShouldBeUsedForBuybackCalculations: Convert.ToBoolean(lookup["ShouldBeUsedForBuybackCalculations"]),
            IsBuyOrder: true,
            Price: Convert.ToDecimal(lookup["Price"]),
            ItemTypeId: item.Id,
            ItemTypeName: item.Name,
            VolumeRemaining: Convert.ToInt32(lookup["VolumeRemaining"]),
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
        Aggregate.RefreshOrderSummary(itemTypeName, volume, currentDateTime);

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
}