namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Defines a contract for objects that contain custom metadata to be included in API responses.
/// This interface enables the ResponseWrapper system to automatically extract and merge
/// custom metadata from response data into the top-level metadata structure, providing
/// flexible extension capabilities for application-specific metadata requirements.
/// </summary>
/// <remarks>
/// This interface follows the same lightweight, non-intrusive design as IHasStatusCode,
/// allowing any response object to provide custom metadata without requiring inheritance
/// from specific base classes or implementing complex interfaces.
/// 
/// **Automatic Extraction**: When the ResponseWrapper filter processes a response,
/// it automatically checks if the response data implements this interface. If it does,
/// the custom metadata is extracted and merged with the system-generated metadata,
/// ensuring that application-specific metadata appears alongside standard metadata.
/// 
/// **Flexible Structure**: The interface doesn't prescribe specific metadata keys or values,
/// allowing each application to define their own metadata vocabulary that matches their
/// business requirements and client needs.
/// 
/// **Use Cases**: Common scenarios include workflow state information, business process
/// indicators, feature flags, permission contexts, or any application-specific data
/// that clients need for decision-making or user experience enhancement.
/// </remarks>
/// <example>
/// <code>
/// public class RegisterNextStepDto : IHasStatusCode, IHasMetadata
/// {
///     public string StatusCode { get; set; } = "VERIFY_2FA";
///     public string Message { get; set; } = "Please enter the 2FA code sent to your device.";
///     
///     public Dictionary&lt;string, object&gt; Metadata { get; set; } = new()
///     {
///         { "verificationType", "2fa" },
///         { "canResend", true },
///         { "resendCooldown", 30 }
///     };
/// }
/// </code>
/// </example>
public interface IHasMetadata
{
    /// <summary>
    /// Gets the custom metadata dictionary containing application-specific information
    /// that should be included in the response metadata structure.
    /// </summary>
    /// <value>
    /// A dictionary containing custom metadata key-value pairs, or null if no custom
    /// metadata is available. The keys should be descriptive strings, and values
    /// can be any serializable object type.
    /// </value>
    /// <remarks>
    /// The metadata dictionary provides a flexible way to include custom information
    /// in API responses that goes beyond the standard business data and system metadata.
    /// 
    /// **Key Naming Conventions**: Use descriptive, camelCase key names that clearly
    /// indicate the purpose of the metadata. Avoid keys that conflict with standard
    /// metadata properties like 'requestId', 'timestamp', or 'executionTimeMs'.
    /// 
    /// **Value Types**: Values can be any type that can be serialized to JSON, including
    /// primitives, objects, arrays, and nested structures. Consider the client's ability
    /// to process complex structures when designing metadata values.
    /// 
    /// **Performance Considerations**: Keep metadata reasonably sized since it's included
    /// in every response. Large or complex metadata structures can impact response size
    /// and serialization performance.
    /// 
    /// **Null Handling**: Returning null or an empty dictionary is acceptable and will
    /// result in no custom metadata being added to the response.
    /// </remarks>
    Dictionary<string, object>? Metadata { get; }
}