namespace EveBuyback.Domain;

public static class OrderSummaryRefreshAbortedEvent
{
    public record InvalidItemTypeName(string itemTypeName) : IErrorEvent
    {
        public string PotentialCorrectiveAction
            => $"The specified type of item '{itemTypeName}' you are looking for isn't recognized. Correct or exclude it.";
    }

    public record OldSummaryIsStillValid(OrderSummary OrderSummary);
}