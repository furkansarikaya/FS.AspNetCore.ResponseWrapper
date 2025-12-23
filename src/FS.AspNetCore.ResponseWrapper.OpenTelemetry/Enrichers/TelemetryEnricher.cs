using System.Diagnostics;
using FS.AspNetCore.ResponseWrapper.Extensibility;
using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Diagnostics;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Models;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Enrichers;

/// <summary>
/// Enricher that adds OpenTelemetry activity tags and baggage to wrapped responses
/// </summary>
public class TelemetryEnricher : IResponseEnricher
{
    private readonly OpenTelemetryOptions _options;
    private readonly ResponseWrapperMeter _meter;

    /// <summary>
    /// Execution order - runs after core enrichers (100+)
    /// </summary>
    public int Order => 100;

    public TelemetryEnricher(OpenTelemetryOptions options, ResponseWrapperMeter meter)
    {
        _options = options;
        _meter = meter;
    }

    public Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context)
    {
        var activity = Activity.Current;

        if (activity != null && _options.EnrichActivities)
        {
            EnrichActivity(activity, response, context);
        }

        if (_options.EnableMetrics)
        {
            RecordMetrics(response, context);
        }

        return Task.CompletedTask;
    }

    private void EnrichActivity<T>(Activity activity, ApiResponse<T> response, HttpContext context)
    {
        // Add response wrapper tags
        activity.SetTag("response_wrapper.success", response.Success);

        if (_options.IncludeStatusCode)
        {
            activity.SetTag("response_wrapper.status_code", context.Response.StatusCode);
        }

        if (_options.IncludeRequestPath)
        {
            activity.SetTag("response_wrapper.path", context.Request.Path.Value);
        }

        if (response.Message != null)
        {
            activity.SetTag("response_wrapper.message", response.Message);
        }

        // Add error details if response is unsuccessful
        if (!response.Success && _options.IncludeErrorDetails && response.Errors?.Any() == true)
        {
            activity.SetTag("response_wrapper.error_count", response.Errors.Count);

            var firstError = response.Errors.First();
            activity.SetTag("response_wrapper.error_message", firstError);

            // Set activity status to error
            activity.SetStatus(ActivityStatusCode.Error, firstError);
        }
        else if (response.Success)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
        }

        // Add response data type
        if (response.Data != null)
        {
            activity.SetTag("response_wrapper.data_type", typeof(T).Name);

            if (_options.IncludeResponseData)
            {
                // Warning: Be careful with sensitive data
                activity.SetTag("response_wrapper.data", System.Text.Json.JsonSerializer.Serialize(response.Data));
            }
        }

        // Add metadata information if available
        if (response.Metadata?.Additional != null && response.Metadata.Additional.Any())
        {
            activity.SetTag("response_wrapper.metadata_additional_count", response.Metadata.Additional.Count);

            // Add specific metadata as tags (limit to avoid too many tags)
            var metadataLimit = 5;
            foreach (var kvp in response.Metadata.Additional.Take(metadataLimit))
            {
                activity.SetTag($"response_wrapper.metadata.{kvp.Key}", kvp.Value?.ToString());
            }
        }
    }

    private void RecordMetrics<T>(ApiResponse<T> response, HttpContext context)
    {
        // Calculate duration from request start
        var duration = 0.0;
        if (context.Items.TryGetValue("RequestStartTime", out var startTimeObj) && startTimeObj is DateTime startTime)
        {
            duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        }

        _meter.RecordResponse(
            success: response.Success,
            durationMs: duration,
            statusCode: context.Response.StatusCode,
            path: context.Request.Path.Value
        );
    }
}
