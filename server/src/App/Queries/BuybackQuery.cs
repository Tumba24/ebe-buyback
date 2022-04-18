using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(string stationName, IEnumerable<BuybackItem> Items, decimal BuybackTaxPercentage) : IRequest<decimal>;

public record BuybackItem(string ItemTypeName, int Volume);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, decimal>
{
    private static readonly HttpClient _httpClient = CreateClient();

    private static readonly IDictionary<string, Station> _stationLookup = 
        new Dictionary<string, Station>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Jita", new Station(10000002, 60003760, "Jita") }
        };

    private readonly InMemoryStationOrderSummaryAggregateRepository _repository;

    public BuybackQueryHandler(IStationOrderSummaryAggregateRepository repository)
    {
        _repository = (InMemoryStationOrderSummaryAggregateRepository)repository;
    }

    public async Task<decimal> Handle(BuybackQuery query, CancellationToken token)
    {
        if (!_stationLookup.TryGetValue(query.stationName, out var station))
            throw new ArgumentException("Invalid station. Sttion not recognized.");

        var aggregate = await _repository.Get(station);

        var currentDateTime = DateTime.UtcNow;

        foreach (var item in query.Items)
            aggregate.RefreshOrderSummary(item.ItemTypeName, item.Volume, currentDateTime);

        var invalidEvents = aggregate.DomainEvents?
            .Where(e => e is InvalidOrderSummaryNoticedEvent)?
            .Select(e => e as InvalidOrderSummaryNoticedEvent) ?? Enumerable.Empty<InvalidOrderSummaryNoticedEvent>();

        foreach (var invalidEvent in invalidEvents)
        {
            if (invalidEvent == null)
                throw new InvalidOperationException();

            var orders = await GetOrders(station, invalidEvent.Item.Id, currentDateTime);
            
            aggregate.UpdateOrderSummary(
                invalidEvent.Item,
                1000000,
                orders,
                currentDateTime);
        }

        await _repository.Save(aggregate);

        var buybackAmount = 0.0m;
        foreach (var item in query.Items)
        {
            var orderSummary = await _repository.GetOrderSummary(station, item.ItemTypeName);
            buybackAmount += (orderSummary.Price * item.Volume);
        }

        var tax = buybackAmount * (query.BuybackTaxPercentage / 100);

        return buybackAmount - tax;
    }

    private static HttpClient CreateClient()
    {
        HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://esi.evetech.net/")
        };

        client.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    private async Task<IEnumerable<Order>> GetOrders(Station station, int itemTypeId, DateTime currentDateTime)
    {
        var orders = new List<Order>();
        
        int page = 1;
        IEnumerable<Order>? pageOrders;
        while ((pageOrders = await GetNextPage()) != null)
        {
            page++;
            orders.AddRange(pageOrders);
        }

        return orders;

        async Task<IEnumerable<Order>?> GetNextPage()
        {
            var pageOrders = new List<Order>();
            var relativeAddress = $"latest/markets/{station.RegionId}/orders/?datasource=tranquility&order_type=buy&page={page}&type_id={itemTypeId}";

            using (var response = await _httpClient.GetAsync(relativeAddress))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                
                response.EnsureSuccessStatusCode();

                var contentStream = await response.Content.ReadAsStreamAsync();
                if (contentStream is null)
                    return null;

                var expiresOnDateTime = currentDateTime.AddMinutes(5);
                if (response.Headers.TryGetValues("expires", out var expiresOnStr))
                    expiresOnDateTime = Convert.ToDateTime(expiresOnStr);

                var result = JsonSerializer.Deserialize<List<EveOrderData>>(contentStream);
                if (result is null)
                    return null;

                foreach (var item in result)
                {
                    pageOrders.Add(
                        new Order(
                            Duration: item.Duration,
                            IsBuyOrder: item.IsBuyOrder,
                            IssuedOnDateTime: item.Issued,
                            LocationId: item.LocationId,
                            MinVolume: item.MinVolume,
                            OrderId: item.OrderId,
                            Price: item.Price,
                            SystemId: item.SystemId,
                            ItemTypeId: item.ItemTypeId,
                            VolumeRemaining: item.VolumeRemaining,
                            VolumeTotal: item.VolumeTotal,
                            ExpiresOnDateTime: expiresOnDateTime
                        )
                    );
                }

                return pageOrders.Any() ? pageOrders : null;
            }
        }
    }

    internal class EveOrderData
    {
        [JsonPropertyName("duration")]
        public int Duration { get; set; }
        [JsonPropertyName("is_buy_order")]
        public bool IsBuyOrder { get; set; }
        [JsonPropertyName("issued")]
        public DateTime Issued { get; set; }
        [JsonPropertyName("location_id")]
        public long LocationId { get; set; }
        [JsonPropertyName("min_volume")]
        public int MinVolume { get; set; }
        [JsonPropertyName("order_id")]
        public long OrderId { get; set; }
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
        [JsonPropertyName("system_id")]
        public int SystemId { get; set; }
        [JsonPropertyName("type_id")]
        public int ItemTypeId { get; set; }
        [JsonPropertyName("volume_remain")]
        public int VolumeRemaining { get; set; }
        [JsonPropertyName("volume_total")]
        public int VolumeTotal { get; set; }
    }
}