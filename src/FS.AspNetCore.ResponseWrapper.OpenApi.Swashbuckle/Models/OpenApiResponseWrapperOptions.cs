namespace FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Models;

/// <summary>
/// Options for OpenAPI documentation integration with Response Wrapper
/// </summary>
public class OpenApiResponseWrapperOptions
{
    /// <summary>
    /// Automatically wrap all response schemas with ApiResponse wrapper
    /// Default: true
    /// </summary>
    public bool AutoWrapResponses { get; set; } = true;

    /// <summary>
    /// Include error response examples in OpenAPI documentation
    /// Default: true
    /// </summary>
    public bool IncludeErrorExamples { get; set; } = true;

    /// <summary>
    /// Include metadata schema in OpenAPI documentation
    /// Default: true
    /// </summary>
    public bool IncludeMetadataSchema { get; set; } = true;

    /// <summary>
    /// List of status codes to exclude from wrapping
    /// Default: empty (wrap all responses)
    /// </summary>
    public HashSet<int> ExcludedStatusCodes { get; set; } = new();
}
