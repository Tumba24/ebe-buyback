using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EveBuyback.App;

[ApiController]
[Route("reprocessing")]
public class ReprocessingController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReprocessingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Consumes("text/plain")]
    [HttpPost("calculate")]
    public async Task<ActionResult<IEnumerable<ContractQueryItem>>> Calculate(
        [FromBody] string rawInput,
        decimal buybackEfficiencyPercentage = 75)
    {
        var contractResult = await _mediator.Send(new ContractQuery(rawInput));
        if (!contractResult.OK)
            return BadRequest(contractResult.ErrorMessage);

        var contractItems = new List<ContractQueryItem>(contractResult.Items ?? Enumerable.Empty<ContractQueryItem>());

        var refinedQueryItems = contractItems
            .Select(i => new RefinedQueryItem(i.ItemTypeName, i.Volume));

        var refinementResult = await _mediator.Send(new RefinedQuery(refinedQueryItems, buybackEfficiencyPercentage));
        if (!refinementResult.OK)
            return BadRequest(refinementResult.ErrorMessage);

        return refinementResult.Items
            .Select(i => new ContractQueryItem(i.ItemTypeName, i.Volume))
            .ToList();
    }
}