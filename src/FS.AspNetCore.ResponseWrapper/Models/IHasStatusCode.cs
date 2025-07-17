namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Defines a contract for objects that contain application-specific status codes.
/// This interface enables the ResponseWrapper system to automatically extract and promote
/// status codes from response data to the top-level ApiResponse structure, providing
/// consistent status communication across all API endpoints.
/// </summary>
/// <remarks>
/// This interface follows the Single Responsibility Principle by focusing solely on
/// status code provision, allowing any response object to participate in status code
/// extraction without requiring inheritance from specific base classes or implementing
/// complex interfaces.
/// 
/// The interface is designed to be lightweight and non-intrusive, enabling existing
/// DTOs and response objects to easily provide status information to the ResponseWrapper
/// system without significant refactoring or architectural changes.
/// 
/// **Automatic Extraction**: When the ResponseWrapper filter processes a response,
/// it automatically checks if the response data implements this interface. If it does,
/// the StatusCode is extracted and promoted to the ApiResponse level, ensuring that
/// status information is consistently available to API consumers.
/// 
/// **Domain Flexibility**: The interface doesn't prescribe specific status codes,
/// allowing each application domain to define their own status code vocabulary that
/// matches their business requirements and workflows.
/// </remarks>
public interface IHasStatusCode
{
    /// <summary>
    /// Gets the application-specific status code for this response object.
    /// This code provides semantic meaning about the operation outcome and any
    /// required next steps for the API consumer.
    /// </summary>
    /// <value>
    /// A string representing the status code, or null if no specific status applies.
    /// The format and values are determined by the implementing application's requirements.
    /// </value>
    string? StatusCode { get; }
}