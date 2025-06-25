namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when data validation fails during request processing.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 400 Bad Request response with detailed validation error information.
/// </summary>
/// <remarks>
/// Validation exceptions are designed to work seamlessly with FluentValidation results, automatically
/// extracting and organizing validation failures into a structured format that's easy for API consumers
/// to process. The exception groups validation errors by property name and provides comprehensive
/// error details for each failed validation rule.
/// 
/// This exception type enables comprehensive validation error reporting, allowing multiple validation
/// failures to be communicated to clients in a single response rather than requiring multiple
/// request-response cycles to identify all validation issues.
/// </remarks>
public class ValidationException : ApplicationExceptionBase
{
    /// <summary>
    /// Gets a read-only dictionary containing validation errors organized by property name.
    /// Each key represents a property that failed validation, and the corresponding value
    /// contains an array of error messages for that property.
    /// </summary>
    /// <value>
    /// A dictionary where keys are property names and values are arrays of error messages
    /// for the respective properties. This structure enables easy client-side processing
    /// of validation errors and supports field-specific error display in user interfaces.
    /// </value>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationException class with validation failures from FluentValidation.
    /// The constructor automatically processes the validation failures and organizes them into a structured
    /// format that's optimized for API response generation and client-side error handling.
    /// </summary>
    /// <param name="failures">
    /// A collection of ValidationFailure objects from FluentValidation that describe the specific
    /// validation rules that were violated. These failures are automatically grouped by property
    /// name and converted into a structured error dictionary.
    /// </param>
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}