using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(IEnumerable<BuybackItem> Items) : IRequest<decimal>;

public record BuybackItem(string orderTypeName, int volume);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, decimal>
{
    public async Task<decimal> Handle(BuybackQuery query, CancellationToken token)
    {
        await Task.CompletedTask;

        var aggregate = new RegionOrderSummaryAggregate(
            orderNameLookup: new Dictionary<int, string>(),
            orderSummaryLookup: new Dictionary<int, OrderSummary>(),
            orderTypeIdLookup: new Dictionary<string, int>(),
            regionId: 10000002
        );

        foreach (var item in query.Items)
            aggregate.RefreshOrderSummary(item.orderTypeName, item.volume);

        var invalidEvents = aggregate.DomainEvents?
            .Where(e => e is InvalidOrderSummaryNoticedEvent)?
            .Select(e => e as InvalidOrderSummaryNoticedEvent) ?? Enumerable.Empty<InvalidOrderSummaryNoticedEvent>();

        foreach (var invalidEvent in invalidEvents)
        {
            if (invalidEvent == null)
                throw new InvalidOperationException();
            
            aggregate.UpdateOrderSummary(invalidEvent.OrderTypeId, invalidEvent.OrderTypeName, 1000000, new Order[0]);
        }

        return 2.0m;
    }
}