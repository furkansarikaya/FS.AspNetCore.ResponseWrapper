using FS.AspNetCore.ResponseWrapper.Caching.Enrichers;
using FS.AspNetCore.ResponseWrapper.Caching.Middleware;
using FS.AspNetCore.ResponseWrapper.Caching.Models;
using FS.AspNetCore.ResponseWrapper.Caching.Providers;
using FS.AspNetCore.ResponseWrapper.Caching.Services;
using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AspNetCore.ResponseWrapper.Caching;

/// <summary>
/// Extension methods for configuring caching integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds caching integration to Response Wrapper
    /// </summary>
    /// <param name="options">Response Wrapper options</param>
    /// <param name="configureOptions">Optional configuration for caching integration</param>
    /// <returns>The same ResponseWrapperOptions instance for method chaining</returns>
    public static ResponseWrapperOptions AddCaching(
        this ResponseWrapperOptions options,
        Action<CachingOptions>? configureOptions = null)
    {
        var cachingOptions = new CachingOptions();
        configureOptions?.Invoke(cachingOptions);

        // Services will be registered separately in DI container
        // This just configures the enricher and metadata provider

        return options;
    }

    /// <summary>
    /// Adds caching services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration for caching</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperCaching(
        this IServiceCollection services,
        Action<CachingOptions>? configureOptions = null)
    {
        var cachingOptions = new CachingOptions();
        configureOptions?.Invoke(cachingOptions);

        // Register caching options as singleton
        services.AddSingleton(cachingOptions);

        // Register cache services
        services.AddSingleton<CacheKeyGenerator>();
        services.AddSingleton<ResponseCacheService>();

        // Add memory cache if not using distributed cache
        if (!cachingOptions.UseDistributedCache)
        {
            services.AddMemoryCache(memoryCacheOptions =>
            {
                if (cachingOptions.MaxCacheEntrySizeBytes > 0)
                {
                    memoryCacheOptions.SizeLimit = cachingOptions.MaxCacheEntrySizeBytes * 100; // Allow 100 entries
                }
            });
        }

        // Configure Response Wrapper to use caching
        services.Configure<ResponseWrapperOptions>(opts =>
        {
            opts.ResponseEnrichers.Add(new CacheEnricher(
                cachingOptions,
                services.BuildServiceProvider().GetRequiredService<ResponseCacheService>(),
                services.BuildServiceProvider().GetRequiredService<CacheKeyGenerator>()
            ));

            if (cachingOptions.AddCacheMetadata)
            {
                opts.MetadataProviders.Add(new CacheMetadataProvider(cachingOptions));
            }
        });

        return services;
    }

    /// <summary>
    /// Adds caching with in-memory cache
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="cacheDurationSeconds">Default cache duration in seconds</param>
    /// <param name="enableETag">Enable ETag generation</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperMemoryCache(
        this IServiceCollection services,
        int cacheDurationSeconds = 60,
        bool enableETag = true)
    {
        return services.AddResponseWrapperCaching(opts =>
        {
            opts.UseDistributedCache = false;
            opts.DefaultCacheDurationSeconds = cacheDurationSeconds;
            opts.EnableETag = enableETag;
        });
    }

    /// <summary>
    /// Adds caching with Redis distributed cache
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="redisConfiguration">Redis connection string</param>
    /// <param name="cacheDurationSeconds">Default cache duration in seconds</param>
    /// <param name="enableETag">Enable ETag generation</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperRedisCache(
        this IServiceCollection services,
        string redisConfiguration,
        int cacheDurationSeconds = 60,
        bool enableETag = true)
    {
        // Add Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfiguration;
            options.InstanceName = "ResponseWrapper:";
        });

        return services.AddResponseWrapperCaching(opts =>
        {
            opts.UseDistributedCache = true;
            opts.DefaultCacheDurationSeconds = cacheDurationSeconds;
            opts.EnableETag = enableETag;
        });
    }

    /// <summary>
    /// Adds caching with SQL Server distributed cache
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <param name="schemaName">Schema name for cache table</param>
    /// <param name="tableName">Cache table name</param>
    /// <param name="cacheDurationSeconds">Default cache duration in seconds</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperSqlServerCache(
        this IServiceCollection services,
        string connectionString,
        string schemaName = "dbo",
        string tableName = "ResponseCache",
        int cacheDurationSeconds = 60)
    {
        // Add SQL Server distributed cache
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName = schemaName;
            options.TableName = tableName;
        });

        return services.AddResponseWrapperCaching(opts =>
        {
            opts.UseDistributedCache = true;
            opts.DefaultCacheDurationSeconds = cacheDurationSeconds;
        });
    }

    /// <summary>
    /// Adds Response Wrapper cache middleware to the pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>The same IApplicationBuilder instance for method chaining</returns>
    public static IApplicationBuilder UseResponseWrapperCache(this IApplicationBuilder app)
    {
        app.UseMiddleware<CacheMiddleware>();
        return app;
    }

    /// <summary>
    /// Invalidates cache for a specific key
    /// </summary>
    public static async Task InvalidateCacheAsync(
        this HttpContext context,
        string? cacheKey = null)
    {
        var cacheService = context.RequestServices.GetService<ResponseCacheService>();
        if (cacheService == null)
            return;

        var key = cacheKey ?? context.Items["CacheKey"]?.ToString();
        if (key != null)
        {
            await cacheService.RemoveAsync(key);
        }
    }

    /// <summary>
    /// Invalidates cache by pattern
    /// </summary>
    public static async Task InvalidateCacheByPatternAsync(
        this HttpContext context,
        string pattern)
    {
        var cacheService = context.RequestServices.GetService<ResponseCacheService>();
        if (cacheService == null)
            return;

        await cacheService.RemoveByPatternAsync(pattern);
    }
}
