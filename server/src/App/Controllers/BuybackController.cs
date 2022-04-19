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
        decimal buybackTaxPercentage = 10)
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
                    return new StatusCodeResult(StatusCodes.Status400BadRequest);

                if (!Int32.TryParse(parts[1], out var volume))
                    return new StatusCodeResult(StatusCodes.Status400BadRequest);

                items.Add(new BuybackItem(parts[0], volume));
            }
        }

        var result = await _mediator.Send(
            new BuybackQuery(
                station, 
                items, 
                shouldCalculateBuybackAfterRefinement, 
                buybackTaxPercentage));

        if (result.OK)
            return result.BuybackAmount;

        return BadRequest(result.errorMessage);
    }
}