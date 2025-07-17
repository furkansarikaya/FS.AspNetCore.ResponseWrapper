namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a request is malformed or contains invalid data that cannot be processed.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 400 Bad Request response for general request format or content issues.
/// </summary>
/// <remarks>
/// Bad request exceptions cover general request issues that don't fall into more specific categories
/// like validation errors or business rule violations. These exceptions typically indicate problems
/// with request format, structure, or content that prevent the server from understanding or processing the request.
/// </remarks>
public class BadRequestException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the BadRequestException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the request issue.</param>
    public BadRequestException(string message) : base(message, "BAD_REQUEST")
    {
    }

    /// <summary>
    /// Initializes a new instance of the BadRequestException class with enhanced request error identification.
    /// </summary>
    /// <param name="message">The message that describes the request issue.</param>
    /// <param name="code">The application-specific error code that identifies the type of request problem.</param>
    public BadRequestException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BadRequestException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the request issue.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that caused the request processing failure.</param>
    public BadRequestException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}