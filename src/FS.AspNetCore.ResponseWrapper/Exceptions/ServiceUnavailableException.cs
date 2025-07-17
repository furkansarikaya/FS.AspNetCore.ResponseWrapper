namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when required services or dependencies are temporarily unavailable.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 503 Service Unavailable response for service dependency issues.
/// </summary>
/// <remarks>
/// Service unavailable exceptions indicate temporary service issues that may resolve automatically.
/// These exceptions help communicate service health status and can guide client applications
/// in implementing appropriate retry strategies or degraded functionality modes.
/// </remarks>
public class ServiceUnavailableException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the ServiceUnavailableException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the service availability issue.</param>
    public ServiceUnavailableException(string message) : base(message, "SERVICE_UNAVAILABLE")
    {
    }

    /// <summary>
    /// Initializes a new instance of the ServiceUnavailableException class with enhanced service identification.
    /// </summary>
    /// <param name="message">The message that describes the service availability issue.</param>
    /// <param name="code">The application-specific error code that identifies the unavailable service or dependency.</param>
    public ServiceUnavailableException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ServiceUnavailableException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the service availability issue.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that caused the service unavailability.</param>
    public ServiceUnavailableException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}