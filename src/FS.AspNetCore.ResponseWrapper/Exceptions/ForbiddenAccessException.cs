namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a user attempts to access a resource for which they lack sufficient permissions.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 403 Forbidden response with customizable error messaging and permission-specific error codes.
/// </summary>
/// <remarks>
/// Forbidden access exceptions indicate successful authentication but insufficient authorization for the requested
/// resource or operation. These exceptions should provide helpful guidance to users about permission requirements
/// while maintaining security by not exposing unnecessary details about the authorization system.
/// </remarks>
public class ForbiddenAccessException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the ForbiddenAccessException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the access restriction.</param>
    public ForbiddenAccessException(string message) : base(message, "FORBIDDEN")
    {
    }

    /// <summary>
    /// Initializes a new instance of the ForbiddenAccessException class with a specified error message
    /// and error code for enhanced client-side permission handling.
    /// </summary>
    /// <param name="message">The message that describes the access restriction.</param>
    /// <param name="code">The application-specific error code that identifies the type of permission violation.</param>
    public ForbiddenAccessException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ForbiddenAccessException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the access restriction.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that occurred during authorization.</param>
    public ForbiddenAccessException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}