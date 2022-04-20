namespace EveBuyback.Domain;

public interface IErrorEvent
{
    string PotentialCorrectiveAction { get; }
}