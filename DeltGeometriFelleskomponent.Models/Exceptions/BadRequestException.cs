

namespace DeltGeometriFelleskomponent.Models.Exceptions;

public class BadRequestException: ExceptionWithHttpStatusCode
{
    public BadRequestException(string message): base(message, System.Net.HttpStatusCode.BadRequest) { }
}

