using FS.AspNetCore.ResponseWrapper.Caching.Models;
using FS.AspNetCore.ResponseWrapper.Extensibility;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Caching.Providers;

/// <summary>
/// Metadata provider that adds cache hit/miss information to response metadata
/// </summary>
public class CacheMetadataProvider : IMetadataProvider
{
    private readonly CachingOptions _options;

    /// <summary>
    /// Provider name used as prefix for metadata keys
    /// </summary>
    public string Name => "cache";

    public CacheMetadataProvider(CachingOptions options)
    {
        _options = options;
    }

    public Task<Dictionary<string, object>?> GetMetadataAsync(HttpContext context)
    {
        if (!_options.AddCacheMetadata)
        {
            return Task.FromResult<Dictionary<string, object>?>(null);
        }

        var metadata = new Dictionary<string, object>();

        // Add cache hit/miss information
        if (context.Items.TryGetValue("CacheHit", out var cacheHitObj) && cacheHitObj is bool cacheHit)
        {
            metadata["hit"] = cacheHit;
        }

        // Add cache key if available
        if (context.Items.TryGetValue("CacheKey", out var cacheKeyObj) && cacheKeyObj is string cacheKey)
        {
            metadata["key"] = cacheKey;
        }

        // Add cache duration
        metadata["ttl_seconds"] = _options.DefaultCacheDurationSeconds;

        // Add cache type
        metadata["type"] = _options.UseDistributedCache ? "distributed" : "memory";

        // Add ETag if present
        if (context.Response.Headers.TryGetValue("ETag", out var etag))
        {
            metadata["etag"] = etag.ToString();
        }

        // Add cache status
        if (context.Response.StatusCode == StatusCodes.Status304NotModified)
        {
            metadata["status"] = "not_modified";
        }

        return Task.FromResult<Dictionary<string, object>?>(metadata.Any() ? metadata : null);
    }
}
