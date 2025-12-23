using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Filters;
using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Models;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle;

/// <summary>
/// Extension methods for configuring Swashbuckle OpenAPI integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Response Wrapper integration to Swashbuckle OpenAPI documentation
    /// </summary>
    /// <param name="options">Swashbuckle options</param>
    /// <param name="configureOptions">Optional configuration for Response Wrapper OpenAPI integration</param>
    /// <returns>The same SwaggerGenOptions instance for method chaining</returns>
    public static SwaggerGenOptions AddResponseWrapper(
        this SwaggerGenOptions options,
        Action<OpenApiResponseWrapperOptions>? configureOptions = null)
    {
        var wrapperOptions = new OpenApiResponseWrapperOptions();
        configureOptions?.Invoke(wrapperOptions);

        // Add operation filter to wrap responses
        options.OperationFilter<ResponseWrapperOperationFilter>(wrapperOptions);

        // Add schema filter for ApiResponse models
        options.SchemaFilter<ResponseWrapperSchemaFilter>();

        return options;
    }

    /// <summary>
    /// Configures Swashbuckle to automatically document all responses as wrapped with ApiResponse
    /// </summary>
    /// <param name="options">Swashbuckle options</param>
    /// <param name="includeErrorExamples">Whether to include error response examples (400, 401, 500)</param>
    /// <param name="includeMetadata">Whether to include metadata schema in responses</param>
    /// <returns>The same SwaggerGenOptions instance for method chaining</returns>
    public static SwaggerGenOptions AddResponseWrapper(
        this SwaggerGenOptions options,
        bool includeErrorExamples = true,
        bool includeMetadata = true)
    {
        return options.AddResponseWrapper(opts =>
        {
            opts.IncludeErrorExamples = includeErrorExamples;
            opts.IncludeMetadataSchema = includeMetadata;
        });
    }
}
