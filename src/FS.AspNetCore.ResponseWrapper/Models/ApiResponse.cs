namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Represents a standardized API response wrapper that provides consistent structure for all API endpoints.
/// This generic class encapsulates response data along with success indicators, error information, and
/// comprehensive metadata to ensure uniform API responses across the entire application.
/// </summary>
/// <typeparam name="T">The type of data being returned in the response. This can be any serializable type including complex objects, collections, or primitives.</typeparam>
/// <remarks>
/// The ApiResponse class serves as the cornerstone of the ResponseWrapper framework, implementing a
/// consistent response pattern that dramatically improves API usability and client development experience.
/// Think of it as a standardized envelope that wraps your actual business data with additional context
/// and metadata that makes APIs easier to work with.
/// 
/// This wrapper solves several common API design challenges:
/// 
/// **Consistency Challenge**: Without a standard response format, different endpoints might return
/// data in completely different structures, making client development more complex and error-prone.
/// ApiResponse ensures every endpoint follows the same pattern.
/// 
/// **Error Handling Challenge**: Traditional APIs often use only HTTP status codes for error communication,
/// which provides limited information. ApiResponse includes detailed error collections and user-friendly
/// messages that help both developers and end users understand what went wrong.
/// 
/// **Metadata Challenge**: Modern APIs often need to include timing information, correlation IDs,
/// and other diagnostic data. ApiResponse provides a structured metadata section that can include
/// this information without cluttering the business data.
/// 
/// **Success Detection Challenge**: Clients often need to programmatically determine if an operation
/// succeeded. The Success boolean provides an immediate, unambiguous indicator that doesn't require
/// parsing HTTP status codes or examining data structures.
/// 
/// The class is automatically applied by the ApiResponseWrapperFilter, which means developers can
/// focus on returning their business data while the framework handles the consistent wrapping.
/// This separation of concerns keeps controller code clean while ensuring API consistency.
/// </remarks>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the API operation completed successfully.
    /// This boolean flag provides a quick way for API consumers to determine the outcome
    /// of their request without needing to check HTTP status codes or error collections.
    /// </summary>
    /// <value>
    /// true if the operation completed successfully; false if any errors occurred during processing.
    /// This value should be checked first by API consumers before attempting to access the Data property.
    /// </value>
    /// <remarks>
    /// The Success property serves as the primary indicator of operation outcome and is designed
    /// to eliminate ambiguity in API responses. Here's how to think about its usage:
    /// 
    /// **For API Consumers**: Always check this property first. If it's true, the Data property
    /// contains valid results. If it's false, examine the Errors collection for details about
    /// what went wrong. This approach provides a consistent pattern across all API endpoints.
    /// 
    /// **For API Developers**: This property is automatically set by the factory methods.
    /// SuccessResult() sets it to true, while ErrorResult() methods set it to false. You
    /// typically don't need to set this manually unless creating custom response objects.
    /// 
    /// **Design Philosophy**: This explicit success indicator follows the "principle of least
    /// surprise" in API design. Rather than requiring clients to interpret HTTP status codes
    /// or check for null data, the Success flag provides immediate, unambiguous feedback
    /// about the operation outcome.
    /// </remarks>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the actual response data returned by the API operation.
    /// This property contains the business data requested by the client and will be null
    /// for unsuccessful operations or operations that don't return data.
    /// </summary>
    /// <value>
    /// The response data of type T, or null if the operation failed or returned no data.
    /// API consumers should check the Success property before accessing this data to ensure
    /// it contains valid information.
    /// </value>
    /// <remarks>
    /// The Data property represents the core business value of the API response - it's what
    /// the client actually requested. Understanding its relationship with other properties
    /// is crucial for effective API usage:
    /// 
    /// **When Success is true**: The Data property contains the requested information. This
    /// could be a single object, a collection, or even null for operations that don't return
    /// data (like delete operations that only confirm completion).
    /// 
    /// **When Success is false**: The Data property will be null, and the Errors collection
    /// will contain information about what went wrong. This separation ensures that error
    /// responses don't contain partial or invalid data that might confuse clients.
    /// 
    /// **Type Safety**: The generic type T ensures compile-time type safety for strongly-typed
    /// clients while maintaining flexibility for different kinds of operations. The same
    /// response structure works for simple primitives, complex objects, and collections.
    /// 
    /// **Null Handling**: The nullable nature of this property aligns with operations that
    /// might legitimately return no data (like successful delete operations) while distinguishing
    /// them from error conditions through the Success flag.
    /// </remarks>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable message describing the operation result.
    /// This message can provide additional context about the operation, whether successful or failed,
    /// and is suitable for display to end users or for logging purposes.
    /// </summary>
    /// <value>
    /// A descriptive message about the operation result, or null if no specific message is needed.
    /// For successful operations, this might contain confirmation text. For errors, it typically
    /// contains a user-friendly description of what went wrong.
    /// </value>
    /// <remarks>
    /// The Message property serves as a bridge between technical operation results and human-readable
    /// communication. Its usage varies depending on the operation outcome and intended audience:
    /// 
    /// **For Successful Operations**: Messages might include confirmations like "User created successfully"
    /// or "Data exported to file". These messages can be displayed directly to users or used in
    /// logging for operational visibility.
    /// 
    /// **For Error Conditions**: Messages should be user-friendly explanations that help users
    /// understand what went wrong and potentially how to fix it. For example, "The email address
    /// is already in use" is more helpful than "Unique constraint violation".
    /// 
    /// **Localization Considerations**: While the ResponseWrapper framework doesn't handle localization
    /// directly, the Message property is an ideal place to include localized text based on the
    /// user's preferences or the Accept-Language header.
    /// 
    /// **Relationship with Errors**: While the Message provides a general description, the Errors
    /// collection contains specific, actionable error details. Think of Message as the summary
    /// and Errors as the detailed breakdown.
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a collection of error messages that occurred during the operation.
    /// This collection provides detailed information about validation failures, business rule
    /// violations, or other issues that prevented successful completion of the request.
    /// </summary>
    /// <value>
    /// A list of error messages describing specific issues that occurred. For successful operations,
    /// this collection will be empty. For failed operations, it contains detailed error information
    /// that can help API consumers understand and resolve the issues.
    /// </value>
    /// <remarks>
    /// The Errors collection is designed to provide comprehensive, actionable feedback about what
    /// went wrong during operation processing. Understanding its structure and usage patterns
    /// is essential for building robust API clients:
    /// 
    /// **Validation Scenarios**: When input validation fails, this collection might contain multiple
    /// entries like "Email is required", "Password must be at least 8 characters", etc. This allows
    /// clients to address all validation issues in a single request-response cycle.
    /// 
    /// **Business Rule Violations**: For business logic errors, the collection might contain
    /// domain-specific messages like "Insufficient inventory for this order" or "Account is
    /// suspended and cannot perform this action".
    /// 
    /// **Multiple Error Handling**: Unlike simple error responses that might only show the first
    /// error encountered, this collection can capture all errors that occurred during processing,
    /// providing a complete picture of what needs to be addressed.
    /// 
    /// **Client Processing**: API clients should iterate through this collection to display all
    /// relevant error information to users. For form-based interfaces, specific validation errors
    /// can be associated with their corresponding input fields.
    /// 
    /// **Empty Collection Pattern**: For successful operations, this collection remains empty rather
    /// than being null, which simplifies client-side processing by eliminating null checks before
    /// iteration.
    /// </remarks>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets comprehensive metadata about the request and response processing.
    /// This metadata includes timing information, correlation IDs, pagination details,
    /// and other diagnostic information that supports monitoring, debugging, and analytics.
    /// </summary>
    /// <value>
    /// A ResponseMetadata object containing detailed information about the request processing,
    /// or null if metadata collection is disabled in the ResponseWrapper configuration.
    /// This metadata is particularly valuable for performance monitoring and distributed tracing.
    /// </value>
    /// <remarks>
    /// The Metadata property transforms API responses from simple data carriers into rich,
    /// observable interactions that support modern application monitoring and debugging needs.
    /// Think of metadata as the "black box recorder" for your API calls:
    /// 
    /// **Performance Monitoring**: Execution timing information helps identify slow endpoints
    /// and track performance trends over time. This data is invaluable for SLA monitoring
    /// and performance optimization efforts.
    /// 
    /// **Distributed Tracing**: Correlation IDs and request identifiers enable tracking of
    /// requests across multiple services in microservice architectures. This is crucial for
    /// debugging complex workflows that span multiple systems.
    /// 
    /// **Pagination Support**: For endpoints returning large datasets, metadata includes
    /// pagination information that clients need for navigation, such as total counts, page
    /// numbers, and availability of additional pages.
    /// 
    /// **Debugging Support**: Request paths, HTTP methods, and additional context information
    /// help developers understand the exact circumstances that led to a particular response,
    /// making debugging much more efficient.
    /// 
    /// **Operational Visibility**: Database query counts, execution times, and cache hit ratios
    /// provide insights into the operational characteristics of each request, supporting both
    /// performance optimization and capacity planning.
    /// 
    /// **Configuration Control**: The metadata can be selectively enabled or disabled through
    /// ResponseWrapper configuration, allowing teams to balance the value of diagnostic information
    /// against potential performance overhead in high-throughput scenarios.
    /// </remarks>
    public ResponseMetadata? Metadata { get; set; }
    
    /// <summary>
    /// Creates a successful API response with the specified data and optional message.
    /// This factory method provides a convenient way to construct positive responses
    /// with proper success indicators and optional confirmation messaging.
    /// </summary>
    /// <param name="data">The response data to include in the successful response.</param>
    /// <param name="message">Optional message describing the successful operation.</param>
    /// <returns>A new ApiResponse instance configured for success with the provided data and message.</returns>
    /// <remarks>
    /// This factory method embodies the "pit of success" design principle, making it easy to create
    /// properly configured success responses while ensuring consistency across the application.
    /// Here's how to think about its usage:
    /// 
    /// **Automatic Configuration**: The method automatically sets Success to true and ensures the
    /// Errors collection remains empty, eliminating the possibility of accidentally creating
    /// inconsistent response states.
    /// 
    /// **Data-First Design**: By requiring the data parameter, the method encourages developers
    /// to think about what value they're providing to API consumers, promoting better API design.
    /// 
    /// **Optional Messaging**: The optional message parameter supports scenarios where additional
    /// context is valuable (like "User created successfully") while not forcing unnecessary
    /// messages for simple data retrieval operations.
    /// 
    /// **Type Inference**: The generic method works with C#'s type inference, so calling
    /// SuccessResult(myData) automatically creates the correctly typed response without
    /// requiring explicit type parameters.
    /// 
    /// **Metadata Integration**: The returned response is ready for metadata injection by the
    /// ResponseWrapper system, which will automatically add timing, correlation, and other
    /// diagnostic information based on configuration settings.
    /// </remarks>
    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
    
    /// <summary>
    /// Creates an error API response with a single error message.
    /// This factory method provides a convenient way to construct error responses
    /// for scenarios where a single error condition needs to be communicated to the API consumer.
    /// </summary>
    /// <param name="error">The error message describing what went wrong during the operation.</param>
    /// <returns>A new ApiResponse instance configured for failure with the specified error message.</returns>
    /// <remarks>
    /// This factory method supports the common scenario where a single, clear error message
    /// is sufficient to communicate what went wrong. Understanding when to use this versus
    /// the multiple-error variant helps maintain appropriate error granularity:
    /// 
    /// **Single Error Scenarios**: Use this method for cases where one primary issue prevents
    /// operation completion, such as "User not found", "Insufficient permissions", or
    /// "Service temporarily unavailable".
    /// 
    /// **Automatic State Management**: The method automatically sets Success to false and
    /// ensures the Data property remains null, maintaining response consistency and preventing
    /// confusion about the operation outcome.
    /// 
    /// **Error Collection Initialization**: Even for single errors, the method creates a
    /// collection with one item, maintaining consistency with the multiple-error response
    /// format and simplifying client-side error processing logic.
    /// 
    /// **Extensibility**: If additional errors are discovered during processing, they can
    /// be added to the Errors collection of the returned response, providing flexibility
    /// for evolving error scenarios.
    /// 
    /// **User Experience**: Single error messages work well for scenarios where users need
    /// clear, direct feedback about why their operation couldn't be completed.
    /// </remarks>
    public static ApiResponse<T> ErrorResult(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = [error]
        };
    }
    
    /// <summary>
    /// Creates an error API response with multiple error messages.
    /// This factory method is designed for scenarios where multiple validation errors
    /// or business rule violations need to be communicated to the API consumer simultaneously.
    /// </summary>
    /// <param name="errors">A collection of error messages describing the various issues that occurred.</param>
    /// <returns>A new ApiResponse instance configured for failure with all specified error messages.</returns>
    /// <remarks>
    /// This factory method addresses the common challenge of comprehensive error reporting,
    /// particularly valuable in validation scenarios where multiple issues might be present.
    /// Here's how to maximize its effectiveness:
    /// 
    /// **Validation Efficiency**: Rather than forcing users through multiple request-response
    /// cycles to discover all validation issues, this method enables "fail fast, fail completely"
    /// error reporting where all problems are identified in a single operation.
    /// 
    /// **User Experience Optimization**: For form-based interfaces, receiving all validation
    /// errors at once allows users to fix all issues before resubmitting, dramatically
    /// improving the user experience compared to iterative error discovery.
    /// 
    /// **Business Rule Aggregation**: Complex business operations might violate multiple rules
    /// simultaneously. This method allows comprehensive reporting of all violations, helping
    /// users understand the complete set of requirements they need to meet.
    /// 
    /// **Error Prioritization**: The order of errors in the collection can be significant.
    /// Consider placing the most critical or actionable errors first to guide user attention
    /// effectively.
    /// 
    /// **Integration with Validation Frameworks**: This method works particularly well with
    /// validation frameworks like FluentValidation, which can produce multiple error results
    /// that map naturally to this error collection format.
    /// 
    /// **Empty Collection Handling**: The method gracefully handles empty error collections,
    /// though such usage might indicate a logic error in the calling code since error responses
    /// should typically contain at least one error message.
    /// </remarks>
    public static ApiResponse<T> ErrorResult(IEnumerable<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors.ToList()
        };
    }
}