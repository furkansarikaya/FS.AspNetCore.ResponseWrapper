namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Enhanced base class for all application-specific exceptions with built-in error code support.
/// This abstract class provides a consistent foundation for structured exception handling across
/// the entire application, enabling rich client-side error processing through machine-readable error codes.
/// </summary>
/// <remarks>
/// This enhanced base class transforms exception handling from simple message-based communication
/// to a sophisticated error management system that supports modern API requirements. The error code
/// system enables consistent, machine-readable error identification that transcends language barriers
/// and provides reliable foundation for client-side error handling logic.
/// 
/// **Structured Error Communication**: By providing both human-readable messages and machine-readable
/// codes, applications can deliver appropriate user experiences while maintaining programmatic
/// error handling capabilities. This dual approach ensures that errors are both user-friendly
/// and developer-friendly.
/// 
/// **Consistency Across Exceptions**: All derived exceptions automatically inherit error code
/// capabilities, ensuring that the entire exception hierarchy provides consistent error identification
/// mechanisms. This eliminates the need to remember which exceptions support codes and which don't.
/// 
/// **Client Integration Support**: Modern client applications can implement sophisticated error
/// handling strategies based on error codes, including conditional retry logic, specific user
/// messaging, and context-aware error recovery flows.
/// 
/// **Localization and Internationalization**: Error codes provide stable identifiers that remain
/// constant across different languages and locales, while messages can be localized based on
/// user preferences or system configuration.
/// 
/// **API Evolution Support**: Error codes enable backward-compatible API evolution where new
/// error conditions can be introduced with new codes while maintaining existing error handling
/// logic for known codes.
/// </remarks>
public abstract class ApplicationExceptionBase : Exception
{
    /// <summary>
    /// Gets the application-specific error code that provides semantic meaning for this exception.
    /// This code enables sophisticated client-side error processing and provides stable identification
    /// across different application versions and localization contexts.
    /// </summary>
    /// <value>
    /// A string representing the error code, or null if no specific code is assigned.
    /// Error codes should follow consistent naming conventions and remain stable across
    /// application versions to ensure reliable client-side error handling.
    /// </value>
    /// <remarks>
    /// The error code serves as a contract between the API and its consumers, providing:
    /// 
    /// **Stable Identification**: Unlike error messages that might change due to UX improvements
    /// or localization, error codes provide consistent identification that client applications
    /// can rely on for conditional logic and error handling strategies.
    /// 
    /// **Machine-Readable Processing**: Error codes enable sophisticated client-side logic
    /// such as automatic retry mechanisms, specific user interface responses, or integration
    /// with monitoring and alerting systems.
    /// 
    /// **Cross-Language Compatibility**: Error codes transcend language barriers, providing
    /// consistent error identification regardless of the user's locale or the application's
    /// internationalization settings.
    /// 
    /// **Hierarchical Organization**: Error codes can follow hierarchical naming conventions
    /// (e.g., "AUTH_EMAIL_REQUIRED", "AUTH_2FA_REQUIRED") that enable both specific and
    /// category-based error handling in client applications.
    /// 
    /// **Documentation and Support**: Error codes provide precise references for documentation,
    /// support tickets, and troubleshooting guides, enabling more effective communication
    /// about specific error conditions.
    /// </remarks>
    public string? Code { get; }

    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message.
    /// This constructor provides basic exception functionality without error code assignment.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    /// <remarks>
    /// This constructor maintains backward compatibility with existing exception usage patterns
    /// while providing the foundation for enhanced error code functionality in derived classes.
    /// </remarks>
    protected ApplicationExceptionBase(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message
    /// and application-specific error code for enhanced client-side error handling.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    /// <param name="code">The application-specific error code that provides semantic meaning for client-side processing.</param>
    /// <remarks>
    /// This constructor enables rich error communication by combining human-readable messages
    /// with machine-readable codes. The error code should follow consistent naming conventions
    /// and remain stable across application versions to ensure reliable client-side integration.
    /// </remarks>
    protected ApplicationExceptionBase(string message, string code) : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
    /// <remarks>
    /// This constructor supports exception chaining scenarios where the current exception
    /// provides additional context or abstraction over an underlying technical exception.
    /// </remarks>
    protected ApplicationExceptionBase(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ApplicationExceptionBase class with a specified error message,
    /// error code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error condition that caused the exception.</param>
    /// <param name="code">The application-specific error code that provides semantic meaning for client-side processing.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
    /// <remarks>
    /// This constructor provides complete exception functionality, combining error code support
    /// with exception chaining capabilities for comprehensive error context and handling.
    /// </remarks>
    protected ApplicationExceptionBase(string message, string code, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}