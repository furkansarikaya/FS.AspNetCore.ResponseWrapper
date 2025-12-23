using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Defines a contract for enriching API responses after they have been wrapped.
/// Extension packages (OpenTelemetry, Caching, etc.) implement this interface to add
/// custom metadata, headers, or perform additional processing on wrapped responses.
/// </summary>
/// <remarks>
/// <para>
/// The IResponseEnricher interface provides a powerful extensibility point in the
/// ResponseWrapper pipeline, allowing extension packages to enhance responses after
/// the core wrapping logic has completed but before the response is sent to the client.
/// </para>
///
/// <para><strong>Execution Order:</strong></para>
/// <para>
/// Enrichers are executed in a specific order controlled by the <see cref="Order"/> property.
/// The recommended ordering scheme is:
/// </para>
/// <list type="bullet">
/// <item><description>0-49: Reserved for core ResponseWrapper enrichers</description></item>
/// <item><description>50-99: Caching enrichers (execute before telemetry to include cache metadata)</description></item>
/// <item><description>100-199: OpenTelemetry enrichers (execute after caching to capture complete metadata)</description></item>
/// <item><description>200+: Custom user-defined enrichers</description></item>
/// </list>
///
/// <para><strong>Common Use Cases:</strong></para>
/// <list type="bullet">
/// <item><description>Adding HTTP headers (ETag, Cache-Control, X-Custom-Headers)</description></item>
/// <item><description>Recording telemetry data (spans, metrics, traces)</description></item>
/// <item><description>Augmenting response metadata with custom information</description></item>
/// <item><description>Performing logging or auditing of responses</description></item>
/// <item><description>Triggering side effects based on response content</description></item>
/// </list>
///
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// Implementations must be thread-safe as enrichers are registered as singletons and
/// may be invoked concurrently for multiple requests.
/// </para>
///
/// <para><strong>Performance Considerations:</strong></para>
/// <para>
/// Enrichers are executed synchronously in the response pipeline. Heavy operations
/// should be offloaded to background tasks or implemented asynchronously to avoid
/// blocking the response.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CustomHeaderEnricher : IResponseEnricher
/// {
///     public int Order => 200; // Custom enrichers start at 200
///
///     public Task EnrichAsync&lt;T&gt;(ApiResponse&lt;T&gt; response, HttpContext context)
///     {
///         // Add custom header
///         context.Response.Headers["X-Api-Version"] = "2.0";
///
///         // Add custom metadata
///         response.Metadata.Additional ??= new Dictionary&lt;string, object&gt;();
///         response.Metadata.Additional["apiVersion"] = "2.0";
///
///         return Task.CompletedTask;
///     }
/// }
///
/// // Register in Startup/Program.cs
/// services.AddResponseWrapper(options =>
/// {
///     options.ResponseEnrichers.Add(new CustomHeaderEnricher());
/// });
/// </code>
/// </example>
public interface IResponseEnricher
{
    /// <summary>
    /// Gets the execution order for this enricher. Lower values execute first.
    /// </summary>
    /// <value>
    /// An integer representing the execution priority. The recommended ranges are:
    /// <list type="bullet">
    /// <item><description>0-49: Core enrichers (reserved)</description></item>
    /// <item><description>50-99: Caching enrichers</description></item>
    /// <item><description>100-199: OpenTelemetry enrichers</description></item>
    /// <item><description>200+: Custom enrichers</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// The order property allows fine-grained control over the enrichment pipeline.
    /// For example, caching enrichers (Order = 50) run before OpenTelemetry enrichers
    /// (Order = 100) so that cache-related metadata is available when telemetry is recorded.
    /// </remarks>
    int Order { get; }

    /// <summary>
    /// Enriches the wrapped API response with additional data, headers, or side effects.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the API response.</typeparam>
    /// <param name="response">
    /// The wrapped API response that can be enriched with additional metadata.
    /// Enrichers can modify the <see cref="ApiResponse{T}.Metadata"/> property to add
    /// custom metadata that will be included in the final JSON response.
    /// </param>
    /// <param name="context">
    /// The HTTP context for the current request. Use this to:
    /// <list type="bullet">
    /// <item><description>Add response headers via <c>context.Response.Headers</c></description></item>
    /// <item><description>Access request information via <c>context.Request</c></description></item>
    /// <item><description>Retrieve endpoint metadata via <c>context.GetEndpoint()</c></description></item>
    /// <item><description>Access request services via <c>context.RequestServices</c></description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A task representing the asynchronous enrichment operation. Most enrichers
    /// can return <see cref="Task.CompletedTask"/> for synchronous operations.
    /// </returns>
    /// <remarks>
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>
    /// Keep enrichment logic fast and lightweight. The enricher executes in the
    /// critical response path and will delay the response if it runs slowly.
    /// </description></item>
    /// <item><description>
    /// Use <c>response.Metadata.Additional</c> for custom metadata rather than
    /// modifying the response data directly.
    /// </description></item>
    /// <item><description>
    /// Handle exceptions gracefully. Unhandled exceptions will cause the entire
    /// request to fail. Consider logging errors and returning successfully.
    /// </description></item>
    /// <item><description>
    /// For conditional enrichment, check endpoint metadata or request properties
    /// to determine if enrichment is needed.
    /// </description></item>
    /// <item><description>
    /// Avoid modifying <c>response.Data</c> or <c>response.Success</c> as these
    /// should be controlled by the core ResponseWrapper logic.
    /// </description></item>
    /// </list>
    ///
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// If an enricher throws an exception, it will propagate up and cause the request
    /// to fail. Implementations should catch and log exceptions internally if they
    /// want to allow the request to proceed even if enrichment fails.
    /// </para>
    /// </remarks>
    Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context);
}
