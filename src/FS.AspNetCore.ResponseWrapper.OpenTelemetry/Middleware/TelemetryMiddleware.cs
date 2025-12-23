using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Middleware;

/// <summary>
/// Middleware to track request start time for metrics calculation
/// </summary>
public class TelemetryMiddleware
{
    private readonly RequestDelegate _next;

    public TelemetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store request start time for duration calculation
        context.Items["RequestStartTime"] = DateTime.UtcNow;

        await _next(context);
    }
}
