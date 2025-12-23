using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Diagnostics;

/// <summary>
/// Meter for Response Wrapper metrics
/// </summary>
public class ResponseWrapperMeter
{
    private readonly Meter _meter;

    /// <summary>
    /// Counter for total number of responses
    /// </summary>
    public Counter<long> ResponseCount { get; }

    /// <summary>
    /// Counter for total number of errors
    /// </summary>
    public Counter<long> ErrorCount { get; }

    /// <summary>
    /// Histogram for response processing duration
    /// </summary>
    public Histogram<double> ResponseDuration { get; }

    /// <summary>
    /// Counter for successful responses
    /// </summary>
    public Counter<long> SuccessCount { get; }

    public ResponseWrapperMeter(string meterName = "FS.AspNetCore.ResponseWrapper")
    {
        _meter = new Meter(meterName, "10.0.0");

        ResponseCount = _meter.CreateCounter<long>(
            "response_wrapper.responses.total",
            description: "Total number of wrapped responses");

        ErrorCount = _meter.CreateCounter<long>(
            "response_wrapper.errors.total",
            description: "Total number of error responses");

        ResponseDuration = _meter.CreateHistogram<double>(
            "response_wrapper.response.duration",
            unit: "ms",
            description: "Response processing duration in milliseconds");

        SuccessCount = _meter.CreateCounter<long>(
            "response_wrapper.success.total",
            description: "Total number of successful responses");
    }

    /// <summary>
    /// Records a response metric
    /// </summary>
    public void RecordResponse(bool success, double durationMs, int statusCode, string? path = null)
    {
        var tags = new TagList
        {
            { "success", success },
            { "status_code", statusCode }
        };

        if (!string.IsNullOrEmpty(path))
        {
            tags.Add("path", path);
        }

        ResponseCount.Add(1, tags);

        if (success)
        {
            SuccessCount.Add(1, tags);
        }
        else
        {
            ErrorCount.Add(1, tags);
        }

        ResponseDuration.Record(durationMs, tags);
    }
}
