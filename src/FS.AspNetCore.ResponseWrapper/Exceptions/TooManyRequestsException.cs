namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when rate limiting thresholds are exceeded.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 429 Too Many Requests response for rate limiting scenarios.
/// </summary>
/// <remarks>
/// Too many requests exceptions are used to communicate rate limiting enforcement to client applications.
/// These exceptions should include guidance about when clients can retry requests and help prevent
/// system overload while maintaining service availability for legitimate usage patterns.
/// </remarks>
public class TooManyRequestsException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the TooManyRequestsException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the rate limiting condition.</param>
    public TooManyRequestsException(string message) : base(message, "TOO_MANY_REQUESTS")
    {
    }

    /// <summary>
    /// Initializes a new instance of the TooManyRequestsException class with enhanced rate limiting identification.
    /// </summary>
    /// <param name="message">The message that describes the rate limiting condition.</param>
    /// <param name="code">The application-specific error code that identifies the type of rate limit exceeded.</param>
    public TooManyRequestsException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the TooManyRequestsException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the rate limiting condition.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that caused the rate limiting.</param>
    public TooManyRequestsException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}