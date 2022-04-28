namespace EveBuyback.Domain;

public record InvalidOrderSummaryNoticedEvent(ItemType Item, long Volume);