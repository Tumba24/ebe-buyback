namespace EveBuyback.Domain;

public record InvalidOrderSummaryNoticedEvent(int OrderTypeId, string OrderTypeName);