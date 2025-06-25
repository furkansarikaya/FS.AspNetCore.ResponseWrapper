/// <summary>
/// Configuration class for customizing error messages returned by the GlobalExceptionHandlingMiddleware.
/// This class enables complete control over user-facing error messages while maintaining fallback
/// defaults to ensure the system never returns empty or null error messages.
/// </summary>
/// <remarks>
/// The error message configuration follows a hierarchical approach:
/// 1. If a custom message is provided for a specific error type, it will be used
/// 2. If no custom message is provided, a sensible default message will be used
/// 3. All messages support placeholder substitution for dynamic content
/// 
/// This design ensures that the system is resilient and always provides meaningful
/// error messages to users, while allowing complete customization for branding,
/// localization, and user experience requirements.
/// 
/// The configuration also supports future enhancements such as:
/// - Internationalization through resource managers
/// - Context-aware messaging based on user roles or application state
/// - A/B testing of error messages for user experience optimization
/// </remarks>
public class ErrorMessageConfiguration
{
    /// <summary>
    /// Gets or sets the error message displayed when validation errors occur.
    /// This typically happens when request data doesn't meet validation requirements.
    /// </summary>
    /// <value>
    /// Custom validation error message, or null to use the default message.
    /// Default: "Validation errors occurred"
    /// </value>
    /// <remarks>
    /// Validation errors are among the most common user-facing errors in APIs.
    /// Customizing this message allows you to:
    /// - Provide more user-friendly language
    /// - Match your application's tone and voice
    /// - Include specific guidance for users on how to fix their input
    /// - Support multiple languages through resource management
    /// 
    /// The message should be generic enough to cover all validation scenarios,
    /// as specific validation details are provided in the errors array.
    /// </remarks>
    /// <example>
    /// <code>
    /// ValidationErrorMessage = "Please check your input and try again"
    /// ValidationErrorMessage = "Lütfen girdiğiniz bilgileri kontrol edin"
    /// ValidationErrorMessage = "The information you provided contains errors"
    /// </code>
    /// </example>
    public string? ValidationErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when a requested resource is not found.
    /// This covers scenarios where users request data that doesn't exist or has been deleted.
    /// </summary>
    /// <value>
    /// Custom not found error message, or null to use the default message.
    /// Default: "Resource not found"
    /// </value>
    /// <remarks>
    /// Not found errors can be frustrating for users, especially if they don't understand
    /// why a resource they expect to exist cannot be found. Customizing this message allows you to:
    /// - Provide more helpful context about what might have happened
    /// - Suggest alternative actions users can take
    /// - Maintain consistency with your application's error handling approach
    /// - Avoid technical jargon that might confuse non-technical users
    /// 
    /// Consider making this message helpful rather than just informative.
    /// </remarks>
    /// <example>
    /// <code>
    /// NotFoundErrorMessage = "The item you're looking for could not be found"
    /// NotFoundErrorMessage = "Bu öğe bulunamadı veya kaldırılmış olabilir"
    /// NotFoundErrorMessage = "Sorry, we couldn't find what you're looking for"
    /// </code>
    /// </example>
    public string? NotFoundErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when access to a resource is forbidden.
    /// This occurs when users try to access resources they don't have permission for.
    /// </summary>
    /// <value>
    /// Custom forbidden access error message, or null to use the default message.
    /// Default: "Access forbidden"
    /// </value>
    /// <remarks>
    /// Forbidden access errors require careful messaging because they involve security
    /// and permissions. The message should be informative enough to help legitimate users
    /// understand what happened, but not so detailed as to provide information to potential
    /// attackers. Customizing this message allows you to:
    /// - Provide appropriate guidance for users who might need to request access
    /// - Match your application's security messaging strategy
    /// - Avoid overly technical or intimidating language
    /// - Include links to help or contact information where appropriate
    /// </remarks>
    /// <example>
    /// <code>
    /// ForbiddenAccessMessage = "You don't have permission to access this resource"
    /// ForbiddenAccessMessage = "Bu kaynağa erişim yetkiniz bulunmuyor"
    /// ForbiddenAccessMessage = "Access denied. Please contact support if you believe this is an error"
    /// </code>
    /// </example>
    public string? ForbiddenAccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when authentication is required but not provided.
    /// This occurs when users try to access protected resources without proper authentication.
    /// </summary>
    /// <value>
    /// Custom unauthorized access error message, or null to use the default message.
    /// Default: "Authentication required"
    /// </value>
    /// <remarks>
    /// Unauthorized access errors are often the first interaction users have with your
    /// authentication system. The message should be clear about what users need to do
    /// to resolve the issue. Customizing this message allows you to:
    /// - Provide clear instructions on how to authenticate
    /// - Direct users to appropriate login or registration flows
    /// - Maintain consistency with your application's authentication UX
    /// - Include helpful links or guidance for account recovery
    /// </remarks>
    /// <example>
    /// <code>
    /// UnauthorizedAccessMessage = "Please log in to access this resource"
    /// UnauthorizedAccessMessage = "Bu kaynağa erişmek için giriş yapmanız gerekiyor"
    /// UnauthorizedAccessMessage = "Authentication required. Please sign in to continue"
    /// </code>
    /// </example>
    public string? UnauthorizedAccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when business rule violations occur.
    /// These are application-specific errors that occur when operations violate domain rules.
    /// </summary>
    /// <value>
    /// Custom business rule error message, or null to use the default message.
    /// Default: "Business rule violation"
    /// </value>
    /// <remarks>
    /// Business rule violations are domain-specific errors that occur when operations
    /// conflict with your application's business logic. These errors are often the most
    /// important to customize because they directly relate to your specific use case.
    /// Customizing this message allows you to:
    /// - Use terminology that matches your domain and user base
    /// - Provide context about why the operation cannot be completed
    /// - Guide users toward valid alternative actions
    /// - Maintain consistency with your application's business language
    /// 
    /// Since business rules vary significantly between applications, this message
    /// should be tailored to your specific domain and user expectations.
    /// </remarks>
    /// <example>
    /// <code>
    /// BusinessRuleViolationMessage = "This operation cannot be completed due to business rules"
    /// BusinessRuleViolationMessage = "İş kuralları nedeniyle bu işlem gerçekleştirilemez"
    /// BusinessRuleViolationMessage = "The requested action conflicts with system policies"
    /// </code>
    /// </example>
    public string? BusinessRuleViolationMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when unexpected application errors occur.
    /// These are typically unhandled exceptions that shouldn't normally happen in production.
    /// </summary>
    /// <value>
    /// Custom application error message, or null to use the default message.
    /// Default: "Application error occurred"
    /// </value>
    /// <remarks>
    /// Application errors represent unexpected failures in your application logic.
    /// While these should be rare in production, when they do occur, the message should
    /// be reassuring and helpful to users. Customizing this message allows you to:
    /// - Provide reassurance that the issue is being addressed
    /// - Include contact information or support ticket creation guidance
    /// - Maintain your application's voice even during error scenarios
    /// - Avoid technical jargon that might alarm or confuse users
    /// 
    /// These messages should strike a balance between acknowledging the problem
    /// and maintaining user confidence in your application.
    /// </remarks>
    /// <example>
    /// <code>
    /// ApplicationErrorMessage = "We're experiencing technical difficulties. Please try again later"
    /// ApplicationErrorMessage = "Teknik bir sorun yaşanıyor. Lütfen daha sonra tekrar deneyin"
    /// ApplicationErrorMessage = "Something went wrong on our end. Our team has been notified"
    /// </code>
    /// </example>
    public string? ApplicationErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message displayed when completely unexpected system errors occur.
    /// This is the fallback message for any exception type not specifically handled.
    /// </summary>
    /// <value>
    /// Custom unexpected error message, or null to use the default message.
    /// Default: "An unexpected error occurred"
    /// </value>
    /// <remarks>
    /// Unexpected errors represent the highest level of failure in your application.
    /// These should be extremely rare in production, but when they occur, the message
    /// is crucial for maintaining user trust and providing appropriate guidance.
    /// Customizing this message allows you to:
    /// - Provide appropriate escalation guidance for users
    /// - Include emergency contact information if needed
    /// - Maintain your brand voice even in crisis scenarios
    /// - Give users confidence that the issue will be resolved
    /// 
    /// This message should be carefully crafted as it represents your application's
    /// response to its most severe failure scenarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// UnexpectedErrorMessage = "We encountered an unexpected error. Please contact support"
    /// UnexpectedErrorMessage = "Beklenmeyen bir hata oluştu. Lütfen destek ekibiyle iletişime geçin"
    /// UnexpectedErrorMessage = "System error detected. Our technical team has been alerted"
    /// </code>
    /// </example>
    public string? UnexpectedErrorMessage { get; set; }

    /// <summary>
    /// Gets the appropriate error message for validation exceptions, using custom message if provided
    /// or falling back to a sensible default. This method implements the message resolution strategy.
    /// </summary>
    /// <returns>The validation error message to display to users</returns>
    /// <remarks>
    /// This method demonstrates the Null Object Pattern combined with the Strategy Pattern.
    /// By providing a getter method rather than exposing the nullable property directly,
    /// we ensure that calling code never has to deal with null values while still allowing
    /// users to provide custom messages or rely on defaults.
    /// 
    /// The fallback strategy ensures system resilience - even if all custom messages
    /// are null or empty, the system will still provide meaningful error messages to users.
    /// This follows the principle of graceful degradation in software design.
    /// </remarks>
    public string GetValidationErrorMessage() => 
        !string.IsNullOrWhiteSpace(ValidationErrorMessage) ? ValidationErrorMessage : "Validation errors occurred";

    /// <summary>
    /// Gets the appropriate error message for not found exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The not found error message to display to users</returns>
    public string GetNotFoundErrorMessage() => 
        !string.IsNullOrWhiteSpace(NotFoundErrorMessage) ? NotFoundErrorMessage : "Resource not found";

    /// <summary>
    /// Gets the appropriate error message for forbidden access exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The forbidden access error message to display to users</returns>
    public string GetForbiddenAccessMessage() => 
        !string.IsNullOrWhiteSpace(ForbiddenAccessMessage) ? ForbiddenAccessMessage : "Access forbidden";

    /// <summary>
    /// Gets the appropriate error message for unauthorized access exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The unauthorized access error message to display to users</returns>
    public string GetUnauthorizedAccessMessage() => 
        !string.IsNullOrWhiteSpace(UnauthorizedAccessMessage) ? UnauthorizedAccessMessage : "Authentication required";

    /// <summary>
    /// Gets the appropriate error message for business rule violation exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The business rule violation error message to display to users</returns>
    public string GetBusinessRuleViolationMessage() => 
        !string.IsNullOrWhiteSpace(BusinessRuleViolationMessage) ? BusinessRuleViolationMessage : "Business rule violation";

    /// <summary>
    /// Gets the appropriate error message for application exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The application error message to display to users</returns>
    public string GetApplicationErrorMessage() => 
        !string.IsNullOrWhiteSpace(ApplicationErrorMessage) ? ApplicationErrorMessage : "Application error occurred";

    /// <summary>
    /// Gets the appropriate error message for unexpected exceptions, using custom message if provided
    /// or falling back to a sensible default.
    /// </summary>
    /// <returns>The unexpected error message to display to users</returns>
    public string GetUnexpectedErrorMessage() => 
        !string.IsNullOrWhiteSpace(UnexpectedErrorMessage) ? UnexpectedErrorMessage : "An unexpected error occurred";
}