namespace FS.AspNetCore.ResponseWrapper.OpenApi.Scalar.Models;

/// <summary>
/// Options for OpenAPI documentation integration with Response Wrapper
/// </summary>
public class OpenApiResponseWrapperOptions
{
    /// <summary>
    /// Show ApiResponse wrapper information in Scalar UI
    /// Default: true
    /// </summary>
    public bool ShowWrapperInfo { get; set; } = true;

    /// <summary>
    /// Include error examples in Scalar documentation
    /// Default: true
    /// </summary>
    public bool IncludeErrorExamples { get; set; } = true;

    /// <summary>
    /// Custom CSS for Scalar UI to highlight wrapper structure
    /// Default: null (use Scalar defaults)
    /// </summary>
    public string? CustomCss { get; set; }
}
