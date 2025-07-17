namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a request conflicts with the current state of the target resource.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 409 Conflict response, typically used for resource state conflicts or duplicate resource creation attempts.
/// </summary>
/// <remarks>
/// Conflict exceptions indicate that the request could not be completed due to a conflict with the current
/// state of the resource. Common scenarios include attempting to create resources that already exist,
/// updating resources that have been modified by other processes, or performing operations that violate
/// unique constraints or business rules related to resource state.
/// </remarks>
public class ConflictException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the ConflictException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the resource conflict.</param>
    public ConflictException(string message) : base(message, "CONFLICT")
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConflictException class with enhanced conflict identification.
    /// </summary>
    /// <param name="message">The message that describes the resource conflict.</param>
    /// <param name="code">The application-specific error code that identifies the type of conflict.</param>
    public ConflictException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ConflictException class with complete error information.
    /// </summary>
    /// <param name="message">The message that describes the resource conflict.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that caused the conflict.</param>
    public ConflictException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}