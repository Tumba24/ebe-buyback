using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EveBuyback.App;

[ApiController]
[Route("buyback")]
public class BuybackController : ControllerBase
{
    private readonly IMediator _mediator;

    public BuybackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Consumes("text/plain")]
    [HttpPost("calculate")]
    public async Task<ActionResult<decimal>> Calculate(
        [FromBody] string rawInput,
        string station = "Jita",
        bool shouldCalculateBuybackAfterRefinement = true,
        decimal buybackTaxPercentage = 10,
        decimal buybackEfficiencyPercentage = 75)
    {
        var contractResult = await _mediator.Send(new ContractQuery(rawInput));
        if (!contractResult.OK)
            return BadRequest(contractResult.ErrorMessage);

        var contractItems = new List<ContractQueryItem>(contractResult.Items ?? Enumerable.Empty<ContractQueryItem>());

        if (shouldCalculateBuybackAfterRefinement)
        {
            var refinedQueryItems = contractItems
                .Select(i => new RefinedQueryItem(i.ItemTypeName, i.Volume));

            var refinementResult = await _mediator.Send(new RefinedQuery(refinedQueryItems, buybackEfficiencyPercentage));
            
            if (!refinementResult.OK)
                return BadRequest(refinementResult.ErrorMessage);

            contractItems.Clear();
            contractItems.AddRange(refinementResult.Items
                .Select(i => new ContractQueryItem(i.ItemTypeName, i.Volume)));
        }

        var refreshCommandItems = contractItems
            .Select(i => new OrderSummaryRefreshCommandItem(i.ItemTypeName, i.Volume));

        var refreshResult = await _mediator.Send(new OrderSummaryRefreshCommand(station, refreshCommandItems));
        if (!refreshResult.OK)
            return BadRequest(refreshResult.ErrorMessage);

        var buybackQueryItems = contractItems
            .Select(i => new BuybackQueryItem(i.ItemTypeName, i.Volume));

        var buybackResult = await _mediator.Send(
            new BuybackQuery(
                station, 
                buybackQueryItems,
                buybackTaxPercentage));

        if (buybackResult.OK)
            return buybackResult.BuybackAmount;

        return BadRequest(buybackResult.ErrorMessage);
    }
}