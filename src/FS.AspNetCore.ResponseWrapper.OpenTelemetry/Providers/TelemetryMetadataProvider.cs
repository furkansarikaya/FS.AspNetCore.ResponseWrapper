using System.Diagnostics;
using FS.AspNetCore.ResponseWrapper.Extensibility;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Models;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Providers;

/// <summary>
/// Metadata provider that adds trace context information to response metadata
/// </summary>
public class TelemetryMetadataProvider : IMetadataProvider
{
    private readonly OpenTelemetryOptions _options;

    /// <summary>
    /// Provider name used as prefix for metadata keys
    /// </summary>
    public string Name => "telemetry";

    public TelemetryMetadataProvider(OpenTelemetryOptions options)
    {
        _options = options;
    }

    public Task<Dictionary<string, object>?> GetMetadataAsync(HttpContext context)
    {
        if (!_options.AddTraceContextToMetadata)
        {
            return Task.FromResult<Dictionary<string, object>?>(null);
        }

        var activity = Activity.Current;
        if (activity == null)
        {
            return Task.FromResult<Dictionary<string, object>?>(null);
        }

        var metadata = new Dictionary<string, object>();

        // Add trace ID
        if (activity.TraceId != default)
        {
            metadata["trace_id"] = activity.TraceId.ToString();
        }

        // Add span ID
        if (activity.SpanId != default)
        {
            metadata["span_id"] = activity.SpanId.ToString();
        }

        // Add parent span ID if available
        if (activity.ParentSpanId != default)
        {
            metadata["parent_span_id"] = activity.ParentSpanId.ToString();
        }

        // Add trace flags
        metadata["trace_flags"] = activity.ActivityTraceFlags.ToString();

        // Add trace state if available
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            metadata["trace_state"] = activity.TraceStateString;
        }

        // Add W3C trace parent header format (useful for distributed tracing)
        metadata["traceparent"] = $"00-{activity.TraceId}-{activity.SpanId}-{(int)activity.ActivityTraceFlags:x2}";

        // Add baggage items if any
        var baggage = activity.Baggage.ToList();
        if (baggage.Any())
        {
            metadata["baggage"] = baggage.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value!);
        }

        return Task.FromResult<Dictionary<string, object>?>(metadata.Any() ? metadata : null);
    }
}
