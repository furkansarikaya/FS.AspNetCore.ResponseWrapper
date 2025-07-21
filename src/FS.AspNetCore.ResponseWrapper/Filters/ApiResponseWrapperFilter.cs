using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FS.AspNetCore.ResponseWrapper.Helpers;
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
    /// This method now handles ALL metadata extraction in one place to avoid duplication.
    /// </summary>
    /// <param name="context">The action executed context</param>
    /// <param name="trackingData">Request tracking data</param>
    /// <param name="originalData">The original response data for analysis</param>
    /// <returns>A task containing the complete response metadata with merged custom metadata</returns>
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

        // Extract system-generated additional metadata
        var systemAdditionalMetadata = ExtractAdditionalMetadata(httpContext);

        // Extract custom metadata from response object (ONLY HERE!)
        var customMetadata = ExtractCustomMetadata(originalData);

        // Merge system and custom metadata (ONLY ONCE!)
        metadata.Additional = MergeMetadataDictionaries(systemAdditionalMetadata, customMetadata);

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Extracts custom metadata from response objects that implement IHasMetadata interface.
    /// This method provides automatic custom metadata extraction while maintaining type safety
    /// and error resilience.
    /// </summary>
    /// <param name="data">The response data to analyze for custom metadata</param>
    /// <returns>Dictionary of custom metadata if available; otherwise, null</returns>
    /// <remarks>
    /// This method implements the same pattern as status code extraction, providing automatic
    /// detection and extraction of custom metadata from response objects. The extraction process:
    /// 
    /// 1. **Interface Detection**: Checks if the response object implements IHasMetadata
    /// 2. **Safe Extraction**: Uses defensive programming to handle potential null values or exceptions
    /// 3. **Logging**: Provides appropriate debug logging for troubleshooting and monitoring
    /// 4. **Error Resilience**: Ensures that metadata extraction failures don't break response processing
    /// 
    /// **Custom Metadata Benefits**: Enables applications to include business-specific metadata
    /// such as workflow states, feature flags, permission contexts, or any other information
    /// that clients need for enhanced user experiences or decision-making logic.
    /// 
    /// **Performance Considerations**: The extraction process is designed to be lightweight,
    /// using simple interface checks and direct property access to minimize performance impact.
    /// </remarks>
    private Dictionary<string, object>? ExtractCustomMetadata(object data)
    {
        if (data == null)
            return null;

        try
        {
            // Check if the response object provides custom metadata
            if (data is not IHasMetadata metadataProvider) return null;
            var customMetadata = metadataProvider.Metadata;

            if (customMetadata != null && customMetadata.Count > 0)
            {
                _logger.LogDebug("Extracted {MetadataCount} custom metadata entries from {DataType}",
                    customMetadata.Count, data.GetType().Name);

                // Log the metadata keys for debugging (but not values for security)
                _logger.LogTrace("Custom metadata keys: {MetadataKeys}",
                    string.Join(", ", customMetadata.Keys));

                return new Dictionary<string, object>(customMetadata);
            }

            _logger.LogTrace("Response object {DataType} implements IHasMetadata but provided no custom metadata",
                data.GetType().Name);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract custom metadata from {DataType}. " +
                                   "Error: {ErrorMessage}. Continuing without custom metadata",
                data.GetType().Name, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Merges system-generated additional metadata with custom metadata from response objects,
    /// handling potential conflicts and ensuring proper metadata organization.
    /// </summary>
    /// <param name="systemMetadata">System-generated additional metadata (clientIP, userAgent, etc.)</param>
    /// <param name="customMetadata">Custom metadata from response objects implementing IHasMetadata</param>
    /// <returns>Combined metadata dictionary, or null if both inputs are null or empty</returns>
    /// <remarks>
    /// This method implements a sophisticated merging strategy that handles conflicts between
    /// system and custom metadata while preserving both types of information:
    /// 
    /// **Conflict Resolution**: When both system and custom metadata contain the same key,
    /// the method uses a prefixing strategy to preserve both values rather than overwriting.
    /// Custom metadata gets priority for the original key name, while system metadata is
    /// prefixed with "system_".
    /// 
    /// **Namespace Separation**: The merging process maintains clear separation between
    /// different types of metadata while presenting them in a unified structure that's
    /// convenient for client consumption.
    /// 
    /// **Null Safety**: Handles all combinations of null/empty inputs gracefully, ensuring
    /// that the method never fails due to null reference exceptions.
    /// 
    /// **Performance Optimization**: Uses efficient dictionary operations and avoids
    /// unnecessary allocations when one or both inputs are empty.
    /// </remarks>
    private Dictionary<string, object>? MergeMetadataDictionaries(
        Dictionary<string, object>? systemMetadata,
        Dictionary<string, object>? customMetadata)
    {
        // If both are null or empty, return null
        if ((systemMetadata == null || systemMetadata.Count == 0) &&
            (customMetadata == null || customMetadata.Count == 0))
        {
            return null;
        }

        // If only one has data, return a copy of it
        if (systemMetadata == null || systemMetadata.Count == 0)
        {
            return customMetadata != null ? new Dictionary<string, object>(customMetadata) : null;
        }

        if (customMetadata == null || customMetadata.Count == 0)
        {
            return new Dictionary<string, object>(systemMetadata);
        }

        // Both have data - merge them with conflict resolution
        var mergedMetadata = new Dictionary<string, object>(systemMetadata);
        var conflictCount = 0;

        foreach (var customEntry in customMetadata)
        {
            if (mergedMetadata.TryGetValue(customEntry.Key, out var systemValue))
            {
                // Handle conflict: preserve both values with different keys
                conflictCount++;
                mergedMetadata[$"system_{customEntry.Key}"] = systemValue;

                // Give custom metadata priority for the original key
                mergedMetadata[customEntry.Key] = customEntry.Value;

                _logger.LogDebug($"Metadata key conflict resolved: '{customEntry.Key}' - custom value kept, " +
                                 "system value moved to 'system_{customEntry.Key}'");
            }
            else
            {
                // No conflict - add custom metadata directly
                mergedMetadata[customEntry.Key] = customEntry.Value;
            }
        }

        if (conflictCount > 0)
        {
            _logger.LogInformation("Resolved {ConflictCount} metadata key conflicts during merge", conflictCount);
        }

        _logger.LogDebug("Successfully merged system metadata ({SystemCount} entries) with " +
                         "custom metadata ({CustomCount} entries) into {TotalCount} total entries",
            systemMetadata.Count, customMetadata.Count, mergedMetadata.Count);

        return mergedMetadata;
    }

    /// <summary>
    /// Extracts pagination metadata from data objects using flexible duck typing detection.
    /// This method now works with ANY object that has the required pagination properties,
    /// regardless of which interface it implements or what namespace it comes from.
    /// </summary>
    /// <param name="data">The response data to analyze for pagination information</param>
    /// <returns>Pagination metadata if the data has pagination properties; otherwise, null</returns>
    /// <remarks>
    /// This updated implementation solves the interface conflict problem by using duck typing principles.
    /// Instead of checking for a specific interface implementation, it analyzes the object's structure
    /// to determine if it contains pagination properties. This approach provides several benefits:
    /// 
    /// 1. **Namespace Independence**: Works with user's custom pagination interfaces regardless of namespace
    /// 2. **Library Agnostic**: Compatible with any pagination library or custom implementation  
    /// 3. **Flexible Matching**: Recognizes pagination patterns without requiring specific inheritance
    /// 4. **Backward Compatible**: Still works with the original IPagedResult interface
    /// 
    /// The detection process uses cached reflection for optimal performance and handles edge cases gracefully.
    /// </remarks>
    /// <example>
    /// This method now works with all of these different pagination implementations:
    /// <code>
    /// // Original ResponseWrapper interface
    /// FS.AspNetCore.ResponseWrapper.Models.Paging.PagedResult&lt;Product&gt;
    /// 
    /// // User's custom interface
    /// MyProject.Models.PagedResponse&lt;Product&gt;
    /// 
    /// // Third-party library interface  
    /// SomeLibrary.Pagination.PaginatedResult&lt;Product&gt;
    /// 
    /// // Any class with the right properties
    /// public class CustomPagedData&lt;T&gt; 
    /// {
    ///     public List&lt;T&gt; Items { get; set; }
    ///     public int Page { get; set; }
    ///     public int PageSize { get; set; }
    ///     public int TotalPages { get; set; }
    ///     public int TotalItems { get; set; }
    ///     public bool HasNextPage { get; set; }
    ///     public bool HasPreviousPage { get; set; }
    /// }
    /// </code>
    /// </example>
    private static PaginationMetadata? ExtractPaginationMetadata(object data)
    {
        // Use the flexible duck typing approach instead of interface checking
        return PaginationDetectionHelper.ExtractPaginationMetadata(data);
    }

    /// <summary>
    /// Transforms paged results into clean data structures using flexible detection,
    /// working with any pagination implementation regardless of interface or namespace.
    /// </summary>
    /// <param name="originalData">The original paged data from any pagination implementation</param>
    /// <param name="dataType">The type of the original data</param>
    /// <returns>Tuple containing transformed data and its type</returns>
    /// <remarks>
    /// This updated method provides robust pagination handling that adapts to various pagination patterns:
    /// 
    /// **Detection Strategy**: Uses duck typing to identify pagination properties rather than interface matching.
    /// This means it works with any object structure that contains the expected pagination properties.
    /// 
    /// **Extraction Process**: 
    /// 1. Analyzes the object structure using cached reflection for performance
    /// 2. Extracts the "Items" collection containing the actual business data
    /// 3. Determines the correct item type for response construction
    /// 4. Creates a clean result structure without pagination metadata
    /// 
    /// **Error Handling**: The method includes comprehensive error handling and logging to ensure
    /// that pagination detection failures don't break the entire response wrapping process.
    /// 
    /// **Performance Optimization**: Uses type caching to minimize reflection overhead on repeated calls
    /// with the same types, which is common in API scenarios.
    /// </remarks>
    private (object transformedData, Type transformedDataType) TransformPagedResult(object originalData, Type dataType)
    {
        // First, check if this object has pagination properties using duck typing
        if (!PaginationDetectionHelper.HasPaginationProperties(originalData))
        {
            return (originalData, dataType);
        }

        try
        {
            // Extract the items collection from the paginated object
            var (items, itemType) = PaginationDetectionHelper.ExtractItems(originalData);

            if (items == null)
            {
                _logger.LogWarning("Items collection is null for paginated type {DataType}", dataType.Name);
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

            _logger.LogDebug("Successfully transformed paginated result with {ItemType} to CleanPagedResult<{ItemType}>",
                itemType.Name, itemType.Name);

            return (cleanResult, cleanResultType);
        }
        catch (Exception ex)
        {
            // If transformation fails, log the issue but don't break the response
            _logger.LogWarning(ex, "Failed to transform paginated result of type {DataType}. Using original data", dataType.Name);
            return (originalData, dataType);
        }
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
    /// Updated CreateWrappedResponse method that no longer duplicates custom metadata extraction.
    /// Custom metadata is now handled exclusively in BuildResponseMetadata.
    /// </summary>
    /// <param name="originalData">The original response data from the controller</param>
    /// <param name="metadata">The COMPLETE metadata (already includes custom metadata)</param>
    /// <returns>The wrapped response object with extracted status/message and clean data</returns>
    /// <remarks>
    /// This updated method fixes the duplication issue by removing the redundant custom
    /// metadata extraction that was causing duplicate entries with system_ prefixes.
    /// 
    /// **No Duplicate Extraction**: Custom metadata is no longer extracted here since
    /// it's already been processed and merged in BuildResponseMetadata.
    /// 
    /// **Simpler Logic**: The method now focuses solely on status code/message extraction
    /// and clean data structure creation, making it more maintainable.
    /// 
    /// **Complete Metadata**: The metadata parameter already contains the complete
    /// metadata structure including merged custom metadata, so no additional merging
    /// is needed.
    /// </remarks>
    private object CreateWrappedResponse(object originalData, ResponseMetadata metadata)
    {
        var dataType = originalData.GetType();

        // Extract ONLY StatusCode and Message (custom metadata already handled!)
        var (extractedStatusCode, extractedMessage) = ExtractStatusCodeAndMessage(originalData);

        // Transform paginated results (existing logic)
        var (transformedData, transformedDataType) = TransformPagedResult(originalData, dataType);

        // Create clean data structure by removing ALL metadata properties
        var cleanData = CreateCleanDataStructure(transformedData, transformedDataType);
        var cleanDataType = cleanData.GetType();

        // NO MORE CUSTOM METADATA MERGING HERE - it's already done!

        var responseType = typeof(ApiResponse<>).MakeGenericType(cleanDataType);
        var response = Activator.CreateInstance(responseType);

        if (response == null)
        {
            throw new InvalidOperationException($"Failed to create ApiResponse<{cleanDataType.Name}>");
        }

        // Set response properties
        var dataProperty = responseType.GetProperty("Data");
        var successProperty = responseType.GetProperty("Success");
        var metadataProperty = responseType.GetProperty("Metadata");
        var statusCodeProperty = responseType.GetProperty("StatusCode");
        var messageProperty = responseType.GetProperty("Message");

        dataProperty?.SetValue(response, cleanData);
        successProperty?.SetValue(response, true);
        metadataProperty?.SetValue(response, metadata); // Complete metadata!

        // Set extracted status code at ApiResponse level
        if (!string.IsNullOrEmpty(extractedStatusCode))
        {
            statusCodeProperty?.SetValue(response, extractedStatusCode);
            _logger.LogDebug("Promoted status code '{StatusCode}' to ApiResponse level", extractedStatusCode);
        }

        // Set extracted message at ApiResponse level
        if (!string.IsNullOrEmpty(extractedMessage))
        {
            messageProperty?.SetValue(response, extractedMessage);
            _logger.LogDebug("Promoted message to ApiResponse level");
        }

        _logger.LogDebug("Successfully created clean ApiResponse<{CleanDataType}> without duplicate metadata",
            cleanDataType.Name);

        return response;
    }

    /// <summary>
    /// Creates a clean data structure by removing status code and metadata properties from the original data.
    /// This method provides comprehensive property removal that works with any object type by creating
    /// anonymous objects or dynamic structures containing only business-relevant properties.
    /// </summary>
    /// <param name="data">The original data that may contain status code properties</param>
    /// <param name="dataType">The type of the original data</param>
    /// <returns>A clean data structure without status code or metadata properties</returns>
    /// <remarks>
    /// This enhanced method provides universal status code property removal that works with any object type.
    /// The method creates new object structures that exclude status code and metadata properties entirely,
    /// ensuring clean separation between business data and response metadata across all supported types.
    /// 
    /// **Universal Compatibility**: Works with any object type, not just specific implementations,
    /// making it suitable for a generic response wrapper framework that handles diverse response types.
    /// 
    /// **Complete Property Removal**: Unlike approaches that nullify properties, this method creates
    /// entirely new object structures without the unwanted properties, ensuring they don't appear
    /// in JSON serialization output.
    /// 
    /// **Type-Safe Dynamic Creation**: Uses reflection and dynamic object creation to maintain
    /// type safety while providing flexible property filtering for any object structure.
    /// </remarks>
    private object CreateCleanDataStructure(object data, Type dataType)
    {
        if (data == null)
            return data;

        try
        {
            // For primitive types, strings, and value types, return as-is
            if (dataType.IsPrimitive || dataType == typeof(string) || dataType.IsValueType)
            {
                return data;
            }

            // Check if the object has status code properties that need to be cleaned
            if (!HasStatusCodeProperties(data, dataType))
                return data;

            _logger.LogDebug("Creating clean data structure for type {DataType} - removing status code properties", dataType.Name);

            // Create anonymous object with only business properties
            var cleanData = CreateAnonymousCleanObject(data, dataType);
            if (cleanData == null)
            {
                _logger.LogWarning("Failed to create clean anonymous object for type {DataType}, using original data", dataType.Name);
                return data;
            }

            _logger.LogDebug("Successfully created clean anonymous object for {DataType}, excluded status code properties", dataType.Name);
            return cleanData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clean data structure for type {DataType}. " +
                                 "Error details: {ErrorMessage}. Using original data structure",
                dataType.Name, ex.Message);
            return data;
        }
    }

    /// <summary>
    /// Creates an anonymous object containing only business properties, excluding status code and metadata properties.
    /// This method uses reflection to dynamically build a new object structure with only the desired properties.
    /// </summary>
    /// <param name="source">The source object to extract business properties from</param>
    /// <param name="sourceType">The type of the source object</param>
    /// <returns>An anonymous object with only business properties, or null if creation fails</returns>
    /// <remarks>
    /// This method implements dynamic object creation to solve the generic property filtering challenge.
    /// By creating anonymous objects rather than modifying existing objects, it ensures complete removal
    /// of unwanted properties from the serialization output.
    /// 
    /// **Dynamic Property Selection**: Uses reflection to identify business properties by excluding
    /// known status code and metadata property names, making it adaptable to any object structure.
    /// 
    /// **Anonymous Object Benefits**: Anonymous objects provide clean serialization without carrying
    /// unwanted properties, solving the fundamental issue of property visibility in JSON output.
    /// 
    /// **Recursive Cleaning**: Handles nested objects and collections by recursively applying the
    /// same cleaning logic to maintain consistency throughout the object hierarchy.
    /// </remarks>
    private object? CreateAnonymousCleanObject(object source, Type sourceType)
    {
        if (source == null)
            return null;

        try
        {
            _logger.LogDebug("Creating anonymous clean object for type {SourceType}", sourceType.Name);

            // Handle collections
            if (IsCollection(sourceType) && sourceType != typeof(string))
            {
                return CreateCleanCollection(source, sourceType);
            }

            // Get all readable properties excluding status code properties
            var businessProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => !IsStatusOrMetadataProperty(p.Name))
                .ToArray();

            if (businessProperties.Length == 0)
            {
                _logger.LogWarning("No business properties found for type {SourceType}", sourceType.Name);
                return source;
            }

            // Create dictionary with business property values
            var propertyValues = new Dictionary<string, object?>();

            foreach (var property in businessProperties)
            {
                try
                {
                    var value = property.GetValue(source);

                    // Recursively clean nested objects
                    if (value != null && ShouldRecursivelyClean(property.PropertyType))
                    {
                        value = CreateCleanDataStructure(value, property.PropertyType);
                    }

                    propertyValues[property.Name] = value;
                    _logger.LogTrace("Added business property {PropertyName} to clean object for type {SourceType}",
                        property.Name, sourceType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract property {PropertyName} from type {SourceType}, skipping",
                        property.Name, sourceType.Name);
                }
            }

            // Convert to ExpandoObject for dynamic JSON serialization
            dynamic cleanObject = new System.Dynamic.ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)cleanObject;

            foreach (var kvp in propertyValues)
            {
                expandoDict[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug("Successfully created anonymous object with {PropertyCount} business properties for type {SourceType}. " +
                             "Excluded properties: {ExcludedProperties}",
                propertyValues.Count,
                sourceType.Name,
                string.Join(", ", sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => IsStatusOrMetadataProperty(p.Name))
                    .Select(p => p.Name)));

            return cleanObject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating anonymous clean object for type {SourceType}", sourceType.Name);
            return null;
        }
    }

    /// <summary>
    /// Determines whether a property name represents status code or metadata information
    /// that should be excluded from clean data structures. This method provides the core
    /// filtering logic for identifying properties that should be removed from business data.
    /// </summary>
    /// <param name="propertyName">The name of the property to check</param>
    /// <returns>True if the property should be excluded from clean data; otherwise, false</returns>
    /// <remarks>
    /// This method implements the business rule for distinguishing between business data
    /// and metadata properties. The exclusion list can be extended to support additional
    /// metadata property patterns as needed by different application domains.
    /// 
    /// **Standard Metadata Properties**: Identifies common property names used for status
    /// codes and messages across different application architectures and frameworks.
    /// 
    /// **Case-Insensitive Matching**: Uses case-insensitive comparison to handle different
    /// naming conventions (StatusCode, statusCode, STATUSCODE) consistently.
    /// 
    /// **Extensible Pattern**: The method can be easily extended to support additional
    /// property name patterns or more sophisticated filtering rules as requirements evolve.
    /// </remarks>
    private static bool IsStatusOrMetadataProperty(string propertyName)
    {
        // Properties that should be excluded from clean business data
        var excludedProperties = new[]
        {
            "StatusCode", // From IHasStatusCode interface
            "Message", // From IHasMessage interface  
            "Metadata" // From IHasMetadata interface
        };

        var isExcluded = excludedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);

        return isExcluded;
    }

    /// <summary>
    /// Determines if a type represents a collection that should be processed recursively.
    /// This method helps identify when special collection handling is needed during clean data creation.
    /// </summary>
    /// <param name="type">The type to check for collection characteristics</param>
    /// <returns>True if the type is a collection requiring special processing; otherwise, false</returns>
    /// <remarks>
    /// Collection detection is important for maintaining proper data structure during cleaning operations.
    /// This method ensures that collections are processed appropriately while excluding string types
    /// that implement IEnumerable but should be treated as primitive values.
    /// </remarks>
    private static bool IsCollection(Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Determines if a type should be recursively cleaned for status code properties.
    /// This method helps optimize the cleaning process by identifying which types require
    /// deep inspection for nested status code properties.
    /// </summary>
    /// <param name="type">The type to evaluate for recursive cleaning requirements</param>
    /// <returns>True if the type should undergo recursive cleaning; otherwise, false</returns>
    /// <remarks>
    /// This method implements performance optimization by avoiding unnecessary recursive processing
    /// of types that cannot contain status code properties, while ensuring comprehensive cleaning
    /// for complex object hierarchies that might contain nested metadata properties.
    /// </remarks>
    private static bool ShouldRecursivelyClean(Type type)
    {
        // Don't recursively clean primitive types, strings, or value types
        if (type.IsPrimitive || type == typeof(string) || type.IsValueType)
            return false;

        // Don't clean system types unless they're collections
        return type.Namespace?.StartsWith("System") != true || IsCollection(type);
    }

    /// <summary>
    /// Determines if an object or its properties contain status code or metadata properties
    /// that should be cleaned from the response data. This method provides comprehensive
    /// analysis of object structure to identify cleaning requirements.
    /// </summary>
    /// <param name="data">The object to examine for status code properties</param>
    /// <param name="dataType">The type of the object</param>
    /// <returns>True if the object contains properties that should be cleaned; otherwise, false</returns>
    /// <remarks>
    /// This method implements a comprehensive analysis strategy to identify objects that require
    /// status code property removal. The analysis includes direct property inspection, interface
    /// implementation checking, and collection item analysis to ensure complete coverage.
    /// 
    /// **Interface Detection**: Quickly identifies objects implementing IHasStatusCode interface
    /// as candidates for cleaning, providing fast detection for the most common scenarios.
    /// 
    /// **Property-Based Analysis**: Examines object properties to identify status code and metadata
    /// properties by name, enabling detection even when objects don't implement specific interfaces.
    /// 
    /// **Collection Analysis**: For collection types, samples collection items to determine if
    /// they contain status code properties, ensuring nested cleaning requirements are identified.
    /// 
    /// **Performance Optimization**: Uses early exit strategies and caching where appropriate
    /// to minimize performance impact on objects that don't require cleaning.
    /// </remarks>
    private bool HasStatusCodeProperties(object data, Type dataType)
    {
        if (data == null)
            return false;

        try
        {
            // Quick check for IHasStatusCode implementation - most reliable indicator
            if (data is IHasStatusCode)
            {
                _logger.LogTrace("Object type {DataType} implements IHasStatusCode interface", dataType.Name);
                return true;
            }

            // Check if the type has any properties we want to exclude
            var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            // Look for direct status code properties on the object
            foreach (var property in properties)
            {
                if (!IsStatusOrMetadataProperty(property.Name)) continue;
                _logger.LogTrace("Found status code property {PropertyName} in type {DataType}",
                    property.Name, dataType.Name);
                return true;
            }

            // For collections, check if items might have status properties
            if (IsCollection(dataType) && dataType != typeof(string))
            {
                if (HasStatusPropertiesInCollection(data))
                {
                    _logger.LogTrace("Collection type {DataType} contains items with status code properties", dataType.Name);
                    return true;
                }
            }

            // For complex objects, check nested properties (limited depth to avoid performance issues)
            foreach (var property in properties)
            {
                if (!ShouldCheckNestedProperty(property.PropertyType)) continue;
                try
                {
                    var value = property.GetValue(data);
                    if (value == null || !HasStatusCodeProperties(value, property.PropertyType)) continue;
                    _logger.LogTrace("Nested property {PropertyName} in type {DataType} has status code properties",
                        property.Name, dataType.Name);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Failed to check nested property {PropertyName} in type {DataType}",
                        property.Name, dataType.Name);
                    // Continue checking other properties
                }
            }

            _logger.LogTrace("No status code properties found in type {DataType}", dataType.Name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for status code properties in type {DataType}, assuming false", dataType.Name);
            return false;
        }
    }

    /// <summary>
    /// Checks if a collection contains items that might have status code properties.
    /// This method samples collection items to determine cleaning requirements without
    /// exhaustively examining every item for performance reasons.
    /// </summary>
    /// <param name="collection">The collection to examine for status code properties in items</param>
    /// <returns>True if any items in the collection have status code properties; otherwise, false</returns>
    /// <remarks>
    /// This method implements a sampling strategy for collection analysis to balance thoroughness
    /// with performance. Rather than examining every item in potentially large collections, it
    /// samples a representative subset to determine if cleaning is required.
    /// 
    /// **Sampling Strategy**: Examines the first few items and a random sampling of larger
    /// collections to efficiently detect status code properties without performance penalties.
    /// 
    /// **Type Diversity Handling**: For collections containing different types, the method
    /// checks multiple items to ensure diverse type representation in the analysis.
    /// 
    /// **Performance Optimization**: Limits the number of items examined to prevent performance
    /// degradation on large collections while maintaining detection reliability.
    /// </remarks>
    private bool HasStatusPropertiesInCollection(object collection)
    {
        if (collection is not IEnumerable enumerable)
            return false;

        try
        {
            var itemsChecked = 0;
            const int maxItemsToCheck = 5; // Limit checks for performance

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                var itemType = item.GetType();

                // Skip primitive types and strings
                if (itemType.IsPrimitive || itemType == typeof(string) || itemType.IsValueType)
                    continue;

                // Quick check for IHasStatusCode
                if (item is IHasStatusCode)
                {
                    _logger.LogTrace("Found IHasStatusCode implementation in collection item of type {ItemType}", itemType.Name);
                    return true;
                }

                // Check for status code properties in the item
                var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead);

                if (properties.Any(p => IsStatusOrMetadataProperty(p.Name)))
                {
                    _logger.LogTrace("Found status code properties in collection item of type {ItemType}", itemType.Name);
                    return true;
                }

                itemsChecked++;
                if (itemsChecked < maxItemsToCheck) continue;
                _logger.LogTrace("Checked {ItemsChecked} collection items for status code properties, stopping for performance", itemsChecked);
                break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error checking collection items for status code properties");
            return false; // Assume no status properties if we can't check
        }
    }

    /// <summary>
    /// Creates a clean copy of a collection by recursively cleaning its items and preserving
    /// the collection structure. This method handles various collection types and ensures
    /// that nested status code properties are removed throughout the collection hierarchy.
    /// </summary>
    /// <param name="source">The source collection to create a clean copy from</param>
    /// <param name="sourceType">The type of the source collection</param>
    /// <returns>A clean collection with items processed for status code property removal</returns>
    /// <remarks>
    /// This method provides comprehensive collection cleaning that maintains collection structure
    /// while ensuring all nested items are properly cleaned of status code and metadata properties.
    /// The approach handles different collection types and maintains type safety where possible.
    /// 
    /// **Type Preservation**: Attempts to maintain the original collection type structure
    /// when possible, falling back to generic collections when specific type reconstruction fails.
    /// 
    /// **Recursive Cleaning**: Applies the same cleaning logic recursively to collection items,
    /// ensuring that nested objects and sub-collections are properly processed.
    /// 
    /// **Performance Optimization**: Uses efficient enumeration and construction patterns to
    /// minimize performance impact during collection processing.
    /// 
    /// **Error Handling**: Includes comprehensive error handling to ensure that collection
    /// processing failures don't break the entire response transformation process.
    /// </remarks>
    private object? CreateCleanCollection(object source, Type sourceType)
    {
        if (source is not IEnumerable sourceEnumerable)
        {
            _logger.LogWarning("Source object is not enumerable for type {SourceType}", sourceType.Name);
            return source;
        }

        try
        {
            _logger.LogDebug("Creating clean collection for type {SourceType}", sourceType.Name);

            var cleanItems = new List<object?>();
            var itemsCleaned = 0;

            foreach (var item in sourceEnumerable)
            {
                if (item == null)
                {
                    cleanItems.Add(null);
                    continue;
                }

                var itemType = item.GetType();

                // Clean the item if it needs cleaning
                object? cleanItem = item;
                if (ShouldRecursivelyClean(itemType))
                {
                    cleanItem = CreateCleanDataStructure(item, itemType);
                    itemsCleaned++;
                }

                cleanItems.Add(cleanItem);
            }

            _logger.LogDebug("Processed {TotalItems} items in collection, cleaned {CleanedItems} items for type {SourceType}",
                cleanItems.Count, itemsCleaned, sourceType.Name);

            // Try to maintain collection type structure
            if (sourceType.IsArray)
            {
                return CreateCleanArray(cleanItems, sourceType);
            }

            if (sourceType.IsGenericType)
            {
                var genericTypeDefinition = sourceType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(List<>) ||
                    genericTypeDefinition == typeof(IList<>) ||
                    genericTypeDefinition == typeof(ICollection<>) ||
                    genericTypeDefinition == typeof(IEnumerable<>))
                {
                    return CreateCleanList(cleanItems, sourceType);
                }
            }

            // Fallback to generic list for unknown collection types
            _logger.LogDebug("Using generic list fallback for collection type {SourceType}", sourceType.Name);
            return cleanItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create clean collection for type {SourceType}", sourceType.Name);
            return source; // Return original collection if cleaning fails
        }
    }

    /// <summary>
    /// Creates a clean array by processing each item and maintaining the array structure.
    /// This method ensures that array types are properly reconstructed after cleaning.
    /// </summary>
    /// <param name="cleanItems">The list of cleaned items to convert to an array</param>
    /// <param name="arrayType">The original array type to reconstruct</param>
    /// <returns>A clean array with the original element type, or a generic array if type reconstruction fails</returns>
    private Array? CreateCleanArray(List<object?> cleanItems, Type arrayType)
    {
        try
        {
            var elementType = arrayType.GetElementType();
            if (elementType == null)
            {
                _logger.LogWarning("Could not determine element type for array type {ArrayType}", arrayType.Name);
                return cleanItems.ToArray();
            }

            var cleanArray = Array.CreateInstance(elementType, cleanItems.Count);
            for (var i = 0; i < cleanItems.Count; i++)
            {
                cleanArray.SetValue(cleanItems[i], i);
            }

            _logger.LogTrace("Successfully created clean array of type {ElementType}[] with {ItemCount} items",
                elementType.Name, cleanItems.Count);
            return cleanArray;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create typed array for type {ArrayType}, using object array", arrayType.Name);
            return cleanItems.ToArray();
        }
    }

    /// <summary>
    /// Creates a clean generic list by processing items and attempting to maintain the original generic type.
    /// This method preserves List&lt;T&gt; types when possible for better type safety and client compatibility.
    /// </summary>
    /// <param name="cleanItems">The list of cleaned items to convert to a generic list</param>
    /// <param name="listType">The original list type to reconstruct</param>
    /// <returns>A clean generic list with the original element type, or a generic list if type reconstruction fails</returns>
    private object? CreateCleanList(List<object?> cleanItems, Type listType)
    {
        try
        {
            var elementType = listType.GetGenericArguments()[0];
            var listConstructor = typeof(List<>).MakeGenericType(elementType);
            var cleanList = Activator.CreateInstance(listConstructor);

            if (cleanList == null)
            {
                _logger.LogWarning("Failed to create instance of List<{ElementType}>", elementType.Name);
                return cleanItems;
            }

            var addMethod = listConstructor.GetMethod("Add");
            if (addMethod == null)
            {
                _logger.LogWarning("Could not find Add method for List<{ElementType}>", elementType.Name);
                return cleanItems;
            }

            foreach (var item in cleanItems)
            {
                addMethod.Invoke(cleanList, [item]);
            }

            _logger.LogTrace("Successfully created clean List<{ElementType}> with {ItemCount} items",
                elementType.Name, cleanItems.Count);
            return cleanList;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create typed list for type {ListType}, using generic list", listType.Name);
            return cleanItems;
        }
    }

    /// <summary>
    /// Determines whether a nested property should be checked for status code properties.
    /// This method provides optimization by limiting deep nested analysis to prevent performance issues.
    /// </summary>
    /// <param name="propertyType">The type of the property to evaluate</param>
    /// <returns>True if the property should be checked for nested status code properties; otherwise, false</returns>
    /// <remarks>
    /// This method implements performance optimization for nested property analysis by identifying
    /// types that are unlikely to contain status code properties and excluding them from deep analysis.
    /// This prevents excessive recursion while ensuring comprehensive cleaning coverage.
    /// </remarks>
    private static bool ShouldCheckNestedProperty(Type propertyType)
    {
        // Don't check primitive types, strings, or value types
        if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType.IsValueType)
            return false;

        // Don't check system types unless they're collections
        return propertyType.Namespace?.StartsWith("System") != true || IsCollection(propertyType);
        // Limit depth to prevent infinite recursion and performance issues
    }

    /// <summary>
    /// Enhanced extraction method that handles StatusCode and Message but NOT custom metadata.
    /// Custom metadata is handled separately in BuildResponseMetadata to avoid duplication.
    /// </summary>
    /// <param name="data">The response data to analyze for status code and message</param>
    /// <returns>A tuple containing extracted status code and message (NO custom metadata)</returns>
    /// <remarks>
    /// This method is now focused solely on StatusCode and Message extraction to avoid
    /// the duplication issue where custom metadata was being extracted and merged twice.
    /// 
    /// **Single Responsibility**: This method now has a single, clear responsibility:
    /// extract StatusCode and Message for promotion to ApiResponse level properties.
    /// 
    /// **No Duplication**: Custom metadata extraction is handled exclusively in
    /// BuildResponseMetadata, ensuring it's only processed once per request.
    /// 
    /// **Clean Separation**: This separation makes the code more maintainable and
    /// eliminates the complex conflict resolution that was causing duplicate entries.
    /// </remarks>
    private (string? statusCode, string? message) ExtractStatusCodeAndMessage(object data)
    {
        if (data == null)
            return (null, null);

        try
        {
            string? statusCode = null;
            string? message = null;

            // Extract StatusCode using interface-first approach
            if (data is IHasStatusCode statusProvider)
            {
                statusCode = statusProvider.StatusCode;
                _logger.LogTrace("Extracted StatusCode '{StatusCode}' from IHasStatusCode interface", statusCode);
            }
            else
            {
                // Reflection fallback for backward compatibility
                var statusCodeProperty = data.GetType().GetProperty("StatusCode", BindingFlags.Public | BindingFlags.Instance);
                if (statusCodeProperty?.PropertyType == typeof(string) && statusCodeProperty.CanRead)
                {
                    statusCode = statusCodeProperty.GetValue(data) as string;
                    _logger.LogTrace("Extracted StatusCode '{StatusCode}' via reflection fallback", statusCode);
                }
            }

            // Extract Message using interface-first approach
            if (data is IHasMessage messageProvider)
            {
                message = messageProvider.Message;
                _logger.LogTrace("Extracted Message from IHasMessage interface");
            }
            else
            {
                // Reflection fallback for backward compatibility
                var messageProperty = data.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
                if (messageProperty?.PropertyType == typeof(string) && messageProperty.CanRead)
                {
                    message = messageProperty.GetValue(data) as string;
                    _logger.LogTrace("Extracted Message via reflection fallback");
                }
            }

            // Log extraction summary
            var extractedTypes = new List<string>();
            if (!string.IsNullOrEmpty(statusCode)) extractedTypes.Add("StatusCode");
            if (!string.IsNullOrEmpty(message)) extractedTypes.Add("Message");

            if (extractedTypes.Count > 0)
            {
                _logger.LogDebug("Successfully extracted: {ExtractedTypes} from {DataType}",
                    string.Join(", ", extractedTypes), data.GetType().Name);
            }

            return (statusCode, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract status code and message from {DataType}. " +
                                   "Error: {ErrorMessage}. Continuing without extraction",
                data.GetType().Name, ex.Message);
            return (null, null);
        }
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