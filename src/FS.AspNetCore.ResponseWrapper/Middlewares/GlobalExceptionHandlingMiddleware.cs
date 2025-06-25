using System.Diagnostics;
using System.Net;
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FS.AspNetCore.ResponseWrapper.Middlewares;

public class GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger, Func<DateTime>? getCurrentTime) : IMiddleware
{
    private readonly Func<DateTime> _getCurrentTime = getCurrentTime ?? (() => DateTime.UtcNow);
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Pre-execution: Start timing and setup tracking
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        var correlationId = GetCorrelationId(context);
        
        var now = _getCurrentTime!.Invoke();

        // Store timing info in HttpContext for access in post-execution
        context.Items["RequestStartTime"] = now;
        context.Items["RequestId"] = requestId;
        context.Items["CorrelationId"] = correlationId;
        context.Items["Stopwatch"] = stopwatch;

        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await HandleExceptionAsync(context, exception, "Validation hatası oluştu", exception.Errors.Values.SelectMany(x => x).ToList(), HttpStatusCode.BadRequest, stopwatch.ElapsedMilliseconds, false);
        }
        catch (NotFoundException exception)
        {
            await HandleExceptionAsync(context, exception, "Kayıt bulunamadı", [exception.Message], HttpStatusCode.NotFound, stopwatch.ElapsedMilliseconds, false);
        }
        catch (ForbiddenAccessException exception)
        {
            await HandleExceptionAsync(context, exception, "Yetkiniz yok", [exception.Message], HttpStatusCode.Forbidden, stopwatch.ElapsedMilliseconds, false);
        }
        catch (UnauthorizedAccessException exception)
        {
            await HandleExceptionAsync(context, exception, "Yetkiniz yok", [exception.Message], HttpStatusCode.Unauthorized, stopwatch.ElapsedMilliseconds, false);
        }
        catch (BusinessException exception)
        {
            await HandleExceptionAsync(context, exception, "İş kuralı hatası oluştu", [exception.Message], HttpStatusCode.BadRequest, stopwatch.ElapsedMilliseconds, false);
        }
        catch (ApplicationException exception)
        {
            await HandleExceptionAsync(context, exception, "Uygulama hatası oluştu", [exception.Message], HttpStatusCode.InternalServerError, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, "Beklenmeyen bir hata oluştu", [exception.Message], HttpStatusCode.InternalServerError, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string message, List<string> errors, HttpStatusCode statusCode, long executionTimeMs, bool logError = true)
    {
        if (logError)
            logger.LogError(exception, message);

        var response = ApiResponse<object>.ErrorResult(errors);
        response.Message = message;
        response.Metadata = await BuildMetadata(context, executionTimeMs);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
    
    private static QueryMetadata? ExtractQueryMetadata(HttpContext httpContext)
    {
        // EF Core interceptor'lardan query stats alınabilir
        var queryStats = httpContext.Items["QueryStats"] as Dictionary<string, object>;

        if (queryStats != null)
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
    
    private Task<ResponseMetadata> BuildMetadata(
        HttpContext httpContext,
        long executionTimeMs)
    {
        var request = httpContext.Request;
        
        var now = _getCurrentTime!.Invoke();

        var metadata = new ResponseMetadata
        {
            // Basic request information
            RequestId = httpContext.Items["RequestId"]?.ToString() ?? "",
            CorrelationId = httpContext.Items["CorrelationId"]?.ToString(),
            Timestamp = (DateTime)(httpContext.Items["RequestStartTime"] ?? now),
            ExecutionTimeMs = executionTimeMs,
            Path = request.Path.Value ?? "",
            Method = request.Method,

            // API version (header'dan veya config'den)
            Version = GetApiVersion(httpContext),

            // Query metadata (eğer varsa)
            Query = ExtractQueryMetadata(httpContext),

            // Additional metadata
            Additional = ExtractAdditionalMetadata(httpContext)
        };

        return Task.FromResult(metadata);
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
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
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
}