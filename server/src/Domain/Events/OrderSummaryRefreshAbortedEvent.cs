namespace EveBuyback.Domain;

public static class OrderSummaryRefreshAbortedEvent
{
    public record InvalidItemTypeName(string itemTypeName)
    {
        public string PotentialCorrectiveAction
            = $"The specified type of item '{itemTypeName}' you are looking for wasn't recognized.";
    }

    public record OldSummaryIsStillValid(OrderSummary OrderSummary);
}