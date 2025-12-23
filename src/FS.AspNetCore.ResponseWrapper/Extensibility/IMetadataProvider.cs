using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Defines a contract for providing custom metadata to be included in API response metadata.
/// Extension packages and custom implementations use this interface to inject domain-specific
/// metadata that augments the standard ResponseWrapper metadata (requestId, timestamp, etc.).
/// </summary>
/// <remarks>
/// <para>
/// The IMetadataProvider interface enables modular extensibility of response metadata without
/// modifying the core ResponseWrapper logic. Providers are invoked during response metadata
/// construction, and their output is merged into the final metadata dictionary.
/// </para>
///
/// <para><strong>Metadata Naming Convention:</strong></para>
/// <para>
/// To prevent metadata key conflicts, all provider metadata is prefixed with the provider's
/// <see cref="Name"/> property. For example, if a provider named "cache" returns a key "hit",
/// it will appear in the response metadata as "cache_hit".
/// </para>
///
/// <para><strong>Common Use Cases:</strong></para>
/// <list type="bullet">
/// <item><description>Cache information (hit/miss status, expiration time, cache key)</description></item>
/// <item><description>Distributed tracing context (trace ID, span ID, parent span)</description></item>
/// <item><description>Feature flags (enabled features for the request)</description></item>
/// <item><description>Rate limiting information (remaining quota, reset time)</description></item>
/// <item><description>Geographic information (data center, region)</description></item>
/// <item><description>Request classification (priority, category, customer tier)</description></item>
/// </list>
///
/// <para><strong>Execution Timing:</strong></para>
/// <para>
/// Metadata providers are invoked during the <c>BuildResponseMetadata</c> phase, which occurs
/// after action execution completes but before enrichers run. This means providers can access
/// the final response data but cannot add HTTP headers (use <see cref="IResponseEnricher"/> for headers).
/// </para>
///
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// Implementations must be thread-safe as providers are registered as singletons and may be
/// invoked concurrently across multiple requests.
/// </para>
///
/// <para><strong>Performance Considerations:</strong></para>
/// <para>
/// Metadata providers execute synchronously in the response pipeline. Heavy computations or
/// I/O operations should be avoided. Consider caching computed values or using fast lookups.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CacheMetadataProvider : IMetadataProvider
/// {
///     private readonly IDistributedCache _cache;
///
///     public string Name => "cache";
///
///     public CacheMetadataProvider(IDistributedCache cache)
///     {
///         _cache = cache;
///     }
///
///     public Task&lt;Dictionary&lt;string, object&gt;?&gt; GetMetadataAsync(HttpContext context)
///     {
///         // Check if response was served from cache
///         var cacheHit = context.Items.ContainsKey("CacheHit");
///
///         var metadata = new Dictionary&lt;string, object&gt;
///         {
///             { "hit", cacheHit },
///             { "key", context.Items["CacheKey"]?.ToString() ?? "none" }
///         };
///
///         // Will appear in response as: "cache_hit": true, "cache_key": "..."
///         return Task.FromResult&lt;Dictionary&lt;string, object&gt;?&gt;(metadata);
///     }
/// }
///
/// // Register in Startup/Program.cs
/// services.AddSingleton&lt;IMetadataProvider, CacheMetadataProvider&gt;();
/// services.AddResponseWrapper(options =>
/// {
///     options.MetadataProviders.Add(
///         serviceProvider.GetRequiredService&lt;CacheMetadataProvider&gt;());
/// });
/// </code>
/// </example>
public interface IMetadataProvider
{
    /// <summary>
    /// Gets the name of this metadata provider. Used as a prefix for all metadata keys
    /// to prevent conflicts between different providers.
    /// </summary>
    /// <value>
    /// A short, descriptive name for the provider (e.g., "cache", "telemetry", "rateLimit").
    /// Should be alphanumeric and use camelCase. This name will be used as a prefix for
    /// all metadata keys (e.g., "cache_hit", "cache_expiration").
    /// </value>
    /// <remarks>
    /// <para><strong>Naming Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>Use camelCase (e.g., "rateLimit", not "RateLimit" or "rate-limit")</description></item>
    /// <item><description>Keep it short and descriptive (5-15 characters)</description></item>
    /// <item><description>Avoid generic names like "data" or "info"</description></item>
    /// <item><description>Use the provider's primary purpose (e.g., "cache", "geo", "feature")</description></item>
    /// </list>
    ///
    /// <para><strong>Key Prefixing:</strong></para>
    /// <para>
    /// All metadata keys returned by <see cref="GetMetadataAsync"/> will be prefixed
    /// with this name followed by an underscore. For example, if Name is "cache" and
    /// the metadata contains {"hit": true, "ttl": 300}, the final response metadata
    /// will include "cache_hit": true and "cache_ttl": 300.
    /// </para>
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Retrieves custom metadata to be included in the API response metadata.
    /// </summary>
    /// <param name="context">
    /// The HTTP context for the current request. Use this to:
    /// <list type="bullet">
    /// <item><description>Access request properties via <c>context.Request</c></description></item>
    /// <item><description>Retrieve per-request data via <c>context.Items</c></description></item>
    /// <item><description>Get endpoint metadata via <c>context.GetEndpoint()</c></description></item>
    /// <item><description>Access registered services via <c>context.RequestServices</c></description></item>
    /// <item><description>Read response information via <c>context.Response</c></description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A dictionary of metadata key-value pairs to be included in the response, or null
    /// if no metadata should be added for this request. Keys should be descriptive and
    /// use camelCase. All keys will be prefixed with the provider's <see cref="Name"/>.
    /// </returns>
    /// <remarks>
    /// <para><strong>Metadata Value Types:</strong></para>
    /// <para>
    /// Values can be any type that can be serialized to JSON, including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Primitives (string, int, bool, etc.)</description></item>
    /// <item><description>DateTime (will be serialized as ISO 8601)</description></item>
    /// <item><description>Collections (arrays, lists, dictionaries)</description></item>
    /// <item><description>Anonymous objects or POCOs</description></item>
    /// </list>
    ///
    /// <para><strong>Conditional Metadata:</strong></para>
    /// <para>
    /// Providers can return null or an empty dictionary when metadata is not applicable
    /// for a particular request. For example, a cache provider might return null if the
    /// endpoint is not cacheable.
    /// </para>
    ///
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="bullet">
    /// <item><description>
    /// Keep metadata concise. Excessive metadata increases response size and serialization time.
    /// </description></item>
    /// <item><description>
    /// Return null instead of an empty dictionary when no metadata is applicable to
    /// avoid unnecessary processing.
    /// </description></item>
    /// <item><description>
    /// Use primitive types when possible. Complex objects require more serialization overhead.
    /// </description></item>
    /// <item><description>
    /// Avoid including sensitive information (passwords, tokens, PII) in metadata.
    /// </description></item>
    /// <item><description>
    /// Cache expensive computations in HttpContext.Items if used by multiple providers.
    /// </description></item>
    /// <item><description>
    /// Handle exceptions gracefully. Unhandled exceptions will cause the entire request to fail.
    /// </description></item>
    /// </list>
    ///
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// If this method throws an exception, it will propagate up and cause the request
    /// to fail. Implementations should catch and log exceptions internally if they want
    /// to allow the request to proceed even if metadata collection fails.
    /// </para>
    /// </remarks>
    Task<Dictionary<string, object>?> GetMetadataAsync(HttpContext context);
}
