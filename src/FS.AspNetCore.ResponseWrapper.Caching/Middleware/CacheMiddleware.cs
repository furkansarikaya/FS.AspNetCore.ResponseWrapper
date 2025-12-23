using FS.AspNetCore.ResponseWrapper.Caching.Models;
using FS.AspNetCore.ResponseWrapper.Caching.Services;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Caching.Middleware;

/// <summary>
/// Middleware to check cache before processing request
/// </summary>
public class CacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CachingOptions _options;
    private readonly ResponseCacheService _cacheService;
    private readonly CacheKeyGenerator _keyGenerator;

    public CacheMiddleware(
        RequestDelegate next,
        CachingOptions options,
        ResponseCacheService cacheService,
        CacheKeyGenerator keyGenerator)
    {
        _next = next;
        _options = options;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
            !_options.EnableCaching)
        {
            await _next(context);
            return;
        }

        // Generate cache key
        var cacheKey = _keyGenerator.GenerateKey(context, _options.CacheKeyPrefix);

        // Try to get from cache
        var cachedResponse = await _cacheService.GetAsync<object>(cacheKey);

        if (cachedResponse != null)
        {
            // Cache hit - mark in context items
            context.Items["CacheHit"] = true;
            context.Items["CacheKey"] = cacheKey;

            // Check ETag if enabled
            if (_options.EnableETag)
            {
                var etag = _keyGenerator.GenerateETag(cachedResponse);
                var ifNoneMatch = context.Request.Headers["If-None-Match"].FirstOrDefault();

                if (ifNoneMatch == etag)
                {
                    // Client has latest version
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.Headers["ETag"] = etag;
                    return;
                }

                context.Response.Headers["ETag"] = etag;
            }

            // Serve from cache
            context.Response.Headers["X-Cache"] = "HIT";
            // The actual response will be wrapped by the filter
            // Store cached data in items for the filter to use
            context.Items["CachedData"] = cachedResponse;
        }
        else
        {
            // Cache miss
            context.Items["CacheHit"] = false;
            context.Items["CacheKey"] = cacheKey;
            context.Response.Headers["X-Cache"] = "MISS";
        }

        await _next(context);
    }
}
