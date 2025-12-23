using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Diagnostics;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Enrichers;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Middleware;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Models;
using FS.AspNetCore.ResponseWrapper.OpenTelemetry.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds OpenTelemetry integration to Response Wrapper
    /// </summary>
    /// <param name="options">Response Wrapper options</param>
    /// <param name="configureOptions">Optional configuration for OpenTelemetry integration</param>
    /// <returns>The same ResponseWrapperOptions instance for method chaining</returns>
    public static ResponseWrapperOptions AddOpenTelemetry(
        this ResponseWrapperOptions options,
        Action<OpenTelemetryOptions>? configureOptions = null)
    {
        var telemetryOptions = new OpenTelemetryOptions();
        configureOptions?.Invoke(telemetryOptions);

        // Create meter singleton
        var meter = new ResponseWrapperMeter(telemetryOptions.MeterName);

        // Add telemetry enricher
        options.ResponseEnrichers.Add(new TelemetryEnricher(telemetryOptions, meter));

        // Add telemetry metadata provider
        if (telemetryOptions.AddTraceContextToMetadata)
        {
            options.MetadataProviders.Add(new TelemetryMetadataProvider(telemetryOptions));
        }

        return options;
    }

    /// <summary>
    /// Adds OpenTelemetry integration to Response Wrapper with default settings
    /// </summary>
    /// <param name="options">Response Wrapper options</param>
    /// <returns>The same ResponseWrapperOptions instance for method chaining</returns>
    public static ResponseWrapperOptions AddOpenTelemetry(this ResponseWrapperOptions options)
    {
        return options.AddOpenTelemetry(null);
    }

    /// <summary>
    /// Configures OpenTelemetry to track Response Wrapper activities and metrics
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name for telemetry</param>
    /// <param name="configureTracing">Optional configuration for tracing</param>
    /// <param name="configureMetrics">Optional configuration for metrics</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperOpenTelemetry(
        this IServiceCollection services,
        string serviceName = "ResponseWrapperService",
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName))
            .WithTracing(tracerBuilder =>
            {
                tracerBuilder
                    .AddSource("FS.AspNetCore.ResponseWrapper")
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.request.path", request.Path);
                        };
                        opts.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response.status_code", response.StatusCode);
                        };
                    });

                configureTracing?.Invoke(tracerBuilder);
            })
            .WithMetrics(metricsBuilder =>
            {
                metricsBuilder
                    .AddMeter("FS.AspNetCore.ResponseWrapper")
                    .AddAspNetCoreInstrumentation();

                configureMetrics?.Invoke(metricsBuilder);
            });

        return services;
    }

    /// <summary>
    /// Adds Response Wrapper telemetry middleware to track request timing
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>The same IApplicationBuilder instance for method chaining</returns>
    public static IApplicationBuilder UseResponseWrapperTelemetry(this IApplicationBuilder app)
    {
        app.UseMiddleware<TelemetryMiddleware>();
        return app;
    }

    /// <summary>
    /// Configures OpenTelemetry with common exporters for Response Wrapper
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="serviceName">Service name for telemetry</param>
    /// <param name="useConsoleExporter">Enable console exporter for debugging</param>
    /// <param name="useOtlpExporter">Enable OTLP exporter (for Jaeger, Zipkin, etc.)</param>
    /// <param name="otlpEndpoint">OTLP endpoint URL (default: http://localhost:4317)</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperOpenTelemetryWithExporters(
        this IServiceCollection services,
        string serviceName = "ResponseWrapperService",
        bool useConsoleExporter = false,
        bool useOtlpExporter = true,
        string otlpEndpoint = "http://localhost:4317")
    {
        services.AddResponseWrapperOpenTelemetry(
            serviceName: serviceName,
            configureTracing: builder =>
            {
                if (useConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }

                if (useOtlpExporter)
                {
                    builder.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            },
            configureMetrics: builder =>
            {
                if (useConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }

                if (useOtlpExporter)
                {
                    builder.AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            });

        return services;
    }
}
