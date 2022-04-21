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
    [HttpPost("calculate-amount")]
    public async Task<ActionResult<decimal>> CalculateBuybackAmount(
        [FromBody] string rawInput,
        string station = "Jita",
        bool shouldCalculateBuybackAfterRefinement = true,
        decimal buybackTaxPercentage = 10,
        decimal buybackEfficiencyPercentage = 75)
    {
        var refinedQueryItems = new List<RefinedQueryItem>();

        using (StringReader reader = new StringReader(rawInput))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                
                line = line.Trim();
                
                var parts = line.Split(new string[] { "\t", "  " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return BadRequest("Each line should split into two parts. Parts should be split by a tab or two sapces.");

                if (!Int32.TryParse(parts[1], NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var volume))
                    return BadRequest("The second part of each line must be a valid 32bit integer that indicates volume.");

                refinedQueryItems.Add(new RefinedQueryItem(parts[0], volume));
            }
        }

        if (shouldCalculateBuybackAfterRefinement)
        {
            var refinementResult = await _mediator.Send(new RefinedQuery(refinedQueryItems, buybackEfficiencyPercentage));
            if (!refinementResult.OK)
                return BadRequest(refinementResult.errorMessage);

            refinedQueryItems.Clear();
            refinedQueryItems.AddRange(refinementResult.Items);
        }

        var refreshCommandItems = refinedQueryItems
            .Select(i => new OrderSummaryRefreshCommandItem(i.ItemTypeName, i.Volume));

        await _mediator.Send(new OrderSummaryRefreshCommand(station, refreshCommandItems));

        var buybackQueryItems = refinedQueryItems
            .Select(i => new BuybackQueryItem(i.ItemTypeName, i.Volume));

        var buybackResult = await _mediator.Send(
            new BuybackQuery(
                station, 
                buybackQueryItems,
                buybackTaxPercentage));

        if (buybackResult.OK)
            return buybackResult.BuybackAmount;

        return BadRequest(buybackResult.errorMessage);
    }
}