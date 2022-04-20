using System.Text.Json.Serialization;

namespace Evebuyback.Data;

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