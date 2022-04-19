using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using EveBuyback.Domain;

namespace Evebuyback.Data;

public class EsiOrderRepository : IOrderRepository
{
    private static readonly HttpClient _httpClient = CreateClient();

    public async Task<IEnumerable<Order>> GetOrders(
        Station station, 
        int itemTypeId, 
        DateTime currentDateTime,
        CancellationToken token)
    {
        var orders = new List<Order>();
        
        int page = 1;
        IEnumerable<Order>? pageOrders;
        while ((pageOrders = await GetNextPage(station, itemTypeId, page, currentDateTime)) != null)
        {
            token.ThrowIfCancellationRequested();
            page++;
            orders.AddRange(pageOrders);
        }

        return orders;
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

    private static async Task<IEnumerable<Order>?> GetNextPage(
        Station station, 
        int itemTypeId, 
        int page, 
        DateTime currentDateTime)
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