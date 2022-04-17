using TechTalk.SpecFlow;

namespace EveBuyback.Domain.Specs;

[Binding]
public class RegionOrderSummaryStepDefinitions
{
    [Given("item type '(.*)' - '(.*)'")]
    public void GivenItemType(int itemTypeId, string itemTypeName)
    {
        throw new NotImplementedException();
    }

    [Given("order summary:")]
    public void GivenOrderSummary(Table table)
    {
        throw new NotImplementedException();
    }

    [Given("region '(.*)'")]
    public void GivenRegion(string region)
    {
        throw new NotImplementedException();
    }

    [Given("refreshing order summary for item '(.*)' and a volume of '(.*)'")]
    public void WhenRefreshingOrderSummary(string orderItemName, int volume)
    {
        throw new NotImplementedException();
    }

    [Then("refresh aborted because summary is still valid '(.*)'")]
    public void ThenRefreshAbortedBecauseSummaryIsStillValid(bool isStillValid)
    {
        throw new NotImplementedException();
    }
}