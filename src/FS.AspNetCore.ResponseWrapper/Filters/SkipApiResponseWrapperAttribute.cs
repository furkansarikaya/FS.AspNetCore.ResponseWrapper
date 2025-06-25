namespace FS.AspNetCore.ResponseWrapper.Filters;

/// <summary>
/// Attribute that can be applied to controller actions or entire controllers to exclude them
/// from ResponseWrapper processing. This provides fine-grained control over which endpoints
/// participate in automatic response wrapping and metadata injection.
/// </summary>
/// <remarks>
/// This attribute enables developers to selectively opt out of ResponseWrapper functionality
/// at both the controller and action levels, providing flexibility for mixed scenarios where
/// some endpoints need standard wrapping while others require custom response handling.
/// 
/// The attribute works by being detected during the response processing pipeline, allowing
/// the ResponseWrapper filter to skip all transformation logic for marked endpoints. This
/// approach ensures that excluded endpoints behave exactly as they would without any
/// ResponseWrapper integration, maintaining complete backward compatibility.
/// 
/// Key benefits of this approach include:
/// - Granular control over response processing at method or class level
/// - Zero performance impact on excluded endpoints
/// - Clear documentation of intentional exclusions through the Reason property
/// - Smooth migration path for incrementally adopting ResponseWrapper
/// - Support for mixed API designs with different response requirements
/// 
/// The attribute can be particularly valuable during migration scenarios where legacy
/// endpoints need to maintain their existing response formats while new endpoints
/// adopt the standardized ResponseWrapper structure.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipApiResponseWrapperAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional reason explaining why ResponseWrapper processing should be skipped
    /// for this endpoint. This property is useful for documentation and debugging purposes.
    /// </summary>
    /// <value>
    /// A string describing the reason for skipping ResponseWrapper processing. This can be helpful
    /// for team communication and code maintenance, providing context about why specific endpoints
    /// require special handling. The reason is not used functionally but serves as inline documentation.
    /// </value>
    /// <remarks>
    /// While this property is optional, providing a clear reason helps maintain code clarity
    /// and assists future developers in understanding the architectural decisions behind
    /// endpoint exclusions. Good reasons might include:
    /// - "Legacy endpoint maintains existing API contract"
    /// - "File download endpoint requires binary response"
    /// - "Integration with external system requires specific format"
    /// - "Performance-critical endpoint minimizes processing overhead"
    /// 
    /// The reason is also valuable during code reviews and architectural discussions,
    /// helping teams evaluate whether exclusions are still necessary as the system evolves.
    /// </remarks>
    public string Reason { get; set; } = "";
    
    /// <summary>
    /// Initializes a new instance of the SkipApiResponseWrapperAttribute class with an optional reason.
    /// </summary>
    /// <param name="reason">
    /// Optional explanation for why ResponseWrapper processing should be skipped. Defaults to an empty string
    /// if not provided. While not functionally required, providing a reason improves code documentation
    /// and helps team members understand the exclusion rationale.
    /// </param>
    /// <remarks>
    /// The constructor follows the optional parameter pattern, allowing the attribute to be used
    /// both with and without explanatory text. This design supports quick exclusions during
    /// development while encouraging documentation for production code.
    /// 
    /// Usage examples:
    /// - [SkipApiResponseWrapper] // Quick exclusion without reason
    /// - [SkipApiResponseWrapper("Legacy API compatibility")] // Documented exclusion
    /// 
    /// The flexibility in constructor usage helps accommodate different development workflows
    /// while maintaining the option for comprehensive documentation when needed.
    /// </remarks>
    public SkipApiResponseWrapperAttribute(string reason = "")
    {
        Reason = reason;
    }
}