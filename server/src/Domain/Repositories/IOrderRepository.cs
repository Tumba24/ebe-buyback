namespace EveBuyback.Domain;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetOrders(
        Station station, 
        int itemTypeId, 
        DateTime currentDateTime, 
        CancellationToken token);
}