using NUnit.Framework;
using TechTalk.SpecFlow;

namespace EveBuyback.Domain.Specs;

[Binding]
public class RegionOrderSummaryStepDefinitions
{
    private readonly List<ItemType> _itemTypes = new List<ItemType>();
    private readonly Lazy<RegionOrderSummaryAggregate> _lazyAggregate;
    private readonly List<OrderSummary> _orderSummaries = new List<OrderSummary>();

    private RegionOrderSummaryAggregate Aggregate => _lazyAggregate.Value;

    public RegionOrderSummaryStepDefinitions()
    {
        _lazyAggregate = new Lazy<RegionOrderSummaryAggregate>(() =>
            new RegionOrderSummaryAggregate(
                itemTypeIdLookup: _itemTypes.ToDictionary(
                    i => i.Name,
                    i => i.Id
                ),
                orderSummaryLookup: _orderSummaries.ToDictionary(
                    s => s.ItemTypeId,
                    s => s
                ),
                regionId: _regionId
            ));
    }
    
    private int _regionId = 0;

    [Given("item type '(.*)' - '(.*)'")]
    public void GivenItemType(int itemTypeId, string itemTypeName) => _itemTypes
        .Add(new ItemType(itemTypeId, itemTypeName));

    [Given("order summary:")]
    public void GivenOrderSummary(Table table)
    {
        var lookup = table.ToDictionary();

        _orderSummaries.Add(new OrderSummary(
            IsValid: Convert.ToBoolean(lookup["IsValid"]),
            IsBuyOrder: true,
            Price: Convert.ToDecimal(lookup["Price"]),
            ItemTypeId: Convert.ToInt32(lookup["ItemTypeId"]),
            ItemTypeName: lookup["ItemTypeName"],
            VolumeRemaining: Convert.ToInt32(lookup["VolumeRemaining"]),
            ExpirationDateTime: Convert.ToDateTime(lookup["ExpirationDateTime"])
        ));
    }

    [Given("region '(.*)'")]
    public void GivenRegion(string region)
    {
        if (region.Equals("The Forge", StringComparison.InvariantCultureIgnoreCase))
        {
            _regionId = 10000002;
            return;
        }

        throw new ArgumentException();
    }

    [When("refreshing order summary for item '(.*)' and a volume of '(.*)' at '(.*)'")]
    public void WhenRefreshingOrderSummary(string itemTypeName, int volume, DateTime currentDateTime) =>
        Aggregate.RefreshOrderSummary(itemTypeName, volume, currentDateTime);

    [Then("refresh aborted because summary is still valid '(.*)'")]
    public void ThenRefreshAbortedBecauseSummaryIsStillValid(bool isStillValid)
    {
        bool wasAborted = Aggregate.DomainEvents
            .Any(e => e is OrderSummaryRefreshAbortedEvent.OldSummaryIsStillValid);

        Assert.AreEqual(isStillValid, wasAborted);

        bool isInvalid = Aggregate.DomainEvents
            .Any(e => e is InvalidOrderSummaryNoticedEvent);

        Assert.AreNotEqual(wasAborted, isInvalid);
    }
}