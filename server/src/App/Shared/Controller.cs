using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EveBuyback.App;

public class Controller : ControllerBase
{
    public override BadRequestObjectResult BadRequest([ActionResultObjectValue] object? error) =>
        base.BadRequest(error == null ? null : JsonSerializer.Serialize(error));
}