using DeltGeometriFelleskomponent.Models.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;


namespace DeltGeometriFelleskomponent.Api.Controllers;

public class ErrorResponse
{
    public string Type { get; set; }
    public string Message { get; set; }

    public ErrorResponse(Exception ex)
    {
        Type = ex.GetType().Name;
        Message = ex.Message;
    }
}

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    [Route("error")]
    public ErrorResponse Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context.Error; // Your exception

        Response.StatusCode = GetStatusCode(exception);

        return new ErrorResponse(exception); // Your error model
    }

    private static int GetStatusCode(Exception exception)
        => exception switch
        {
            ExceptionWithHttpStatusCode ex => (int)ex.StatusCode,                
            _ => 500,
        };
}