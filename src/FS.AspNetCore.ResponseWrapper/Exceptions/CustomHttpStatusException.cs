namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that require specific HTTP status codes not covered by other exception types.
/// This exception provides complete control over the HTTP response status code while maintaining
/// the structured error response format and error code identification system.
/// </summary>
/// <remarks>
/// This exception type provides maximum flexibility for scenarios where applications need to return
/// specific HTTP status codes that don't align with the predefined exception types. It enables
/// custom error scenarios while maintaining consistency with the ResponseWrapper error handling system.
/// 
/// **Custom Status Code Support**: Applications can specify any valid HTTP status code, enabling
/// support for specialized response scenarios such as custom business status codes, integration
/// requirements, or domain-specific error classifications.
/// 
/// **Consistent Error Structure**: Despite the flexible status code, the exception maintains the
/// same structured error response format as other exception types, ensuring consistent client-side
/// error handling regardless of the specific HTTP status code used.
/// 
/// **Framework Integration**: The exception integrates seamlessly with the GlobalExceptionHandlingMiddleware
/// to ensure that custom status codes are properly applied to HTTP responses while maintaining
/// all other ResponseWrapper functionality such as error codes, metadata, and logging.
/// </remarks>
public class CustomHttpStatusException : ApplicationExceptionBase
{
    /// <summary>
    /// Gets the HTTP status code that should be returned in the response for this exception.
    /// This enables precise control over the HTTP semantics of the error response while
    /// maintaining structured error information and consistent client-side handling.
    /// </summary>
    /// <value>
    /// The HTTP status code to be used in the response. This should be a valid HTTP status code
    /// appropriate for the error condition being represented.
    /// </value>
    public int HttpStatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the CustomHttpStatusException class with a specified error message and HTTP status code.
    /// </summary>
    /// <param name="message">The message that describes the error condition.</param>
    /// <param name="httpStatusCode">The HTTP status code to be returned in the response.</param>
    /// <remarks>
    /// This constructor enables custom error responses with specific HTTP status codes while
    /// automatically generating an error code based on the status code value for consistent identification.
    /// </remarks>
    public CustomHttpStatusException(string message, int httpStatusCode) 
        : base(message, $"HTTP_{httpStatusCode}")
    {
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the CustomHttpStatusException class with complete error specification.
    /// </summary>
    /// <param name="message">The message that describes the error condition.</param>
    /// <param name="httpStatusCode">The HTTP status code to be returned in the response.</param>
    /// <param name="code">The application-specific error code for client-side error identification.</param>
    /// <remarks>
    /// This constructor provides complete control over both HTTP status code and error code,
    /// enabling sophisticated error communication strategies that align with specific application
    /// requirements or integration needs.
    /// </remarks>
    public CustomHttpStatusException(string message, int httpStatusCode, string code) 
        : base(message, code)
    {
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the CustomHttpStatusException class with inner exception support.
    /// </summary>
    /// <param name="message">The message that describes the error condition.</param>
    /// <param name="httpStatusCode">The HTTP status code to be returned in the response.</param>
    /// <param name="code">The application-specific error code for client-side error identification.</param>
    /// <param name="innerException">The underlying exception that caused this error condition.</param>
    public CustomHttpStatusException(string message, int httpStatusCode, string code, Exception innerException) 
        : base(message, code, innerException)
    {
        HttpStatusCode = httpStatusCode;
    }
}