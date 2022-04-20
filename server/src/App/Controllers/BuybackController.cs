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
        List<BuybackItem> items = new List<BuybackItem>();

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

                if (!Int32.TryParse(parts[1], out var volume))
                    return BadRequest("The second part of each line must be a valid 32bit integer that indicates volume.");

                items.Add(new BuybackItem(parts[0], volume));
            }
        }

        if (shouldCalculateBuybackAfterRefinement)
        {
            var refinementResult = await _mediator.Send(new RefinedQuery(items, buybackEfficiencyPercentage));
            if (!refinementResult.OK)
                return BadRequest(refinementResult.errorMessage);

            items.Clear();
            items.AddRange(refinementResult.Items);
        }

        var buybackResult = await _mediator.Send(
            new BuybackQuery(
                station, 
                items,
                buybackTaxPercentage));

        if (buybackResult.OK)
            return buybackResult.BuybackAmount;

        return BadRequest(buybackResult.errorMessage);
    }
}