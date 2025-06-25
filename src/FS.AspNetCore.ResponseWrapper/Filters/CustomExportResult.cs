using Microsoft.AspNetCore.Mvc;

namespace FS.AspNetCore.ResponseWrapper.Filters;

/// <summary>
/// Provides a specialized action result for handling file exports and downloads that should bypass
/// the standard ResponseWrapper processing. This result type ensures that binary content and file
/// downloads are delivered directly to the client without JSON wrapping or metadata injection.
/// </summary>
/// <remarks>
/// This action result is particularly useful for API endpoints that need to return file content
/// such as PDF reports, Excel exports, or binary data downloads. By implementing ISpecialResult,
/// it signals to the ResponseWrapper system that this content should not be processed through
/// the normal response transformation pipeline, ensuring file integrity and proper content delivery.
/// 
/// The result automatically sets appropriate HTTP headers for file downloads, including Content-Type
/// and Content-Disposition headers that enable proper browser handling of the downloaded content.
/// This eliminates the need for developers to manually configure response headers for file downloads
/// while ensuring compatibility with the ResponseWrapper system.
/// </remarks>
public class CustomExportResult(byte[] data, string fileName, string contentType) : ActionResult, ISpecialResult
{
    /// <summary>
    /// Executes the result by writing the binary file data directly to the HTTP response stream.
    /// This method configures the appropriate headers for file download and streams the content
    /// without any ResponseWrapper processing or JSON serialization.
    /// </summary>
    /// <param name="context">The action context containing the HTTP response and request information.</param>
    /// <returns>A task representing the asynchronous file writing operation.</returns>
    /// <remarks>
    /// The execution process follows these steps to ensure proper file delivery:
    /// 1. Sets the Content-Type header to the specified MIME type for proper browser interpretation
    /// 2. Configures the Content-Disposition header to trigger file download with the specified filename
    /// 3. Writes the binary data directly to the response body stream for optimal performance
    /// 
    /// This approach ensures maximum performance and memory efficiency, especially important for large
    /// file downloads. The method bypasses all JSON serialization and response wrapping overhead,
    /// delivering the file content with minimal processing delay.
    /// </remarks>
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = contentType;
        response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
        await response.Body.WriteAsync(data);
    }
}