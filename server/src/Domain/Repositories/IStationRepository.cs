namespace EveBuyback.Domain;

public interface IStationRepository
{
    Task<Station?> Get(string stationName);
}