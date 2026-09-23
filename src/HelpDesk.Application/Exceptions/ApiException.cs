namespace HelpDesk.Application.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message)
        : base(message, 400)
    {
    }
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message)
        : base(message, 401)
    {
    }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(message, 403)
    {
    }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
