using System.Collections;
using System.Diagnostics;
using System.Reflection;
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
    /// Creates the final wrapped response with automatic status code extraction and clean data transformation.
    /// This method extracts status codes from response data and promotes them to the ApiResponse level
    /// while creating clean, metadata-free data structures for the response body.
    /// </summary>
    /// <param name="originalData">The original response data from the controller</param>
    /// <param name="metadata">The metadata to include in the response</param>
    /// <returns>The wrapped response object with extracted status code and clean data</returns>
    /// <remarks>
    /// This enhanced method provides clean separation of concerns by:
    /// 1. Extracting status codes and messages from response DTOs
    /// 2. Creating clean data structures without metadata properties
    /// 3. Promoting status information to the ApiResponse level for consistent access
    /// 4. Maintaining backward compatibility for responses without status codes
    /// 
    /// The clean data approach ensures that business data remains focused on its primary purpose
    /// while status and metadata information is properly organized at the response wrapper level.
    /// </remarks>
    private object CreateWrappedResponse(object originalData, ResponseMetadata metadata)
    {
        var dataType = originalData.GetType();
        
        // Extract status code and message BEFORE transformation
        var (extractedStatusCode, extractedMessage) = ExtractStatusCodeAndMessage(originalData);
        
        // Transform paginated results (existing logic)
        var (transformedData, transformedDataType) = TransformPagedResult(originalData, dataType);
        
        // Create clean data structure by removing status code properties
        var cleanData = CreateCleanDataStructure(transformedData, transformedDataType);
        var cleanDataType = cleanData.GetType();

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
        metadataProperty?.SetValue(response, metadata);
        
        // Set extracted status code at ApiResponse level
        if (!string.IsNullOrEmpty(extractedStatusCode))
        {
            statusCodeProperty?.SetValue(response, extractedStatusCode);
            _logger.LogDebug("Promoted status code '{StatusCode}' to ApiResponse level", extractedStatusCode);
        }

        // Set extracted message if available
        if (!string.IsNullOrEmpty(extractedMessage))
        {
            messageProperty?.SetValue(response, extractedMessage);
            _logger.LogDebug("Promoted message to ApiResponse level");
        }

        _logger.LogDebug("Successfully created clean ApiResponse<{CleanDataType}> with promoted metadata", 
            cleanDataType.Name);

        return response;
    }

    /// <summary>
    /// Creates a clean data structure by removing status code and metadata properties from the original data.
    /// This method ensures that the response data contains only business-relevant information while
    /// status and metadata are handled at the appropriate response wrapper level.
    /// </summary>
    /// <param name="data">The original data that may contain status code properties</param>
    /// <param name="dataType">The type of the original data</param>
    /// <returns>A clean data structure without status code or metadata properties</returns>
    /// <remarks>
    /// The clean data creation process follows these principles:
    /// 1. **Single Source of Truth**: Status codes exist only at the ApiResponse level
    /// 2. **Clean Separation**: Business data is free from metadata concerns
    /// 3. **Backward Compatibility**: Maintains existing data structure for non-status responses
    /// 4. **Type Safety**: Preserves strong typing while creating clean structures
    /// 
    /// The method handles various scenarios:
    /// - Objects implementing IHasStatusCode get their status properties removed
    /// - Objects with Message properties used for status communication get cleaned
    /// - Other objects pass through unchanged to maintain compatibility
    /// 
    /// This approach ensures that API consumers receive clean, focused business data
    /// while still having access to status information at the appropriate response level.
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

            // Check if the object or any of its properties might have status code properties
            if (!HasStatusCodeProperties(data, dataType)) return data;
            var cleanData = CreateCleanCopy(data, dataType);
            if (cleanData == null) return data;
            _logger.LogDebug("Created clean copy of {DataType}, removed status code properties", dataType.Name);
            return cleanData;

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create clean data structure for type {DataType}. " +
                                   "Using original data structure", dataType.Name);
            return data;
        }
    }
    
    /// <summary>
    /// Determines if an object or its properties contain status code or metadata properties
    /// that should be cleaned from the response data.
    /// </summary>
    /// <param name="data">The object to examine</param>
    /// <param name="dataType">The type of the object</param>
    /// <returns>True if the object contains properties that should be cleaned; otherwise, false</returns>
    private bool HasStatusCodeProperties(object data, Type dataType)
    {
        // Quick check for IHasStatusCode implementation
        if (data is IHasStatusCode)
            return true;

        // Check if the type has any properties we want to exclude
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            if (IsStatusOrMetadataProperty(property.Name))
                return true;

            // For collections, check if items might have status properties
            if (!IsCollection(property.PropertyType)) continue;
            try
            {
                var value = property.GetValue(data);
                if (value != null && HasStatusPropertiesInCollection(value))
                    return true;
            }
            catch
            {
                // If we can't check, err on the safe side
                continue;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a collection contains items that might have status code properties.
    /// </summary>
    private bool HasStatusPropertiesInCollection(object collection)
    {
        if (collection is not IEnumerable enumerable)
            return false;

        foreach (var item in enumerable)
        {
            if (item == null) continue;
            
            var itemType = item.GetType();
            if (itemType.IsPrimitive || itemType == typeof(string) || itemType.IsValueType)
                continue;

            if (item is IHasStatusCode)
                return true;

            var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            if (properties.Any(p => IsStatusOrMetadataProperty(p.Name)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a clean copy of an object by copying all properties except status code and metadata properties.
    /// This method uses reflection to create a new instance with only business-relevant data.
    /// </summary>
    /// <param name="source">The source object to copy from</param>
    /// <param name="sourceType">The type of the source object</param>
    /// <returns>A new object with clean business data, or null if creation fails</returns>
    private object? CreateCleanCopy(object source, Type sourceType)
    {
        if (source == null)
            return null;

        // For primitive types or types that don't need cleaning, return as-is
        if (sourceType.IsPrimitive || sourceType == typeof(string) || sourceType.IsValueType)
        {
            return source;
        }

        // Handle collections
        if (IsCollection(sourceType))
        {
            return CreateCleanCollection(source, sourceType);
        }

        // Create new instance for regular objects
        var cleanInstance = Activator.CreateInstance(sourceType);
        if (cleanInstance == null) 
            return null;

        // Copy all properties except status code and message properties
        var properties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !IsStatusOrMetadataProperty(p.Name));

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(source);
                
                if (value != null)
                {
                    // Recursively clean nested objects
                    var propertyType = property.PropertyType;
                    
                    if (ShouldRecursivelyClean(propertyType))
                    {
                        value = CreateCleanDataStructure(value, propertyType);
                    }
                }
                
                property.SetValue(cleanInstance, value);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to copy property {PropertyName} while creating clean data structure", 
                    property.Name);
                // Continue with other properties
            }
        }

        return cleanInstance;
    }
    
    /// <summary>
    /// Creates a clean copy of a collection by recursively cleaning its items.
    /// </summary>
    private object? CreateCleanCollection(object source, Type sourceType)
    {
        if (source is not IEnumerable sourceEnumerable)
            return source;

        // Handle different collection types
        if (sourceType.IsArray)
        {
            return CreateCleanArray(sourceEnumerable, sourceType);
        }

        if (!sourceType.IsGenericType) return CreateCleanGenericCollection(sourceEnumerable);
        var genericTypeDefinition = sourceType.GetGenericTypeDefinition();
            
        if (genericTypeDefinition == typeof(List<>) || 
            genericTypeDefinition == typeof(IList<>) ||
            genericTypeDefinition == typeof(ICollection<>) ||
            genericTypeDefinition == typeof(IEnumerable<>))
        {
            return CreateCleanList(sourceEnumerable, sourceType);
        }

        // For other collection types, try to create a generic list
        return CreateCleanGenericCollection(sourceEnumerable);
    }

    /// <summary>
    /// Creates a clean array by cleaning each item.
    /// </summary>
    private Array? CreateCleanArray(IEnumerable source, Type arrayType)
    {
        var elementType = arrayType.GetElementType();
        if (elementType == null) return null;

        var items = source.Cast<object>().ToList();
        var cleanArray = Array.CreateInstance(elementType, items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item != null && ShouldRecursivelyClean(item.GetType()))
            {
                item = CreateCleanDataStructure(item, item.GetType());
            }
            cleanArray.SetValue(item, i);
        }

        return cleanArray;
    }

    /// <summary>
    /// Creates a clean list by cleaning each item.
    /// </summary>
    private object? CreateCleanList(IEnumerable source, Type listType)
    {
        var elementType = listType.GetGenericArguments()[0];
        var listConstructor = typeof(List<>).MakeGenericType(elementType);
        var cleanList = Activator.CreateInstance(listConstructor);
        
        if (cleanList == null) return null;

        var addMethod = listConstructor.GetMethod("Add");
        if (addMethod == null) return null;

        foreach (var item in source)
        {
            var cleanItem = item;
            if (item != null && ShouldRecursivelyClean(item.GetType()))
            {
                cleanItem = CreateCleanDataStructure(item, item.GetType());
            }
            addMethod.Invoke(cleanList, [cleanItem]);
        }

        return cleanList;
    }

    /// <summary>
    /// Creates a generic collection for unknown collection types.
    /// </summary>
    private List<object> CreateCleanGenericCollection(IEnumerable source)
    {
        var cleanList = new List<object>();

        foreach (var item in source)
        {
            var cleanItem = item;
            if (item != null && ShouldRecursivelyClean(item.GetType()))
            {
                cleanItem = CreateCleanDataStructure(item, item.GetType());
            }
            cleanList.Add(cleanItem);
        }

        return cleanList;
    }

    /// <summary>
    /// Determines whether a property name represents status code or metadata information
    /// that should be excluded from clean data structures.
    /// </summary>
    /// <param name="propertyName">The name of the property to check</param>
    /// <returns>True if the property should be excluded from clean data; otherwise, false</returns>
    private static bool IsStatusOrMetadataProperty(string propertyName)
    {
        // Properties that should be excluded from clean data
        var excludedProperties = new[] { "StatusCode", "Message", "Code", "ErrorCode", "Status" };
        return excludedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Determines if a type represents a collection that should be processed recursively.
    /// </summary>
    private static bool IsCollection(Type type)
    {
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Determines if a type should be recursively cleaned for status code properties.
    /// </summary>
    private static bool ShouldRecursivelyClean(Type type)
    {
        // Don't recursively clean primitive types, strings, or value types
        if (type.IsPrimitive || type == typeof(string) || type.IsValueType)
            return false;

        // Don't clean system types
        return type.Namespace?.StartsWith("System") != true || IsCollection(type);
    }

    /// <summary>
    /// Extracts status code and message from response data that implements IHasStatusCode interface.
    /// This method provides automatic promotion of status information from response DTOs to the
    /// top-level ApiResponse structure, enabling consistent client-side status handling.
    /// </summary>
    /// <param name="data">The response data to analyze for status code and message information</param>
    /// <returns>A tuple containing the extracted status code and message, or null values if not available</returns>
    /// <remarks>
    /// This method implements the Interface Segregation Principle by checking for specific
    /// interfaces that indicate the presence of status information. The extraction process
    /// is designed to be non-intrusive and fail-safe - if the data doesn't implement the
    /// expected interfaces, the method simply returns null values without affecting the
    /// overall response processing.
    /// 
    /// **Status Code Extraction**: The method first checks if the data implements IHasStatusCode
    /// and extracts the status code if available. This enables automatic promotion of workflow
    /// states and business process indicators to the API response level.
    /// 
    /// **Message Extraction**: The method also attempts to extract message information using
    /// reflection to look for common message property names. This provides additional context
    /// that can complement the status code information.
    /// 
    /// **Error Handling**: All reflection operations are wrapped in try-catch blocks to ensure
    /// that extraction failures don't break the overall response processing. If extraction fails,
    /// the method logs the issue and continues with null values.
    /// 
    /// **Performance Considerations**: The method uses cached reflection where possible and
    /// includes early exit strategies to minimize performance impact on responses that don't
    /// provide status information.
    /// </remarks>
    private (string? statusCode, string? message) ExtractStatusCodeAndMessage(object data)
    {
        if (data == null) return (null, null);

        try
        {
            string? statusCode = null;
            string? message = null;

            // Extract status code from IHasStatusCode implementation
            if (data is IHasStatusCode statusProvider)
            {
                statusCode = statusProvider.StatusCode;
            }
            else
            {
                // Try to extract using reflection for objects that don't implement IHasStatusCode
                var dataType = data.GetType();
                var statusCodeProperty = dataType.GetProperty("StatusCode", BindingFlags.Public | BindingFlags.Instance);
                
                if (statusCodeProperty?.PropertyType == typeof(string) && statusCodeProperty.CanRead)
                {
                    statusCode = statusCodeProperty.GetValue(data) as string;
                }
            }

            // Extract message property if available
            var messageProperty = data.GetType().GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
            
            if (messageProperty?.PropertyType == typeof(string) && messageProperty.CanRead)
            {
                message = messageProperty.GetValue(data) as string;
            }

            return (statusCode, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract status code from {DataType}", data.GetType().Name);
            return (null, null);
        }
    }

    /// <summary>
    /// Determines whether status code extraction should be performed for the given response data.
    /// This method provides an optimization point where extraction can be skipped for response types
    /// that are known not to contain status information.
    /// </summary>
    /// <param name="data">The response data to analyze</param>
    /// <returns>True if status code extraction should be attempted; otherwise, false</returns>
    /// <remarks>
    /// This method enables performance optimization by allowing early exit from status code
    /// extraction for response types that are known not to implement IHasStatusCode. The method
    /// can be extended to include additional optimization logic based on type analysis or
    /// configuration settings.
    /// 
    /// Current optimization strategies include:
    /// - Quick check for IHasStatusCode implementation
    /// - Type-based exclusions for known non-status types
    /// - Early exit for null or primitive types
    /// 
    /// This optimization is particularly valuable in high-throughput scenarios where the majority
    /// of responses don't require status code extraction.
    /// </remarks>
    private static bool ShouldExtractStatusCode(object data)
    {
        switch (data)
        {
            case null:
                return false;
            // Quick check if the object implements IHasStatusCode
            case IHasStatusCode:
                return true;
            default:
            {
                // Skip extraction for primitive types and known system types
                var dataType = data.GetType();
                return !dataType.IsPrimitive && dataType != typeof(string) && dataType != typeof(DateTime);
            }
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