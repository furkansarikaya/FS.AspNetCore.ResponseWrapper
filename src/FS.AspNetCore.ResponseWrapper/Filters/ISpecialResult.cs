namespace FS.AspNetCore.ResponseWrapper.Filters;

/// <summary>
/// Marker interface used to identify action results that require special handling and should
/// bypass the standard ResponseWrapper processing pipeline. Action results implementing this
/// interface are excluded from JSON wrapping, metadata injection, and other response transformations.
/// </summary>
/// <remarks>
/// This interface follows the Marker Interface pattern, which is a design pattern that provides
/// a way to categorize types without requiring any implementation. The ResponseWrapper filter
/// system checks for this interface to determine whether a result should undergo normal processing
/// or be delivered directly to the client without modification.
/// 
/// The interface serves as a contract between action results and the ResponseWrapper system,
/// enabling developers to create custom result types that maintain full control over their
/// response format and delivery mechanism. This is essential for scenarios where the standard
/// JSON response wrapper would interfere with the intended functionality.
/// 
/// Common use cases for implementing this interface include:
/// - File download results that need to stream binary content
/// - Custom media responses (images, videos, audio)
/// - Specialized formatting responses (XML, CSV, custom formats)
/// - Redirect responses that must maintain their original behavior
/// - Integration endpoints that require specific response structures
/// </remarks>
public interface ISpecialResult
{
    // Marker interface - no implementation required
    // // The presence of this interface signals to ResponseWrapper to skip processing
}