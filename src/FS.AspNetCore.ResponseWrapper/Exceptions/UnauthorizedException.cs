namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when authentication is required but not provided or when authentication credentials are invalid.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 401 Unauthorized response with authentication-specific error codes.
/// </summary>
/// <remarks>
/// Unauthorized exceptions indicate authentication failures rather than authorization issues.
/// These exceptions should guide users toward appropriate authentication mechanisms while
/// maintaining security by not providing information that could assist authentication bypass attempts.
/// </remarks>
public class UnauthorizedException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the UnauthorizedException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the authentication requirement or failure.</param>
    public UnauthorizedException(string message) : base(message, "UNAUTHORIZED")
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnauthorizedException class with enhanced authentication error identification.
    /// </summary>
    /// <param name="message">The message that describes the authentication requirement or failure.</param>
    /// <param name="code">The application-specific error code that identifies the type of authentication issue.</param>
    public UnauthorizedException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the UnauthorizedException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the authentication requirement or failure.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that occurred during authentication.</param>
    public UnauthorizedException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}