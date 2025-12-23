using FS.AspNetCore.ResponseWrapper.OpenApi.NSwag.Models;
using FS.AspNetCore.ResponseWrapper.OpenApi.NSwag.Processors;
using NSwag.Generation.AspNetCore;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.NSwag;

/// <summary>
/// Extension methods for configuring NSwag OpenAPI integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Response Wrapper integration to NSwag OpenAPI documentation
    /// </summary>
    /// <param name="settings">NSwag OpenAPI document settings</param>
    /// <param name="configureOptions">Optional configuration for Response Wrapper OpenAPI integration</param>
    /// <returns>The same AspNetCoreOpenApiDocumentGeneratorSettings instance for method chaining</returns>
    public static AspNetCoreOpenApiDocumentGeneratorSettings AddResponseWrapper(
        this AspNetCoreOpenApiDocumentGeneratorSettings settings,
        Action<OpenApiResponseWrapperOptions>? configureOptions = null)
    {
        var wrapperOptions = new OpenApiResponseWrapperOptions();
        configureOptions?.Invoke(wrapperOptions);

        // Add operation processor to wrap responses
        settings.OperationProcessors.Add(new ResponseWrapperOperationProcessor(wrapperOptions));

        return settings;
    }

    /// <summary>
    /// Configures NSwag to automatically document all responses as wrapped with ApiResponse
    /// </summary>
    /// <param name="settings">NSwag OpenAPI document settings</param>
    /// <param name="includeErrorExamples">Whether to include error response examples (400, 401, 500)</param>
    /// <param name="includeMetadata">Whether to include metadata schema in responses</param>
    /// <returns>The same AspNetCoreOpenApiDocumentGeneratorSettings instance for method chaining</returns>
    public static AspNetCoreOpenApiDocumentGeneratorSettings AddResponseWrapper(
        this AspNetCoreOpenApiDocumentGeneratorSettings settings,
        bool includeErrorExamples = true,
        bool includeMetadata = true)
    {
        return settings.AddResponseWrapper(opts =>
        {
            opts.IncludeErrorExamples = includeErrorExamples;
            opts.IncludeMetadataSchema = includeMetadata;
        });
    }
}
