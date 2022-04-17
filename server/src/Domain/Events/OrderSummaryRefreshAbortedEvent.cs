namespace EveBuyback.Domain;

public static class OrderSummaryRefreshAbortedEvent
{
    public record InvalidOrderTypeName(string orderTypeName)
    {
        public string PotentialCorrectiveAction
            = $"The specified type of order '{orderTypeName}' you are looking for wasn't recognized.";
    }

    public record OldSummaryIsStillValid(OrderSummary OrderSummary);
}