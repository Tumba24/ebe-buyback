using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EveBuyback.App;

[ApiController]
[Route("error")]
public class ErrorController : Controller
{
    [ApiExplorerSettings(IgnoreApi=true), Route("{statusCode}")]
    public IActionResult Error(int statusCode)
    {
        var errorMessage = GetErrorMessage();

        var errorResponse = JsonSerializer.Serialize(errorMessage);
        
        return new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };
    }

    private string GetErrorMessage()
    {
        var exceptionHandlerFeature =
            HttpContext.Features.Get<IExceptionHandlerFeature>();

        Exception? error = GetNonAggregateException(exceptionHandlerFeature?.Error);

        return string.IsNullOrWhiteSpace(error?.Message) ? 
            "An unknown error occurred." : 
            error.Message;
    }

    private static Exception? GetNonAggregateException(Exception? exception)
    {
        if (exception is AggregateException)
            return GetNonAggregateException(exception.InnerException);

        return exception;
    }
}