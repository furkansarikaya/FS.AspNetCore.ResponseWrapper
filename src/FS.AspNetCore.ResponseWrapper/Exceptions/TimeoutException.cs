namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when operations exceed their allowed execution time limits.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 408 Request Timeout response for operation timeout scenarios.
/// </summary>
/// <remarks>
/// Timeout exceptions indicate that an operation could not be completed within the allowed time frame.
/// These exceptions help communicate resource availability issues and can guide client applications
/// in implementing appropriate retry strategies or alternative operation approaches.
/// </remarks>
public class TimeoutException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the TimeoutException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the timeout condition.</param>
    public TimeoutException(string message) : base(message, "TIMEOUT")
    {
    }

    /// <summary>
    /// Initializes a new instance of the TimeoutException class with enhanced timeout identification.
    /// </summary>
    /// <param name="message">The message that describes the timeout condition.</param>
    /// <param name="code">The application-specific error code that identifies the type of timeout.</param>
    public TimeoutException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the TimeoutException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the timeout condition.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that caused the timeout.</param>
    public TimeoutException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}
