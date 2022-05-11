using Evebuyback.Data;
using EveBuyback.Domain;
using MediatR;

namespace EveBuyback.App;

public record BuybackQuery(
    string? StationName, 
    IEnumerable<BuybackQueryItem> Items,
    decimal BuybackTaxPercentage) : IRequest<BackendQueryResult>;

public record BuybackQueryItem(string ItemTypeName, long Volume);

public record BackendQueryResult(decimal BuybackAmount, bool OK, string ErrorMessage);

internal class BuybackQueryHandler : IRequestHandler<BuybackQuery, BackendQueryResult>
{
    private readonly IItemTypeRepository _itemTypeRepository;
    private readonly InMemoryStationOrderSummaryAggregateRepository _stationOrderSummaryRepository;
    private readonly IStationRepository _stationRepository;

    public BuybackQueryHandler(
        IItemTypeRepository itemTypeRepository,
        IStationOrderSummaryAggregateRepository stationOrderSummaryRepository,
        IStationRepository stationRepository)
    {
        _itemTypeRepository = itemTypeRepository;
        _stationOrderSummaryRepository = (InMemoryStationOrderSummaryAggregateRepository)stationOrderSummaryRepository;
        _stationRepository = stationRepository;
    }

    public async Task<BackendQueryResult> Handle(BuybackQuery query, CancellationToken token)
    {
        var station = await _stationRepository.Get(query.StationName);
        if (station == null)
            return new BackendQueryResult(0, false, "Invalid station. Station not recognized.");

        var contractItems = await GetContractItems(query);
        token.ThrowIfCancellationRequested();

        var buybackAmount = 0.0m;

        foreach (var contractItem in contractItems)
        {
            var orderSummary = await _stationOrderSummaryRepository.GetOrderSummary(station, contractItem.ItemTypeName);
            buybackAmount += (orderSummary.Price * contractItem.Volume);
        }

        var tax = buybackAmount * (query.BuybackTaxPercentage / 100);
        
        buybackAmount -= tax;

        buybackAmount = Math.Round(buybackAmount, 2, MidpointRounding.AwayFromZero);

        return new BackendQueryResult(buybackAmount, true, string.Empty); 
    }

    private async Task<IEnumerable<ContractItem>> GetContractItems(BuybackQuery query)
    {
        var itemTypeLookup = await _itemTypeRepository.GetLookupByItemTypeName();

        var contractItems = new List<ContractItem>();

        foreach (var item in query.Items)
        {
            if (!itemTypeLookup.TryGetValue(item.ItemTypeName, out var itemType))
                itemType = new ItemType(0, item.ItemTypeName, 0);

            contractItems.Add(new ContractItem(itemType.Name, item.Volume));
        }

        return contractItems;
    }
}