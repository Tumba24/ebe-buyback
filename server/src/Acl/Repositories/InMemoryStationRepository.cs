using EveBuyback.Domain;

namespace Evebuyback.Acl;

public class InMemoryStationRepository : IStationRepository
{
    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    public Task<Station?> Get(string stationName)
    {
        Station? station = null;
        _stationLookup.TryGetValue(stationName, out station);

        return Task.FromResult(station);
    }
}