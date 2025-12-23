namespace FS.AspNetCore.ResponseWrapper.Caching.Models;

/// <summary>
/// Options for response caching integration with Response Wrapper
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Enable automatic response caching
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Default cache duration in seconds
    /// Default: 60 seconds
    /// </summary>
    public int DefaultCacheDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Enable ETag generation for responses
    /// Default: true
    /// </summary>
    public bool EnableETag { get; set; } = true;

    /// <summary>
    /// Add cache hit/miss information to response metadata
    /// Default: true
    /// </summary>
    public bool AddCacheMetadata { get; set; } = true;

    /// <summary>
    /// Cache key prefix to avoid collisions
    /// Default: "rw:"
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "rw:";

    /// <summary>
    /// Enable cache compression to reduce memory usage
    /// Default: true
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Cache only successful responses (Success = true)
    /// Default: true
    /// </summary>
    public bool CacheOnlySuccessful { get; set; } = true;

    /// <summary>
    /// Enable distributed cache (requires configuration)
    /// Default: false (uses in-memory cache)
    /// </summary>
    public bool UseDistributedCache { get; set; } = false;

    /// <summary>
    /// Cache sliding expiration (renews TTL on access)
    /// Default: false
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = false;

    /// <summary>
    /// Maximum cache entry size in bytes (0 = unlimited)
    /// Default: 1MB
    /// </summary>
    public long MaxCacheEntrySizeBytes { get; set; } = 1024 * 1024; // 1MB
}
