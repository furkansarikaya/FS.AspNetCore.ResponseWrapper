namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a requested resource cannot be found in the application.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 404 Not Found response with customizable error messaging and resource identification.
/// </summary>
/// <remarks>
/// Not found exceptions represent normal application flow when users request resources that do not exist
/// or have been removed. These exceptions provide clear communication about missing resources while
/// maintaining security by not exposing unnecessary system details about why resources might be unavailable.
/// 
/// **Resource Identification**: The exception provides flexible resource identification through both
/// structured name/key combinations and simple message-based approaches, accommodating different
/// application architectures and error communication strategies.
/// 
/// **Security Considerations**: Not found exceptions should provide helpful information to legitimate
/// users while avoiding information disclosure that might assist malicious actors in system reconnaissance.
/// The error codes and messages should be carefully crafted to balance usability with security.
/// 
/// **Client Experience**: Clear not found error communication helps users understand when they've
/// requested something that doesn't exist versus when they lack permission to access existing resources,
/// enabling appropriate user interface responses and navigation flows.
/// </remarks>
public class NotFoundException : ApplicationExceptionBase
{
    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a structured resource identifier.
    /// This constructor automatically formats the error message to include both the resource name and key
    /// for comprehensive resource identification in error responses.
    /// </summary>
    /// <param name="name">The name or type of the resource that was not found (e.g., "User", "Product", "Order").</param>
    /// <param name="key">The key or identifier that was used to search for the resource (e.g., ID, email, SKU).</param>
    /// <remarks>
    /// This constructor provides structured resource identification that enables consistent error messaging
    /// and logging across the application. The formatted message follows the pattern "{name} ({key}) was not found."
    /// which provides clear context about both what was being searched for and what identifier was used.
    /// 
    /// The structured approach also enables automatic error code generation based on the resource name,
    /// supporting systematic client-side error handling for different resource types.
    /// </remarks>
    public NotFoundException(string name, object key) : base($"{name} ({key}) was not found.", "NOT_FOUND")
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a custom error message.
    /// This constructor provides flexibility for scenarios where the standard name/key format
    /// is not appropriate or where more specific error communication is required.
    /// </summary>
    /// <param name="message">The custom message that describes the resource that was not found.</param>
    /// <remarks>
    /// This constructor enables custom error messaging for complex scenarios where the standard
    /// resource name and key format doesn't provide sufficient context or where business-specific
    /// language is more appropriate for the user experience.
    /// </remarks>
    public NotFoundException(string message) : base(message, "NOT_FOUND")
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a custom error message
    /// and specific error code for enhanced client-side resource handling.
    /// </summary>
    /// <param name="message">The custom message that describes the resource that was not found.</param>
    /// <param name="code">The application-specific error code that identifies the type of resource or search that failed.</param>
    /// <remarks>
    /// This constructor provides complete control over both error messaging and code assignment,
    /// enabling sophisticated client-side handling of different types of not found scenarios.
    /// Different error codes can trigger different user interface responses or recovery strategies.
    /// </remarks>
    public NotFoundException(string message, string code) : base(message, code)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with an inner exception reference.
    /// </summary>
    /// <param name="name">The name or type of the resource that was not found.</param>
    /// <param name="key">The key or identifier that was used to search for the resource.</param>
    /// <param name="innerException">The underlying exception that occurred during the search operation.</param>
    public NotFoundException(string name, object key, Exception innerException) 
        : base($"{name} ({key}) was not found.", "NOT_FOUND", innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with complete error information.
    /// </summary>
    /// <param name="message">The custom message that describes the resource that was not found.</param>
    /// <param name="code">The application-specific error code.</param>
    /// <param name="innerException">The underlying exception that occurred during the search operation.</param>
    public NotFoundException(string message, string code, Exception innerException) : base(message, code, innerException)
    {
    }
}