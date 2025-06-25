using System.Diagnostics;
using System.Reflection;
using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.Models.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FS.AspNetCore.ResponseWrapper.Filters;

public class ApiResponseWrapperFilter(
    ILogger<ApiResponseWrapperFilter> logger,
    Func<DateTime>? getCurrentTime)
    : IAsyncActionFilter
{
    private readonly Func<DateTime> _getCurrentTime = getCurrentTime ?? (() => DateTime.UtcNow);
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Pre-execution: Start timing and setup tracking
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        var correlationId = GetCorrelationId(context.HttpContext);
        
        var now = _getCurrentTime!.Invoke();

        // Store timing info in HttpContext for access in post-execution
        context.HttpContext.Items["RequestStartTime"] = now;
        context.HttpContext.Items["RequestId"] = requestId;
        context.HttpContext.Items["CorrelationId"] = correlationId;
        context.HttpContext.Items["Stopwatch"] = stopwatch;

        logger.LogDebug("Request started: {RequestId} - {Method} {Path}",
            requestId, context.HttpContext.Request.Method, context.HttpContext.Request.Path);

        // Execute the action
        var executedContext = await next();

        // Post-execution: Wrap response and inject metadata
        stopwatch.Stop();

        if (ShouldWrapResponse(executedContext))
        {
            await WrapResponse(executedContext, stopwatch.ElapsedMilliseconds);
        }

        logger.LogDebug("Request completed: {RequestId} in {ElapsedMs}ms",
            requestId, stopwatch.ElapsedMilliseconds);
    }
    
    private static bool ShouldWrapResponse(ActionExecutedContext context)
    {
        if (context.Exception != null) return false;
        switch (context.Result)
        {
            case FileResult:
            case RedirectResult or RedirectToActionResult:
                return false;
        }
        if (IsAlreadyWrapped(context.Result)) return false;
        return context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null;
    }
    
    private static bool IsAlreadyWrapped(IActionResult? result)
    {
        if (result is ObjectResult objectResult)
        {
            return objectResult.Value?.GetType().IsGenericType == true &&
                   objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>);
        }

        return false;
    }
    
    private async Task WrapResponse(ActionExecutedContext context, long executionTimeMs)
    {
        if (context.Result is ObjectResult { Value: not null } objectResult)
        {
            var originalData = objectResult.Value;
            var metadata = await BuildMetadata(context, executionTimeMs, originalData);
            var wrappedResponse = CreateWrappedResponse(originalData, metadata);

            switch (context.Result)
            {
                case CreatedAtActionResult createdResult:
                    context.Result = new CreatedAtActionResult(
                        createdResult.ActionName,
                        createdResult.ControllerName,
                        createdResult.RouteValues,
                        wrappedResponse)
                    {
                        StatusCode = 201, // Created status code
                        ContentTypes = { "application/json" },
                        DeclaredType = wrappedResponse.GetType()
                    };
                    break;
                case ObjectResult objectResultResponse:
                    context.Result = new ObjectResult(wrappedResponse)
                    {
                        StatusCode = objectResult.StatusCode,
                        ContentTypes = objectResult.ContentTypes,
                        DeclaredType = wrappedResponse.GetType()
                    };
                    break;
                default:
                    logger.LogWarning("Unhandled result type: {ResultType}", context.Result.GetType().Name);
                    break;
            }
           
        }
    }
    
    private Task<ResponseMetadata> BuildMetadata(
        ActionExecutedContext context,
        long executionTimeMs,
        object originalData)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;
        
        var now = _getCurrentTime!.Invoke();

        var metadata = new ResponseMetadata
        {
            RequestId = httpContext.Items["RequestId"]?.ToString() ?? "",
            CorrelationId = httpContext.Items["CorrelationId"]?.ToString(),
            Timestamp = (DateTime)(httpContext.Items["RequestStartTime"] ?? now),
            ExecutionTimeMs = executionTimeMs,
            Path = request.Path.Value ?? "",
            Method = request.Method,
            Version = GetApiVersion(httpContext),
            Pagination = ExtractPaginationMetadata(originalData),
            Query = ExtractQueryMetadata(httpContext),
            Additional = ExtractAdditionalMetadata(httpContext)
        };

        return Task.FromResult(metadata);
    }
    
    private static PaginationMetadata? ExtractPaginationMetadata(object data)
    {
        if (data is IPagedResult pagedResult)
        {
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

        return null;
    }
    
    private static QueryMetadata? ExtractQueryMetadata(HttpContext httpContext)
    {
        if (httpContext.Items["QueryStats"] is Dictionary<string, object> queryStats)
        {
            return new QueryMetadata
            {
                DatabaseQueriesCount = (int)(queryStats.GetValueOrDefault("QueriesCount", 0)),
                DatabaseExecutionTimeMs = (long)(queryStats.GetValueOrDefault("ExecutionTimeMs", 0L)),
                CacheHits = (int)(queryStats.GetValueOrDefault("CacheHits", 0)),
                CacheMisses = (int)(queryStats.GetValueOrDefault("CacheMisses", 0)),
                ExecutedQueries = queryStats.GetValueOrDefault("ExecutedQueries") as string[]
            };
        }

        return null;
    }
    
    private static Dictionary<string, object>? ExtractAdditionalMetadata(HttpContext httpContext)
    {
        var additional = new Dictionary<string, object>();

        // Request size
        if (httpContext.Request.ContentLength.HasValue)
        {
            additional["RequestSizeBytes"] = httpContext.Request.ContentLength.Value;
        }

        // User agent
        var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            additional["UserAgent"] = userAgent;
        }

        // Client IP
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            additional["ClientIP"] = clientIp;
        }

        // Custom headers
        var customHeaders = httpContext.Request.Headers
            .Where(h => h.Key.StartsWith("X-Custom-"))
            .ToDictionary(h => h.Key, object (h) => h.Value.ToString());

        foreach (var header in customHeaders)
        {
            additional[header.Key] = header.Value;
        }

        return additional.Count != 0 ? additional : null;
    }
    
    private (object transformedData, Type transformedDataType) TransformPagedResult(object originalData, Type dataType)
    {
        if(!IsPagedResult(originalData))
            return (originalData, dataType);
        
        // Extract items from PagedResult<T>
        var itemsProperty = dataType.GetProperty("Items");
        if (itemsProperty == null)
        {
            logger.LogWarning("Items property not found on type {DataType}", dataType.Name);
            return (originalData, dataType);
        }
        
        var items = itemsProperty.GetValue(originalData);
        if (items == null)
        {
            logger.LogWarning("Items collection is null for type {DataType}", dataType.Name);
            return (originalData, dataType);
        }
        
        // Determine item type from PagedResult<T>
        var itemType = GetItemTypeFromPagedResult(dataType);
        if (itemType == null)
        {
            logger.LogWarning("Could not determine item type from {DataType}", dataType.Name);
            return (originalData, dataType);
        }
        
        // Create CleanPagedResult<T> - NO pagination fields
        var cleanResultType = typeof(CleanPagedResult<>).MakeGenericType(itemType);
        var cleanResult = Activator.CreateInstance(cleanResultType);
        
        if (cleanResult == null)
        {
            logger.LogError("Failed to create clean result type for {ItemType}", itemType.Name);
            return (originalData, dataType);
        }
        
        // Set only Items property - pagination bilgileri EXCLUDE
        var cleanItemsProperty = cleanResultType.GetProperty("Items");
        cleanItemsProperty?.SetValue(cleanResult, items);
        
        logger.LogInformation("Successfully transformed PagedResult<{ItemType}> to CleanPagedResult<{ItemType}>", 
            itemType.Name, itemType.Name);
        
        return (cleanResult, cleanResultType);
    }
    
    private object CreateWrappedResponse(object originalData, ResponseMetadata metadata)
    {
        var dataType = originalData.GetType();
        var (transformedData, transformedDataType) = TransformPagedResult(originalData, dataType);
        
        var responseType = typeof(ApiResponse<>).MakeGenericType(transformedDataType);

        var response = Activator.CreateInstance(responseType);
        // Set properties with type-safe assignments
        var dataProperty = responseType.GetProperty("Data");
        var successProperty = responseType.GetProperty("Success");
        var metadataProperty = responseType.GetProperty("Metadata");

        if (dataProperty == null || successProperty == null || metadataProperty == null)
        {
            throw new InvalidOperationException($"ApiResponse<{transformedDataType.Name}> missing required properties");
        }

        // Type-safe property assignment
        dataProperty.SetValue(response, transformedData); // No type mismatch now!
        successProperty.SetValue(response, true);
        metadataProperty.SetValue(response, metadata);

        logger.LogDebug("Successfully created ApiResponse<{TransformedType}>", transformedDataType.Name);

        return response!;
    }
    
    private static string GetCorrelationId(HttpContext? httpContext)
    {
        return httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? httpContext?.TraceIdentifier
               ?? Guid.NewGuid().ToString();
    }
    
    private static string GetApiVersion(HttpContext httpContext)
    {
        return httpContext.Request.Headers["X-API-Version"].FirstOrDefault()
               ?? httpContext.Request.Query["version"].FirstOrDefault()
               ?? "1.0";
    }
    
    private static bool IsPagedResult(object data)
    {
        return data is IPagedResult;
    }
    
    private static Type? GetItemTypeFromPagedResult(Type pagedResultType)
    {
        if (!pagedResultType.IsGenericType) return null;
        var genericArgs = pagedResultType.GetGenericArguments();
        return genericArgs.Length > 0 ? genericArgs[0] : // T from PagedResult<T>
            null;
    }
}