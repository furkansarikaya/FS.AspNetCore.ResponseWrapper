using FS.AspNetCore.ResponseWrapper;
using FS.AspNetCore.ResponseWrapper.Caching;
using FS.AspNetCore.ResponseWrapper.Caching.Middleware;
using FS.AspNetCore.ResponseWrapper.Extensions.Models;
using FS.AspNetCore.ResponseWrapper.Extensions.Presets;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Middleware;
using FS.AspNetCore.ResponseWrapper.Transformation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FS.AspNetCore.ResponseWrapper.Extensions;

/// <summary>
/// Unified extension methods for configuring all ResponseWrapper extensions
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds ResponseWrapper with all enterprise extensions using a preset configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="preset">Preset configuration type</param>
    /// <param name="serviceName">Service name for telemetry (optional)</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperWithPreset(
        this IServiceCollection services,
        PresetType preset = PresetType.Standard,
        string? serviceName = null)
    {
        // Add core ResponseWrapper
        services.AddResponseWrapper();

        // Apply preset configuration
        services.ApplyPreset(preset, serviceName);

        return services;
    }

    /// <summary>
    /// Adds ResponseWrapper with full enterprise stack
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name for telemetry</param>
    /// <param name="redisConfiguration">Redis connection string (optional, uses memory cache if null)</param>
    /// <param name="enableSwashbuckle">Enable Swashbuckle OpenAPI integration</param>
    /// <param name="enableNSwag">Enable NSwag OpenAPI integration</param>
    /// <param name="enableScalar">Enable Scalar OpenAPI integration</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperEnterprise(
        this IServiceCollection services,
        string serviceName = "EnterpriseService",
        string? redisConfiguration = null,
        bool enableSwashbuckle = true,
        bool enableNSwag = false,
        bool enableScalar = false)
    {
        // Add core ResponseWrapper
        services.AddResponseWrapper();

        // Add caching
        if (redisConfiguration != null)
        {
            services.AddResponseWrapperRedisCache(
                redisConfiguration,
                cacheDurationSeconds: 600,
                enableETag: true
            );
        }
        else
        {
            services.AddResponseWrapperMemoryCache(
                cacheDurationSeconds: 600,
                enableETag: true
            );
        }

        // Add transformation
        services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = true;
            opts.AutoMaskEmails = true;
            opts.AutoMaskPhoneNumbers = true;
            opts.AutoMaskCreditCards = true;
        });

        // Add telemetry
        services.AddResponseWrapperOpenTelemetryWithExporters(
            serviceName: serviceName,
            useConsoleExporter: false,
            useOtlpExporter: true
        );

        // Note: OpenAPI integration is done via SwaggerGen configuration
        // Users need to call AddSwaggerGen with AddResponseWrapper() separately

        return services;
    }

    /// <summary>
    /// Adds ResponseWrapper with GDPR compliant configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name for telemetry</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperGDPR(
        this IServiceCollection services,
        string serviceName = "GDPRService")
    {
        services.AddResponseWrapper();
        services.ApplyPreset(PresetType.GDPRCompliant, serviceName);
        return services;
    }

    /// <summary>
    /// Adds ResponseWrapper with performance-optimized configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="redisConfiguration">Redis connection string for distributed caching</param>
    /// <param name="cacheDurationSeconds">Cache duration in seconds</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperPerformance(
        this IServiceCollection services,
        string? redisConfiguration = null,
        int cacheDurationSeconds = 1800)
    {
        services.AddResponseWrapper();

        if (redisConfiguration != null)
        {
            services.AddResponseWrapperRedisCache(
                redisConfiguration,
                cacheDurationSeconds,
                enableETag: true
            );
        }
        else
        {
            services.AddResponseWrapperMemoryCache(
                cacheDurationSeconds,
                enableETag: true
            );
        }

        services.AddResponseWrapperFieldSelection();
        services.AddResponseWrapperOpenTelemetry("PerformanceService");

        return services;
    }

    /// <summary>
    /// Configures ResponseWrapper middleware pipeline with all extensions
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="useCache">Enable caching middleware</param>
    /// <param name="useTelemetry">Enable telemetry middleware</param>
    /// <returns>The same IApplicationBuilder instance for method chaining</returns>
    /// <remarks>
    /// Note: The core ResponseWrapper uses a filter-based approach, not middleware.
    /// The filter is automatically registered globally when AddResponseWrapper() is called.
    /// This method only configures extension middleware (caching, telemetry).
    /// </remarks>
    public static IApplicationBuilder UseResponseWrapperExtensions(
        this IApplicationBuilder app,
        bool useCache = true,
        bool useTelemetry = true)
    {
        if (useTelemetry)
        {
            app.UseResponseWrapperTelemetry();
        }

        if (useCache)
        {
            app.UseResponseWrapperCache();
        }

        // Note: Core ResponseWrapper is filter-based, no middleware needed
        // The ApiResponseWrapperFilter is registered globally via AddResponseWrapper()

        return app;
    }

    /// <summary>
    /// Quick setup for development environment
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperForDevelopment(
        this IServiceCollection services,
        string serviceName = "DevService")
    {
        services.AddResponseWrapper();
        services.ApplyPreset(PresetType.Development, serviceName);
        return services;
    }

    /// <summary>
    /// Quick setup for production environment
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name</param>
    /// <param name="redisConfiguration">Redis connection string</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperForProduction(
        this IServiceCollection services,
        string serviceName = "ProductionService",
        string? redisConfiguration = null)
    {
        services.AddResponseWrapper();

        // Use Redis if provided, otherwise memory cache
        if (redisConfiguration != null)
        {
            services.AddResponseWrapperRedisCache(redisConfiguration);
        }
        else
        {
            services.AddResponseWrapperMemoryCache();
        }

        services.AddResponseWrapperTransformation();
        services.AddResponseWrapperOpenTelemetryWithExporters(
            serviceName: serviceName,
            useConsoleExporter: false,
            useOtlpExporter: true
        );

        return services;
    }

    /// <summary>
    /// Configures Swashbuckle with ResponseWrapper support
    /// </summary>
    public static SwaggerGenOptions AddResponseWrapperSwashbuckle(
        this SwaggerGenOptions options,
        bool includeErrorExamples = true,
        bool includeMetadata = true)
    {
        return OpenApi.Swashbuckle.DependencyInjection.AddResponseWrapper(
            options,
            includeErrorExamples,
            includeMetadata
        );
    }
}
