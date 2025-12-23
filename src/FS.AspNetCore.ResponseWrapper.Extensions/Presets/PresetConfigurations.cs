using FS.AspNetCore.ResponseWrapper.Caching;
using FS.AspNetCore.ResponseWrapper.Extensions.Models;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;
using FS.AspNetCore.ResponseWrapper.Transformation;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AspNetCore.ResponseWrapper.Extensions.Presets;

/// <summary>
/// Preset configurations for common scenarios
/// </summary>
public static class PresetConfigurations
{
    /// <summary>
    /// Configures services based on preset type
    /// </summary>
    public static IServiceCollection ApplyPreset(
        this IServiceCollection services,
        PresetType preset,
        string? serviceName = null)
    {
        return preset switch
        {
            PresetType.Minimal => services.ApplyMinimalPreset(),
            PresetType.Basic => services.ApplyBasicPreset(),
            PresetType.Standard => services.ApplyStandardPreset(),
            PresetType.Advanced => services.ApplyAdvancedPreset(),
            PresetType.Enterprise => services.ApplyEnterprisePreset(serviceName),
            PresetType.GDPRCompliant => services.ApplyGDPRPreset(),
            PresetType.Performance => services.ApplyPerformancePreset(),
            PresetType.Development => services.ApplyDevelopmentPreset(serviceName),
            PresetType.Production => services.ApplyProductionPreset(serviceName),
            _ => services
        };
    }

    private static IServiceCollection ApplyMinimalPreset(this IServiceCollection services)
    {
        // Only core ResponseWrapper - no extensions
        return services;
    }

    private static IServiceCollection ApplyBasicPreset(this IServiceCollection services)
    {
        // Core + Memory Caching
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 60,
            enableETag: true
        );

        return services;
    }

    private static IServiceCollection ApplyStandardPreset(this IServiceCollection services)
    {
        // Core + Caching + Field Selection
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 300,
            enableETag: true
        );

        services.AddResponseWrapperFieldSelection();

        return services;
    }

    private static IServiceCollection ApplyAdvancedPreset(this IServiceCollection services)
    {
        // Core + Caching + Transformation + Basic Telemetry
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 600,
            enableETag: true
        );

        services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = true;
        });

        services.AddResponseWrapperOpenTelemetry(
            serviceName: "AdvancedService"
        );

        return services;
    }

    private static IServiceCollection ApplyEnterprisePreset(
        this IServiceCollection services,
        string? serviceName = null)
    {
        // Full stack - All features enabled
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 600,
            enableETag: true
        );

        services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = true;
            opts.AutoMaskEmails = true;
            opts.AutoMaskPhoneNumbers = true;
            opts.AutoMaskCreditCards = true;
        });

        services.AddResponseWrapperOpenTelemetry(
            serviceName: serviceName ?? "EnterpriseService"
        );

        return services;
    }

    private static IServiceCollection ApplyGDPRPreset(this IServiceCollection services)
    {
        // GDPR compliant - Privacy first
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 300,
            enableETag: false // Disable ETag to avoid caching PII hashes
        );

        services.AddResponseWrapperGDPRCompliance();

        // Minimal telemetry (no PII)
        services.AddResponseWrapperOpenTelemetry(
            serviceName: "GDPRService",
            configureTracing: builder =>
            {
                // Disable including response data in traces
            },
            configureMetrics: builder =>
            {
                // Only aggregate metrics, no user-specific data
            }
        );

        return services;
    }

    private static IServiceCollection ApplyPerformancePreset(this IServiceCollection services)
    {
        // Performance optimized - Aggressive caching
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 1800, // 30 minutes
            enableETag: true
        );

        services.AddResponseWrapperFieldSelection();

        // Telemetry for performance monitoring
        services.AddResponseWrapperOpenTelemetry(
            serviceName: "PerformanceService"
        );

        return services;
    }

    private static IServiceCollection ApplyDevelopmentPreset(
        this IServiceCollection services,
        string? serviceName = null)
    {
        // Development - All features, verbose logging
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 60,
            enableETag: true
        );

        services.AddResponseWrapperTransformation();
        services.AddResponseWrapperFieldSelection();

        services.AddResponseWrapperOpenTelemetryWithExporters(
            serviceName: serviceName ?? "DevService",
            useConsoleExporter: true,
            useOtlpExporter: false
        );

        return services;
    }

    private static IServiceCollection ApplyProductionPreset(
        this IServiceCollection services,
        string? serviceName = null)
    {
        // Production - Optimized for production workloads
        services.AddResponseWrapperMemoryCache(
            cacheDurationSeconds: 600,
            enableETag: true
        );

        services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = true;
        });

        services.AddResponseWrapperOpenTelemetryWithExporters(
            serviceName: serviceName ?? "ProductionService",
            useConsoleExporter: false,
            useOtlpExporter: true,
            otlpEndpoint: "http://localhost:4317"
        );

        return services;
    }
}
