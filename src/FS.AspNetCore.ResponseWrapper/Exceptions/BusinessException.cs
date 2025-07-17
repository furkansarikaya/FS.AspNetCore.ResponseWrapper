namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when business logic rules are violated during application execution.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 400 Bad Request response with customizable error messaging and code identification.
/// </summary>
/// <remarks>
/// Business exceptions represent expected application behavior when domain-specific business rules
/// are violated. These exceptions are considered normal application flow rather than technical failures,
/// and are designed to communicate business rule violations clearly to both users and client applications.
/// 
/// **Domain Rule Enforcement**: Business exceptions serve as the enforcement mechanism for domain-specific
/// rules such as inventory limits, account restrictions, workflow state requirements, or any other
/// business logic constraints that cannot be expressed through simple data validation.
/// 
/// **Expected Error Flow**: Unlike technical exceptions that represent unexpected failures, business
/// exceptions are anticipated outcomes of business logic evaluation. They are logged at information
/// level rather than error level to avoid noise in error monitoring systems.
/// 
/// **Client Integration**: The error codes provided by business exceptions enable client applications
/// to implement sophisticated business rule handling, including context-specific user messaging,
/// alternative workflow suggestions, or automated remediation actions.
/// </remarks>
public class BusinessException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the BusinessException class with a specified error message.
    /// The exception will result in an HTTP 400 Bad Request response with the provided message.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    public BusinessException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BusinessException class with a specified error message
    /// and error code for enhanced client-side business rule handling.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    /// <param name="code">The application-specific error code that identifies the specific business rule violation.</param>
    /// <remarks>
    /// This constructor enables sophisticated client-side handling of business rule violations
    /// by providing both user-friendly messages and machine-readable error identification.
    /// Error codes should reflect the specific business rule that was violated to enable
    /// targeted client-side responses and user experience flows.
    /// </remarks>
    public BusinessException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BusinessException class with a specified error message
    /// and a reference to the inner exception that caused the business rule evaluation to fail.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    /// <param name="innerException">The underlying exception that caused the business rule evaluation failure.</param>
    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the BusinessException class with complete error information
    /// including message, error code, and inner exception reference.
    /// </summary>
    /// <param name="message">The message that describes the business rule violation.</param>
    /// <param name="code">The application-specific error code that identifies the specific business rule violation.</param>
    /// <param name="innerException">The underlying exception that caused the business rule evaluation failure.</param>
    public BusinessException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}