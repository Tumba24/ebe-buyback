namespace EveBuyback.Domain;

public static class RefinementAbortedEvent
{
    public record InvalidMaterialTypeId(int materialTypeId) : IErrorEvent
    {
        public string PotentialCorrectiveAction
            => $"The specified material type id '{materialTypeId}' you are looking for isn't recognized. Correct or exclude it.";
    }
}