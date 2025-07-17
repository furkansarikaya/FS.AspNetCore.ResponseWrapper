namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when data validation fails during request processing.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 400 Bad Request response with detailed validation error information and error code identification.
/// </summary>
/// <remarks>
/// Validation exceptions enable comprehensive error reporting for complex validation scenarios,
/// particularly valuable when multiple validation rules fail simultaneously. The exception provides
/// detailed error information organized by property name while maintaining consistent error code
/// identification for client-side validation handling.
/// </remarks>
public class ValidationException : ApplicationExceptionBase
{
    /// <summary>
    /// Gets a read-only dictionary containing validation errors organized by property name.
    /// Each key represents a property that failed validation, and the corresponding value
    /// contains an array of error messages for that property.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class with validation failures from FluentValidation.
    /// The constructor automatically processes the validation failures and organizes them into a structured
    /// format optimized for API response generation and client-side error handling.
    /// </summary>
    /// <param name="failures">A collection of ValidationFailure objects from FluentValidation.</param>
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    /// <summary>
    /// Initializes a new instance of the ValidationException class with a custom validation error message.
    /// </summary>
    /// <param name="message">The custom validation error message.</param>
    public ValidationException(string message) : base(message, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the ValidationException class with a custom message and error code.
    /// </summary>
    /// <param name="message">The custom validation error message.</param>
    /// <param name="code">The specific validation error code.</param>
    public ValidationException(string message, string code) : base(message, code)
    {
        Errors = new Dictionary<string, string[]>();
    }
}