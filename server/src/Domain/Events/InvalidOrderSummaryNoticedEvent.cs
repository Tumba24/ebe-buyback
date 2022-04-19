namespace EveBuyback.Domain;

public record InvalidOrderSummaryNoticedEvent(ItemType Item, int Volume);