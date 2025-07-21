namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Defines a contract for objects that contain human-readable messages describing operation outcomes.
/// This interface enables the ResponseWrapper system to automatically extract and promote
/// messages from response data to the top-level ApiResponse structure, providing consistent
/// message communication across all API endpoints.
/// </summary>
/// <remarks>
/// This interface completes the metadata extraction trinity alongside IHasStatusCode and IHasMetadata,
/// providing a comprehensive and consistent approach to extracting all types of metadata information
/// from response objects. The interface follows the same lightweight, non-intrusive design pattern
/// used throughout the ResponseWrapper framework.
/// 
/// **Consistent Interface Design**: By implementing IHasMessage, response objects participate in
/// the same automatic extraction process used for status codes and custom metadata, ensuring
/// that all metadata types are handled uniformly and predictably.
/// 
/// **Message Purpose**: Messages provide human-readable descriptions of operation outcomes that
/// can be displayed directly to users or used for logging and debugging purposes. Unlike status
/// codes which are machine-readable, messages are designed for human consumption.
/// 
/// **Automatic Promotion**: When the ResponseWrapper filter processes a response, it automatically
/// checks if the response data implements this interface. If it does, the message is extracted
/// and promoted to the ApiResponse level, ensuring consistent message availability to API consumers.
/// 
/// **Clean Data Separation**: Messages are automatically removed from the business data section
/// and placed in the appropriate ApiResponse property, maintaining clean separation between
/// business data and user communication.
/// </remarks>
/// <example>
/// <code>
/// public class UserRegistrationResult : IHasStatusCode, IHasMessage, IHasMetadata
/// {
///     public int UserId { get; set; }
///     public string Email { get; set; }
///     
///     public string StatusCode { get; set; } = "EMAIL_VERIFICATION_REQUIRED";
///     public string Message { get; set; } = "Account created successfully. Please verify your email.";
///     
///     public Dictionary&lt;string, object&gt; Metadata { get; set; } = new()
///     {
///         { "verificationSentTo", "user@example.com" },
///         { "canResend", true }
///     };
/// }
/// </code>
/// </example>
public interface IHasMessage
{
    /// <summary>
    /// Gets the human-readable message describing the operation outcome.
    /// This message provides context about the operation result and can be displayed
    /// directly to users or used for logging and debugging purposes.
    /// </summary>
    /// <value>
    /// A descriptive message about the operation result, or null if no specific message is needed.
    /// The message should be suitable for user display and provide clear, actionable information
    /// about the operation outcome.
    /// </value>
    /// <remarks>
    /// The message property serves as the primary human-readable communication channel between
    /// the API and its consumers. Understanding its role and best practices helps ensure
    /// effective user communication:
    /// 
    /// **User-Friendly Language**: Messages should use clear, non-technical language that
    /// users can understand and act upon. Avoid technical jargon or internal system details
    /// that don't help users understand what happened or what they need to do next.
    /// 
    /// **Actionable Information**: When possible, messages should include guidance about
    /// next steps or actions users can take. For example, "Please check your email to verify
    /// your account" is more helpful than "Verification required".
    /// 
    /// **Contextual Relevance**: Messages should provide appropriate context for the operation
    /// that was performed. Success messages might confirm what was accomplished, while error
    /// messages should explain what went wrong and how to resolve it.
    /// 
    /// **Localization Readiness**: While the ResponseWrapper framework doesn't handle
    /// localization directly, message content should be structured in a way that supports
    /// localization efforts, such as avoiding concatenated strings or embedded variables
    /// that might not translate well.
    /// 
    /// **Consistency**: Messages should follow consistent patterns and terminology across
    /// the application to provide a cohesive user experience and reduce confusion.
    /// </remarks>
    string? Message { get; }
}