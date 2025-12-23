using FS.AspNetCore.ResponseWrapper.OpenApi.Scalar.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.Scalar;

/// <summary>
/// Extension methods for configuring Scalar OpenAPI integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures Scalar options to properly display Response Wrapper structure
    /// </summary>
    /// <param name="options">Scalar options</param>
    /// <param name="configureOptions">Optional configuration for Response Wrapper Scalar integration</param>
    /// <returns>The same ScalarOptions instance for method chaining</returns>
    public static ScalarOptions ConfigureForResponseWrapper(
        this ScalarOptions options,
        Action<OpenApiResponseWrapperOptions>? configureOptions = null)
    {
        var wrapperOptions = new OpenApiResponseWrapperOptions();
        configureOptions?.Invoke(wrapperOptions);

        // Configure Scalar to properly display Response Wrapper structure
        options.Title = "API Documentation";

        // Add custom CSS if provided
        if (!string.IsNullOrEmpty(wrapperOptions.CustomCss))
        {
            options.CustomCss = wrapperOptions.CustomCss;
        }

        return options;
    }

    /// <summary>
    /// Configures Scalar options to properly display Response Wrapper structure with default settings
    /// </summary>
    /// <param name="options">Scalar options</param>
    /// <param name="showWrapperInfo">Whether to highlight wrapper structure in UI</param>
    /// <returns>The same ScalarOptions instance for method chaining</returns>
    public static ScalarOptions ConfigureForResponseWrapper(
        this ScalarOptions options,
        bool showWrapperInfo = true)
    {
        return options.ConfigureForResponseWrapper(opts =>
        {
            opts.ShowWrapperInfo = showWrapperInfo;
        });
    }

    /// <summary>
    /// Maps Scalar endpoints with Response Wrapper configuration
    /// </summary>
    /// <param name="builder">Endpoint route builder</param>
    /// <param name="pattern">The route pattern for Scalar UI (default: /scalar/{documentName})</param>
    /// <returns>The same endpoint route builder for method chaining</returns>
    public static IEndpointRouteBuilder MapScalarWithResponseWrapper(
        this IEndpointRouteBuilder builder,
        string pattern = "/scalar/{documentName}")
    {
        builder.MapScalarApiReference(options =>
        {
            options.WithTitle("API Documentation")
                   .WithTheme(ScalarTheme.Purple)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        return builder;
    }
}
