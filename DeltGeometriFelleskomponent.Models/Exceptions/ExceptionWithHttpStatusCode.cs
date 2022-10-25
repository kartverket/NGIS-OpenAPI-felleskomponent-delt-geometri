using System.Net;


namespace DeltGeometriFelleskomponent.Models.Exceptions;

public class ExceptionWithHttpStatusCode: Exception
{
    private readonly HttpStatusCode _statusCode;

    public ExceptionWithHttpStatusCode(string message, HttpStatusCode statusCode): base(message) {
        _statusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get => _statusCode; }
}

