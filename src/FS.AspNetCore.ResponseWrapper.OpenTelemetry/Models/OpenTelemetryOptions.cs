namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Models;

/// <summary>
/// Options for OpenTelemetry integration with Response Wrapper
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Enable automatic activity enrichment with response metadata
    /// Default: true
    /// </summary>
    public bool EnrichActivities { get; set; } = true;

    /// <summary>
    /// Enable automatic metrics collection (response times, error rates)
    /// Default: true
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Include response data in trace spans (be careful with sensitive data)
    /// Default: false
    /// </summary>
    public bool IncludeResponseData { get; set; } = false;

    /// <summary>
    /// Include request path in activity tags
    /// Default: true
    /// </summary>
    public bool IncludeRequestPath { get; set; } = true;

    /// <summary>
    /// Include HTTP status code in activity tags
    /// Default: true
    /// </summary>
    public bool IncludeStatusCode { get; set; } = true;

    /// <summary>
    /// Include error details in activity tags when response is unsuccessful
    /// Default: true
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = true;

    /// <summary>
    /// Add trace context to response metadata
    /// Default: true
    /// </summary>
    public bool AddTraceContextToMetadata { get; set; } = true;

    /// <summary>
    /// Activity source name for Response Wrapper operations
    /// Default: "FS.AspNetCore.ResponseWrapper"
    /// </summary>
    public string ActivitySourceName { get; set; } = "FS.AspNetCore.ResponseWrapper";

    /// <summary>
    /// Meter name for Response Wrapper metrics
    /// Default: "FS.AspNetCore.ResponseWrapper"
    /// </summary>
    public string MeterName { get; set; } = "FS.AspNetCore.ResponseWrapper";
}
