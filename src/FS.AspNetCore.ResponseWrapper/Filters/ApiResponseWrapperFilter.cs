using System.Diagnostics;
using System.Reflection;
using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.Models.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FS.AspNetCore.ResponseWrapper.Filters;

/// <summary>
/// An action filter that automatically wraps API responses in a consistent format with metadata injection.
/// This filter provides execution time tracking, pagination metadata extraction, query statistics integration,
/// and correlation ID management for ASP.NET Core API controllers.
/// </summary>
/// <remarks>
/// The filter operates in two phases:
/// 1. Pre-execution: Sets up timing, correlation tracking, and request metadata
/// 2. Post-execution: Wraps the response data and injects comprehensive metadata
/// 
/// The filter respects configuration options to enable/disable specific features for optimal performance.
/// It automatically detects paged results and transforms them while preserving pagination information in metadata.
/// </remarks>
public class ApiResponseWrapperFilter : IAsyncActionFilter
{
    private readonly ILogger<ApiResponseWrapperFilter> _logger;
    private readonly Func<DateTime> _getCurrentTime;
    private readonly ResponseWrapperOptions _options;

    /// <summary>
    /// Initializes a new instance of the ApiResponseWrapperFilter with required dependencies.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic and debug information</param>
    /// <param name="getCurrentTime">Function to provide current DateTime, useful for testing and timezone control</param>
    /// <param name="options">Configuration options that control filter behavior and feature enablement</param>
    public ApiResponseWrapperFilter(
        ILogger<ApiResponseWrapperFilter> logger,
        Func<DateTime> getCurrentTime,
        ResponseWrapperOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _getCurrentTime = getCurrentTime ?? throw new ArgumentNullException(nameof(getCurrentTime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Executes the filter logic around action execution, providing pre and post-processing capabilities.
    /// This method sets up request tracking, executes the action, and then wraps the response if applicable.
    /// </summary>
    /// <param name="context">The action executing context containing request information</param>
    /// <param name="next">Delegate to execute the next filter or action in the pipeline</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check if this request should be excluded from wrapping based on configuration
        if (ShouldExcludeRequest(context.HttpContext))
        {
            await next();
            return;
        }

        // Pre-execution: Initialize request tracking and timing
        var requestTrackingData = InitializeRequestTracking(context.HttpContext);

        if (_options.EnableExecutionTimeTracking)
        {
            _logger.LogDebug("Request started: {RequestId} - {Method} {Path}",
                requestTrackingData.RequestId, context.HttpContext.Request.Method, context.HttpContext.Request.Path);
        }

        // Execute the actual action
        var executedContext = await next();

        // Post-execution: Wrap response if conditions are met
        if (ShouldWrapResponse(executedContext))
        {
            await WrapResponseWithMetadata(executedContext, requestTrackingData);
        }

        if (_options.EnableExecutionTimeTracking)
        {
            requestTrackingData.Stopwatch?.Stop();
            _logger.LogDebug("Request completed: {RequestId} in {ElapsedMs}ms",
                requestTrackingData.RequestId, requestTrackingData.Stopwatch?.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Determines whether a request should be excluded from response wrapping based on configuration.
    /// This method checks excluded paths and provides early exit for non-API endpoints.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing request information</param>
    /// <returns>True if the request should be excluded from wrapping; otherwise, false</returns>
    private bool ShouldExcludeRequest(HttpContext httpContext)
    {
        var requestPath = httpContext.Request.Path.Value ?? string.Empty;
        
        // Check if path is in exclusion list
        return _options.ExcludedPaths.Any(excludedPath => 
            requestPath.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initializes request tracking data including timing, correlation IDs, and request metadata.
    /// This method sets up the foundation for metadata that will be included in the response.
    /// </summary>
    /// <param name="httpContext">The HTTP context to store tracking information</param>
    /// <returns>Request tracking data structure containing timing and identification information</returns>
    private RequestTrackingData InitializeRequestTracking(HttpContext httpContext)
    {
        var requestId = Guid.NewGuid().ToString();
        var correlationId = _options.EnableCorrelationId ? GetCorrelationId(httpContext) : null;
        var timestamp = _getCurrentTime();
        var stopwatch = _options.EnableExecutionTimeTracking ? Stopwatch.StartNew() : null;

        var trackingData = new RequestTrackingData
        {
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            Stopwatch = stopwatch
        };

        // Store tracking data in HttpContext for later retrieval
        httpContext.Items["RequestTrackingData"] = trackingData;

        return trackingData;
    }

    /// <summary>
    /// Determines whether a response should be wrapped based on result type and configuration.
    /// This method implements the core logic for deciding when to apply response wrapping.
    /// </summary>
    /// <param name="context">The action executed context containing the result</param>
    /// <returns>True if the response should be wrapped; otherwise, false</returns>
    private bool ShouldWrapResponse(ActionExecutedContext context)
    {
        // Don't wrap if success response wrapping is disabled
        if (!_options.WrapSuccessResponses)
            return false;

        // Don't wrap if there was an exception (handled by middleware)
        if (context.Exception != null)
            return false;

        // Don't wrap specific result types that shouldn't be modified
        switch (context.Result)
        {
            case FileResult:
            case RedirectResult:
            case RedirectToActionResult:
                return false;
        }

        // Don't wrap responses that are already wrapped
        if (IsAlreadyWrapped(context.Result))
            return false;

        // Check if result type is in exclusion list
        if (context.Result != null && _options.ExcludedTypes.Contains(context.Result.GetType()))
            return false;

        // Only wrap API controllers (those with ApiController attribute)
        return context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null;
    }

    /// <summary>
    /// Checks if a result is already wrapped in ApiResponse format to prevent double-wrapping.
    /// This prevents infinite nesting of response wrappers.
    /// </summary>
    /// <param name="result">The action result to check</param>
    /// <returns>True if the result is already wrapped; otherwise, false</returns>
    private static bool IsAlreadyWrapped(IActionResult? result)
    {
        if (result is ObjectResult objectResult && objectResult.Value != null)
        {
            var valueType = objectResult.Value.GetType();
            return valueType.IsGenericType && 
                   valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>);
        }

        return false;
    }

    /// <summary>
    /// Wraps the response data with comprehensive metadata and replaces the original result.
    /// This is the core transformation method that creates the final wrapped response.
    /// </summary>
    /// <param name="context">The action executed context containing the original result</param>
    /// <param name="trackingData">Request tracking data collected during execution</param>
    /// <returns>A task representing the asynchronous wrapping operation</returns>
    private async Task WrapResponseWithMetadata(ActionExecutedContext context, RequestTrackingData trackingData)
    {
        if (context.Result is not ObjectResult { Value: not null } objectResult)
            return;

        var originalData = objectResult.Value;
        var metadata = await BuildResponseMetadata(context, trackingData, originalData);
        var wrappedResponse = CreateWrappedResponse(originalData, metadata);

        // Replace the original result with wrapped version, preserving result-specific behavior
        context.Result = context.Result switch
        {
            CreatedAtActionResult createdResult => new CreatedAtActionResult(
                createdResult.ActionName,
                createdResult.ControllerName,
                createdResult.RouteValues,
                wrappedResponse)
            {
                StatusCode = 201,
                ContentTypes = { "application/json" },
                DeclaredType = wrappedResponse.GetType()
            },
            
            ObjectResult _ => new ObjectResult(wrappedResponse)
            {
                StatusCode = objectResult.StatusCode,
                ContentTypes = objectResult.ContentTypes,
                DeclaredType = wrappedResponse.GetType()
            },
            
            _ => throw new InvalidOperationException($"Unhandled result type: {context.Result.GetType().Name}")
        };
    }

    /// <summary>
    /// Builds comprehensive response metadata from request context and tracking data.
    /// This method orchestrates the collection of all metadata components based on configuration.
    /// </summary>
    /// <param name="context">The action executed context</param>
    /// <param name="trackingData">Request tracking data</param>
    /// <param name="originalData">The original response data for analysis</param>
    /// <returns>A task containing the complete response metadata</returns>
    private Task<ResponseMetadata> BuildResponseMetadata(
        ActionExecutedContext context,
        RequestTrackingData trackingData,
        object originalData)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        var metadata = new ResponseMetadata
        {
            RequestId = trackingData.RequestId,
            Timestamp = trackingData.Timestamp,
            Path = request.Path.Value ?? "",
            Method = request.Method,
            Version = GetApiVersion(httpContext)
        };

        // Conditionally add metadata based on configuration options
        if (_options.EnableCorrelationId)
        {
            metadata.CorrelationId = trackingData.CorrelationId;
        }

        if (_options.EnableExecutionTimeTracking && trackingData.Stopwatch != null)
        {
            metadata.ExecutionTimeMs = trackingData.Stopwatch.ElapsedMilliseconds;
        }

        if (_options.EnablePaginationMetadata)
        {
            metadata.Pagination = ExtractPaginationMetadata(originalData);
        }

        if (_options.EnableQueryStatistics)
        {
            metadata.Query = ExtractQueryMetadata(httpContext);
        }

        // Always extract additional metadata (it's lightweight and generally useful)
        metadata.Additional = ExtractAdditionalMetadata(httpContext);

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Extracts pagination metadata from data objects that implement IPagedResult interface.
    /// This method enables automatic pagination information extraction for paged responses.
    /// </summary>
    /// <param name="data">The response data to analyze for pagination information</param>
    /// <returns>Pagination metadata if the data is paged; otherwise, null</returns>
    private static PaginationMetadata? ExtractPaginationMetadata(object data)
    {
        if (data is not IPagedResult pagedResult)
            return null;

        return new PaginationMetadata
        {
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            TotalItems = pagedResult.TotalItems,
            HasNextPage = pagedResult.HasNextPage,
            HasPreviousPage = pagedResult.HasPreviousPage
        };
    }

    /// <summary>
    /// Extracts database query statistics from HttpContext items.
    /// This works in conjunction with Entity Framework interceptors to provide query performance metrics.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing query statistics</param>
    /// <returns>Query metadata if available; otherwise, null</returns>
    private static QueryMetadata? ExtractQueryMetadata(HttpContext httpContext)
    {
        if (httpContext.Items["QueryStats"] is not Dictionary<string, object> queryStats)
            return null;

        return new QueryMetadata
        {
            DatabaseQueriesCount = (int)queryStats.GetValueOrDefault("QueriesCount", 0),
            DatabaseExecutionTimeMs = (long)queryStats.GetValueOrDefault("ExecutionTimeMs", 0L),
            CacheHits = (int)queryStats.GetValueOrDefault("CacheHits", 0),
            CacheMisses = (int)queryStats.GetValueOrDefault("CacheMisses", 0),
            ExecutedQueries = queryStats.GetValueOrDefault("ExecutedQueries") as string[]
        };
    }

    /// <summary>
    /// Extracts additional request metadata such as client information and custom headers.
    /// This provides supplementary information that can be useful for debugging and analytics.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing request information</param>
    /// <returns>Dictionary of additional metadata if any is found; otherwise, null</returns>
    private static Dictionary<string, object>? ExtractAdditionalMetadata(HttpContext httpContext)
    {
        var additional = new Dictionary<string, object>();

        // Add request size information
        if (httpContext.Request.ContentLength.HasValue)
        {
            additional["RequestSizeBytes"] = httpContext.Request.ContentLength.Value;
        }

        // Add user agent information
        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            additional["UserAgent"] = userAgent;
        }

        // Add client IP information
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            additional["ClientIP"] = clientIp;
        }

        // Add custom headers that start with X-Custom-
        var customHeaders = httpContext.Request.Headers
            .Where(h => h.Key.StartsWith("X-Custom-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => (object)h.Value.ToString());

        foreach (var header in customHeaders)
        {
            additional[header.Key] = header.Value;
        }

        return additional.Count > 0 ? additional : null;
    }

    /// <summary>
    /// Transforms paged results into clean data structures while preserving pagination information in metadata.
    /// This method separates business data from pagination metadata for cleaner API responses.
    /// </summary>
    /// <param name="originalData">The original paged data</param>
    /// <param name="dataType">The type of the original data</param>
    /// <returns>Tuple containing transformed data and its type</returns>
    private (object transformedData, Type transformedDataType) TransformPagedResult(object originalData, Type dataType)
    {
        if (originalData is not IPagedResult)
            return (originalData, dataType);

        // Extract items from PagedResult<T>
        var itemsProperty = dataType.GetProperty("Items");
        if (itemsProperty == null)
        {
            _logger.LogWarning("Items property not found on type {DataType}", dataType.Name);
            return (originalData, dataType);
        }

        var items = itemsProperty.GetValue(originalData);
        if (items == null)
        {
            _logger.LogWarning("Items collection is null for type {DataType}", dataType.Name);
            return (originalData, dataType);
        }

        // Determine item type from PagedResult<T>
        var itemType = GetItemTypeFromPagedResult(dataType);
        if (itemType == null)
        {
            _logger.LogWarning("Could not determine item type from {DataType}", dataType.Name);
            return (originalData, dataType);
        }

        // Create CleanPagedResult<T> containing only business data
        var cleanResultType = typeof(CleanPagedResult<>).MakeGenericType(itemType);
        var cleanResult = Activator.CreateInstance(cleanResultType);

        if (cleanResult == null)
        {
            _logger.LogError("Failed to create clean result type for {ItemType}", itemType.Name);
            return (originalData, dataType);
        }

        // Set only Items property, excluding pagination metadata
        var cleanItemsProperty = cleanResultType.GetProperty("Items");
        cleanItemsProperty?.SetValue(cleanResult, items);

        _logger.LogDebug("Successfully transformed PagedResult<{ItemType}> to CleanPagedResult<{ItemType}>",
            itemType.Name, itemType.Name);

        return (cleanResult, cleanResultType);
    }

    /// <summary>
    /// Creates the final wrapped response using the ApiResponse wrapper with provided data and metadata.
    /// This method performs the actual wrapping transformation and ensures type safety.
    /// </summary>
    /// <param name="originalData">The original response data</param>
    /// <param name="metadata">The metadata to include in the response</param>
    /// <returns>The wrapped response object</returns>
    private object CreateWrappedResponse(object originalData, ResponseMetadata metadata)
    {
        var dataType = originalData.GetType();
        var (transformedData, transformedDataType) = TransformPagedResult(originalData, dataType);

        var responseType = typeof(ApiResponse<>).MakeGenericType(transformedDataType);
        var response = Activator.CreateInstance(responseType);

        if (response == null)
        {
            throw new InvalidOperationException($"Failed to create ApiResponse<{transformedDataType.Name}>");
        }

        // Set response properties using reflection
        var dataProperty = responseType.GetProperty("Data");
        var successProperty = responseType.GetProperty("Success");
        var metadataProperty = responseType.GetProperty("Metadata");

        if (dataProperty == null || successProperty == null || metadataProperty == null)
        {
            throw new InvalidOperationException($"ApiResponse<{transformedDataType.Name}> missing required properties");
        }

        dataProperty.SetValue(response, transformedData);
        successProperty.SetValue(response, true);
        metadataProperty.SetValue(response, metadata);

        _logger.LogDebug("Successfully created ApiResponse<{TransformedType}>", transformedDataType.Name);

        return response;
    }

    /// <summary>
    /// Extracts or generates a correlation ID for request tracking across distributed systems.
    /// This method checks for existing correlation ID headers or generates a new one.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing request headers</param>
    /// <returns>The correlation ID for this request</returns>
    private static string GetCorrelationId(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? httpContext.TraceIdentifier
               ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Extracts API version information from request headers or query parameters.
    /// This method provides version tracking for API analytics and compatibility management.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing version information</param>
    /// <returns>The API version string</returns>
    private static string GetApiVersion(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-API-Version"].FirstOrDefault()
               ?? httpContext.Request.Query["version"].FirstOrDefault()
               ?? "1.0";
    }

    /// <summary>
    /// Extracts the item type from a generic PagedResult type.
    /// This method enables type-safe transformation of paged results.
    /// </summary>
    /// <param name="pagedResultType">The PagedResult type to analyze</param>
    /// <returns>The item type if found; otherwise, null</returns>
    private static Type? GetItemTypeFromPagedResult(Type pagedResultType)
    {
        if (!pagedResultType.IsGenericType)
            return null;

        var genericArgs = pagedResultType.GetGenericArguments();
        return genericArgs.Length > 0 ? genericArgs[0] : null;
    }

    /// <summary>
    /// Internal data structure for tracking request-specific information throughout the filter lifecycle.
    /// This class encapsulates all the timing and identification data needed for metadata generation.
    /// </summary>
    private class RequestTrackingData
    {
        public string RequestId { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public Stopwatch? Stopwatch { get; set; }
    }
}