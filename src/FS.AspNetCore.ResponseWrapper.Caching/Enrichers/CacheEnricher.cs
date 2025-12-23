using FS.AspNetCore.ResponseWrapper.Caching.Models;
using FS.AspNetCore.ResponseWrapper.Caching.Services;
using FS.AspNetCore.ResponseWrapper.Extensibility;
using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Caching.Enrichers;

/// <summary>
/// Enricher that handles response caching and ETag generation
/// </summary>
public class CacheEnricher : IResponseEnricher
{
    private readonly CachingOptions _options;
    private readonly ResponseCacheService _cacheService;
    private readonly CacheKeyGenerator _keyGenerator;

    /// <summary>
    /// Execution order - runs in caching phase (50-99)
    /// </summary>
    public int Order => 50;

    public CacheEnricher(
        CachingOptions options,
        ResponseCacheService cacheService,
        CacheKeyGenerator keyGenerator)
    {
        _options = options;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
    }

    public async Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context)
    {
        // Only process GET requests for caching
        if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return;

        // Skip if caching is disabled
        if (!_options.EnableCaching)
            return;

        // Skip caching unsuccessful responses if configured
        if (_options.CacheOnlySuccessful && !response.Success)
            return;

        // Generate cache key
        var cacheKey = _keyGenerator.GenerateKey(context, _options.CacheKeyPrefix);

        // Store cache key in items for later use (e.g., invalidation)
        context.Items["CacheKey"] = cacheKey;

        // Generate and set ETag if enabled
        if (_options.EnableETag && response.Data != null)
        {
            var etag = _keyGenerator.GenerateETag(response.Data);

            // Check If-None-Match header
            var ifNoneMatch = context.Request.Headers["If-None-Match"].FirstOrDefault();
            if (ifNoneMatch == etag)
            {
                // Client has the latest version, return 304 Not Modified
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                context.Response.Headers["ETag"] = etag;
                return;
            }

            // Set ETag header
            context.Response.Headers["ETag"] = etag;
        }

        // Cache the response
        if (response.Success && response.Data != null)
        {
            // Get custom cache duration from response metadata if available
            var cacheDuration = GetCacheDuration(context);

            await _cacheService.SetAsync(
                cacheKey,
                response.Data,
                cacheDuration
            );
        }
    }

    private TimeSpan GetCacheDuration(HttpContext context)
    {
        // Check if a custom duration is specified in route data or headers
        if (context.Items.TryGetValue("CacheDuration", out var durationObj) &&
            durationObj is int durationSeconds)
        {
            return TimeSpan.FromSeconds(durationSeconds);
        }

        return TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds);
    }
}
