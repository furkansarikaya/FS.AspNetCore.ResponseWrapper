namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Serves as the base class for all application-specific exceptions in the ResponseWrapper framework.
/// This abstract class provides a consistent foundation for custom exception handling and ensures
/// that all application exceptions follow the same inheritance pattern and construction conventions.
/// </summary>
/// <remarks>
/// This base class is designed to integrate seamlessly with the GlobalExceptionHandlingMiddleware,
/// which provides specialized handling for different exception categories. By inheriting from this base class,
/// custom exceptions automatically participate in the structured error response system and benefit
/// from consistent error formatting, logging, and HTTP status code mapping.
/// 
/// The class follows standard .NET exception patterns by providing both simple message constructors
/// and inner exception support for comprehensive error chaining and debugging scenarios.
/// </remarks>
public abstract class ApplicationExceptionBase : Exception
{
    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    protected ApplicationExceptionBase(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
    protected ApplicationExceptionBase(string message, Exception innerException) : base(message, innerException)
    {
    }
}