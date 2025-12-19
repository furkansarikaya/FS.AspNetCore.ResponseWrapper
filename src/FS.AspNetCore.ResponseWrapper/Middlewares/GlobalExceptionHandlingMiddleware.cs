using System.Diagnostics;
using System.Net;
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FS.AspNetCore.ResponseWrapper.Middlewares;

/// <summary>
/// Middleware for global exception handling that provides consistent error responses across the application.
/// This middleware catches unhandled exceptions and converts them into structured API responses with appropriate
/// HTTP status codes and comprehensive error metadata. It serves as the last line of defense for error handling
/// and ensures that no exception leaves the application in an unstructured format.
/// </summary>
/// <remarks>
/// The middleware operates as the outermost layer of the request pipeline for exception handling:
/// 1. Pre-execution: Sets up error tracking and timing for diagnostic purposes
/// 2. Exception handling: Catches and categorizes exceptions based on type
/// 3. Response generation: Creates consistent error responses with metadata
/// 
/// This middleware works in conjunction with ApiResponseWrapperFilter to provide comprehensive
/// request/response lifecycle management. While the filter handles successful responses,
/// the middleware ensures errors are handled with the same level of consistency and metadata richness.
/// 
/// The middleware supports complete customization of error messages through the ErrorMessageConfiguration,
/// allowing developers to provide domain-specific, localized, or brand-appropriate error messaging
/// while maintaining the technical robustness of the error handling system.
/// </remarks>
public class GlobalExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly Func<DateTime> _getCurrentTime;
    private readonly ResponseWrapperOptions _options;
    private readonly ErrorMessageConfiguration _errorMessages;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionHandlingMiddleware with required dependencies.
    /// </summary>
    /// <param name="logger">Logger instance for error logging and diagnostic information</param>
    /// <param name="getCurrentTime">Function to provide current DateTime, ensuring consistency with filter timing</param>
    /// <param name="options">Configuration options that control middleware behavior and error response formatting</param>
    /// <param name="errorMessages">Configuration for customizing user-facing error messages, enabling localization and branding</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
    /// <remarks>
    /// The middleware requires all dependencies to be properly configured to ensure robust error handling.
    /// The error message configuration allows complete control over user-facing messaging while maintaining
    /// the technical aspects of error processing and metadata generation.
    /// 
    /// This constructor follows the Explicit Dependencies Principle, making all required dependencies
    /// visible and ensuring that the middleware cannot be instantiated without proper configuration.
    /// </remarks>
    public GlobalExceptionHandlingMiddleware(
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        Func<DateTime> getCurrentTime,
        ResponseWrapperOptions options,
        ErrorMessageConfiguration errorMessages)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _getCurrentTime = getCurrentTime ?? throw new ArgumentNullException(nameof(getCurrentTime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _errorMessages = errorMessages ?? throw new ArgumentNullException(nameof(errorMessages));
    }

    /// <summary>
    /// Processes HTTP requests and handles any unhandled exceptions that occur during request processing.
    /// This method implements the core exception handling logic and ensures all errors are properly logged
    /// and converted to structured responses using configured error messages.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="next">The next middleware delegate in the pipeline</param>
    /// <returns>A task representing the asynchronous middleware operation</returns>
    /// <remarks>
    /// The middleware implements a comprehensive exception handling strategy that:
    /// 1. Categorizes exceptions by type for appropriate HTTP status code mapping
    /// 2. Uses configured error messages for consistent user communication
    /// 3. Logs exceptions at appropriate levels based on their expected nature
    /// 4. Generates rich metadata for debugging and monitoring purposes
    /// 5. Ensures no exception leaves the application in an unhandled state
    /// 
    /// The exception categorization follows industry best practices:
    /// - Validation errors (400 Bad Request) for input validation failures
    /// - Not found errors (404 Not Found) for missing resources
    /// - Authorization errors (401/403) for authentication and permission issues
    /// - Business logic errors (400 Bad Request) for domain rule violations
    /// - Application errors (500) for unexpected application failures
    /// - System errors (500) for completely unexpected failures
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Check if this request should be excluded from error wrapping
        if (ShouldExcludeRequest(context))
        {
            await next(context);
            return;
        }

        // Initialize request tracking for error scenarios
        var errorTrackingData = InitializeErrorTracking(context);

        try
        {
            // Execute the next middleware in the pipeline
            await next(context);
        }
        catch (ValidationException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetValidationErrorMessage(),
                exception.Errors.Values.SelectMany(x => x).ToList(),
                HttpStatusCode.BadRequest,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (NotFoundException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetNotFoundErrorMessage(),
                [exception.Message],
                HttpStatusCode.NotFound,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (ConflictException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                exception.Message, // ConflictException messages are usually specific enough
                [exception.Message],
                HttpStatusCode.Conflict,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (UnauthorizedException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetUnauthorizedAccessMessage(),
                [exception.Message],
                HttpStatusCode.Unauthorized,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (ForbiddenAccessException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetForbiddenAccessMessage(),
                [exception.Message],
                HttpStatusCode.Forbidden,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (BadRequestException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                exception.Message,
                [exception.Message],
                HttpStatusCode.BadRequest,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (Exceptions.TimeoutException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                "The request timed out. Please try again later.",
                [exception.Message],
                HttpStatusCode.RequestTimeout,
                errorTrackingData,
                shouldLogAsError: true); // Timeouts might indicate system issues
        }
        catch (TooManyRequestsException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                "Too many requests. Please slow down and try again later.",
                [exception.Message],
                (HttpStatusCode)429, // HTTP 429 Too Many Requests
                errorTrackingData,
                shouldLogAsError: false); // Rate limiting is expected behavior
        }
        catch (ServiceUnavailableException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                "Service temporarily unavailable. Please try again later.",
                [exception.Message],
                HttpStatusCode.ServiceUnavailable,
                errorTrackingData,
                shouldLogAsError: true); // Service unavailability should be logged
        }
        catch (CustomHttpStatusException exception)
        {
            // Handle custom HTTP status codes
            await HandleCustomHttpStatusExceptionAsync(
                context,
                exception,
                errorTrackingData);
        }
        catch (BusinessException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetBusinessRuleViolationMessage(),
                [exception.Message],
                HttpStatusCode.BadRequest,
                errorTrackingData,
                shouldLogAsError: false); // Business rule violations are expected
        }
        catch (UnauthorizedAccessException exception)
        {
            // Handle system UnauthorizedAccessException (different from our UnauthorizedException)
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetUnauthorizedAccessMessage(),
                [exception.Message],
                HttpStatusCode.Unauthorized,
                errorTrackingData,
                shouldLogAsError: false);
        }
        catch (ApplicationException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetApplicationErrorMessage(),
                [exception.Message],
                HttpStatusCode.InternalServerError,
                errorTrackingData,
                shouldLogAsError: true); // Application errors should be logged
        }
        catch (Exception exception)
        {
            var isExposeMessage =
                exception.Data.Contains("ExposeMessage") &&
                exception.Data["ExposeMessage"] is true;

            var message = isExposeMessage
                ? exception.Message
                : _errorMessages.GetUnexpectedErrorMessage();

            var errors = isExposeMessage
                ? new List<string> { exception.Message }
                : new List<string> { GetSafeErrorMessage(exception) };

            var statusCode = isExposeMessage
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.InternalServerError;

            await HandleExceptionAsync(
                context,
                exception,
                message,
                errors,
                statusCode,
                errorTrackingData,
                shouldLogAsError: !isExposeMessage);
        }
        finally
        {
            // Clean up tracking resources
            if (_options.EnableExecutionTimeTracking)
            {
                errorTrackingData.Stopwatch?.Stop();
            }
        }
    }

    
    
    /// <summary>
    /// Determines whether a request should be excluded from error wrapping based on configuration.
    /// This method provides early exit for requests that should handle errors in their own way.
    /// </summary>
    /// <param name="context">The HTTP context containing request information</param>
    /// <returns>True if the request should be excluded from error wrapping; otherwise, false</returns>
    /// <remarks>
    /// The exclusion logic allows fine-grained control over which requests receive error wrapping.
    /// This is particularly useful for endpoints that need to maintain specific error response formats
    /// for integration purposes, such as health checks, metrics endpoints, or legacy API compatibility.
    /// 
    /// The path-based exclusion uses case-insensitive prefix matching for simplicity and performance.
    /// More complex exclusion patterns can be implemented by extending this method or using the
    /// ExcludedTypes configuration for type-based exclusions.
    /// </remarks>
    private bool ShouldExcludeRequest(HttpContext context)
    {
        // Don't wrap errors if error wrapping is disabled
        if (!_options.WrapErrorResponses)
            return true;

        var requestPath = context.Request.Path.Value ?? string.Empty;

        // Check if path is in exclusion list
        return _options.ExcludedPaths.Any(excludedPath =>
            requestPath.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initializes error tracking data for diagnostic and metadata purposes.
    /// This method sets up the foundation for error response metadata generation.
    /// </summary>
    /// <param name="context">The HTTP context to store tracking information</param>
    /// <returns>Error tracking data structure containing timing and identification information</returns>
    /// <remarks>
    /// The error tracking data serves multiple purposes:
    /// 1. Unique request identification for correlation across logs and responses
    /// 2. Timing information for performance analysis of error scenarios
    /// 3. Distributed tracing support through correlation IDs
    /// 4. Consistent metadata structure between successful and error responses
    /// 
    /// The tracking data is stored in HttpContext.Items to ensure it's available throughout
    /// the request processing pipeline and can be accessed by other components if needed.
    /// </remarks>
    private ErrorTrackingData InitializeErrorTracking(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        var correlationId = _options.EnableCorrelationId ? GetCorrelationId(context) : null;
        var timestamp = _getCurrentTime();
        var stopwatch = _options.EnableExecutionTimeTracking ? Stopwatch.StartNew() : null;

        var trackingData = new ErrorTrackingData
        {
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            Stopwatch = stopwatch
        };

        // Store tracking data in HttpContext for consistency with filter behavior
        context.Items["ErrorTrackingData"] = trackingData;

        return trackingData;
    }
    
    /// <summary>
    /// Handles CustomHttpStatusException with support for custom HTTP status codes while maintaining
    /// the structured error response format and error code extraction functionality.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="exception">The CustomHttpStatusException containing custom status code information</param>
    /// <param name="trackingData">Error tracking data for metadata generation</param>
    /// <returns>A task representing the asynchronous custom exception handling operation</returns>
    /// <remarks>
    /// This specialized handling method enables applications to use any HTTP status code while
    /// maintaining consistency with the ResponseWrapper error handling system. The method preserves
    /// all standard ResponseWrapper functionality including error codes, metadata generation,
    /// and structured error responses while allowing complete control over HTTP semantics.
    /// 
    /// **Custom Status Code Support**: The method extracts the custom HTTP status code from the
    /// exception and applies it to the response, enabling precise control over HTTP semantics
    /// for specialized error scenarios or integration requirements.
    /// 
    /// **Consistent Error Structure**: Despite the flexible status code, the method maintains
    /// the same structured error response format as other exception handlers, ensuring consistent
    /// client-side error handling regardless of the specific HTTP status code used.
    /// 
    /// **Error Code Preservation**: The method preserves and promotes error codes from the
    /// exception to the ApiResponse level, maintaining rich error identification capabilities
    /// even with custom HTTP status codes.
    /// </remarks>
    private async Task HandleCustomHttpStatusExceptionAsync(
        HttpContext context,
        CustomHttpStatusException exception,
        ErrorTrackingData trackingData)
    {
        // Determine logging level based on HTTP status code
        var shouldLogAsError = exception.HttpStatusCode >= 500;
        
        // Use custom HTTP status code from exception
        var statusCode = (HttpStatusCode)exception.HttpStatusCode;

        // Log the exception appropriately based on status code
        if (shouldLogAsError)
        {
            _logger.LogError(exception, "Custom HTTP status error {StatusCode} processing request {RequestId}: {Message}",
                exception.HttpStatusCode, trackingData.RequestId, exception.Message);
        }
        else
        {
            _logger.LogInformation("Request {RequestId} resulted in custom status {StatusCode}: {Message}",
                trackingData.RequestId, exception.HttpStatusCode, exception.Message);
        }

        // Create the error response with metadata
        var errorResponse = ApiResponse<object>.ErrorResult([exception.Message]);
        errorResponse.Message = exception.Message;
        
        // Extract and set error code from the exception
        var extractedErrorCode = ExtractErrorCode(exception);
        if (!string.IsNullOrEmpty(extractedErrorCode))
        {
            errorResponse.StatusCode = extractedErrorCode;
            _logger.LogDebug("Extracted error code '{ErrorCode}' from CustomHttpStatusException", 
                extractedErrorCode);
        }

        // Build and attach metadata
        errorResponse.Metadata = await BuildErrorMetadata(context, trackingData);

        // Set response properties with custom status code
        context.Response.StatusCode = exception.HttpStatusCode;
        context.Response.ContentType = "application/json";

        try
        {
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
        catch (Exception writeException)
        {
            _logger.LogError(writeException, "Failed to write custom status error response for request {RequestId}",
                trackingData.RequestId);
        }
    }

    /// <summary>
    /// Enhanced exception handling with automatic error code extraction and promotion for all exception types.
    /// This method provides comprehensive error code support while maintaining backward compatibility
    /// with existing error handling patterns.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="userMessage">User-friendly error message from configuration</param>
    /// <param name="errors">List of specific error messages</param>
    /// <param name="statusCode">HTTP status code to return</param>
    /// <param name="trackingData">Request tracking data for metadata generation</param>
    /// <param name="shouldLogAsError">Whether this exception should be logged as an error level</param>
    /// <returns>A task representing the asynchronous exception handling operation</returns>
    /// <remarks>
    /// This enhanced method provides universal error code extraction that works with all
    /// ApplicationExceptionBase-derived exceptions as well as system exceptions. The method
    /// maintains backward compatibility while adding sophisticated error identification
    /// capabilities that enable rich client-side error handling.
    /// 
    /// **Universal Error Code Support**: The method automatically extracts error codes from
    /// any exception that derives from ApplicationExceptionBase, ensuring consistent error
    /// identification across all custom exception types without requiring specific handling code.
    /// 
    /// **Fallback Error Codes**: For system exceptions and exceptions without explicit codes,
    /// the method provides appropriate fallback error codes based on exception type, ensuring
    /// that all error responses include machine-readable error identification.
    /// 
    /// **Metadata Consistency**: All error responses include the same rich metadata structure
    /// regardless of exception type, providing consistent debugging and monitoring capabilities
    /// across the entire application.
    /// </remarks>
    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string userMessage,
        List<string> errors,
        HttpStatusCode statusCode,
        ErrorTrackingData trackingData,
        bool shouldLogAsError = true)
    {
        // Log the exception based on its severity and type
        if (shouldLogAsError)
        {
            _logger.LogError(exception, "Error processing request {RequestId}: {Message}",
                trackingData.RequestId, userMessage);
        }
        else
        {
            _logger.LogInformation("Request {RequestId} resulted in expected error: {Message}",
                trackingData.RequestId, userMessage);
        }

        // Create the error response with metadata
        var errorResponse = ApiResponse<object>.ErrorResult(errors);
        errorResponse.Message = userMessage;
        
        // Extract and set error code from any exception type
        var extractedErrorCode = ExtractErrorCode(exception);
        if (!string.IsNullOrEmpty(extractedErrorCode))
        {
            errorResponse.StatusCode = extractedErrorCode;
            _logger.LogDebug("Extracted error code '{ErrorCode}' from {ExceptionType}", 
                extractedErrorCode, exception.GetType().Name);
        }

        // Build and attach metadata if enabled
        errorResponse.Metadata = await BuildErrorMetadata(context, trackingData);

        // Set response properties and send the response
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        try
        {
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
        catch (Exception writeException)
        {
            // If we can't write the structured response, log the issue but don't throw
            // This prevents infinite exception loops
            _logger.LogError(writeException, "Failed to write error response for request {RequestId}",
                trackingData.RequestId);
        }
    }
    
    /// <summary>
    /// Enhanced error code extraction that supports all ApplicationExceptionBase-derived exceptions
    /// and provides appropriate fallback codes for system exceptions. This method enables universal
    /// error code support across the entire exception hierarchy.
    /// </summary>
    /// <param name="exception">The exception to extract error code from</param>
    /// <returns>The extracted error code, or a fallback code based on exception type</returns>
    /// <remarks>
    /// This enhanced extraction method provides comprehensive error code support that works with:
    /// 
    /// **Custom Application Exceptions**: All exceptions derived from ApplicationExceptionBase
    /// automatically provide their configured error codes, enabling rich error identification
    /// across the entire custom exception hierarchy.
    /// 
    /// **System Exception Mapping**: Common system exceptions are mapped to appropriate error
    /// codes that provide meaningful identification for client-side error handling while
    /// maintaining security by not exposing internal system details.
    /// 
    /// **Fallback Strategy**: The method implements a comprehensive fallback strategy that
    /// ensures all exceptions result in some form of error code, preventing scenarios where
    /// error responses lack machine-readable identification.
    /// 
    /// **Type-Safe Processing**: The method uses pattern matching and type checking to ensure
    /// safe error code extraction that doesn't fail even with unexpected exception types
    /// or malformed exception instances.
    /// 
    /// **Extensibility**: The extraction logic can be easily extended to support additional
    /// exception types or custom error code mapping strategies without affecting existing
    /// error handling behavior.
    /// </remarks>
    private string? ExtractErrorCode(Exception exception)
    {
        try
        {
            // First, check if it's one of our custom exceptions with error codes
            if (exception is ApplicationExceptionBase appException && !string.IsNullOrEmpty(appException.Code))
            {
                return appException.Code;
            }

            if (exception.Data.Contains("ErrorCode") &&
                exception.Data["ErrorCode"] is string dataErrorCode &&
                !string.IsNullOrEmpty(dataErrorCode))
            {
                return dataErrorCode;
            }

            // Provide fallback error codes for system exceptions and specific exception types
            return exception switch
            {
                ValidationException => "VALIDATION_ERROR",
                NotFoundException => "NOT_FOUND", 
                ConflictException => "CONFLICT",
                UnauthorizedException => "UNAUTHORIZED",
                ForbiddenAccessException => "FORBIDDEN",
                BadRequestException => "BAD_REQUEST",
                BusinessException => "BUSINESS_RULE_VIOLATION",
                Exceptions.TimeoutException => "TIMEOUT",
                TooManyRequestsException => "TOO_MANY_REQUESTS",
                ServiceUnavailableException => "SERVICE_UNAVAILABLE",
                CustomHttpStatusException customEx => customEx.Code,
                
                // System exception mappings
                UnauthorizedAccessException => "UNAUTHORIZED",
                ArgumentException => "INVALID_ARGUMENT",
                InvalidOperationException => "INVALID_OPERATION",
                NotSupportedException => "NOT_SUPPORTED",
                System.TimeoutException => "TIMEOUT",
                ApplicationException => "APPLICATION_ERROR",
                
                // Generic fallback for any other exception
                _ => "INTERNAL_ERROR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract error code from exception {ExceptionType}", 
                exception.GetType().Name);
            return "INTERNAL_ERROR"; // Safe fallback
        }
    }

    /// <summary>
    /// Builds comprehensive error metadata from request context and tracking data.
    /// This method creates consistent metadata for error responses, mirroring the metadata
    /// structure used by successful responses in the filter.
    /// </summary>
    /// <param name="context">The HTTP context containing request information</param>
    /// <param name="trackingData">Error tracking data collected during processing</param>
    /// <returns>A task containing the complete error response metadata</returns>
    /// <remarks>
    /// The error metadata provides valuable information for debugging, monitoring, and analytics:
    /// 1. Request identification and timing for performance analysis
    /// 2. API version and routing information for endpoint analytics
    /// 3. Database query statistics for performance optimization
    /// 4. Client information for debugging and security analysis
    /// 5. Correlation data for distributed tracing and log correlation
    /// 
    /// The metadata structure is identical to successful responses, ensuring consistency
    /// in monitoring tools and client applications that process API responses.
    /// </remarks>
    private Task<ResponseMetadata> BuildErrorMetadata(HttpContext context, ErrorTrackingData trackingData)
    {
        var request = context.Request;

        var metadata = new ResponseMetadata
        {
            RequestId = trackingData.RequestId,
            Timestamp = trackingData.Timestamp,
            Path = request.Path.Value ?? "",
            Method = request.Method,
            Version = GetApiVersion(context)
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

        if (_options.EnableQueryStatistics)
        {
            metadata.Query = ExtractQueryMetadata(context);
        }

        // Always extract additional metadata for error scenarios (helpful for debugging)
        metadata.Additional = ExtractAdditionalMetadata(context);

        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Extracts database query statistics from HttpContext items for error analysis.
    /// This information can be valuable for understanding performance issues that led to errors.
    /// </summary>
    /// <param name="context">The HTTP context containing query statistics</param>
    /// <returns>Query metadata if available; otherwise, null</returns>
    /// <remarks>
    /// Query statistics in error scenarios can provide crucial insights into whether database
    /// performance issues contributed to the error condition. This is particularly valuable for:
    /// - Identifying timeout scenarios caused by slow queries
    /// - Understanding resource contention issues
    /// - Debugging transaction-related problems
    /// - Analyzing query patterns that lead to exceptions
    /// 
    /// The statistics are populated by Entity Framework interceptors and provide the same
    /// level of detail as successful requests for consistent performance analysis.
    /// </remarks>
    private static QueryMetadata? ExtractQueryMetadata(HttpContext context)
    {
        if (context.Items["QueryStats"] is not Dictionary<string, object> queryStats)
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
    /// Extracts additional request metadata for error analysis and debugging purposes.
    /// This information can be crucial for diagnosing issues in production environments.
    /// </summary>
    /// <param name="context">The HTTP context containing request information</param>
    /// <returns>Dictionary of additional metadata if any is found; otherwise, null</returns>
    /// <remarks>
    /// Additional metadata in error scenarios provides comprehensive context for debugging:
    /// 1. Request characteristics (size, user agent) for understanding client behavior
    /// 2. Client identification (IP address) for security analysis and rate limiting
    /// 3. Authentication context for permission-related error analysis  
    /// 4. Custom headers for application-specific debugging information
    /// 
    /// This metadata is particularly valuable in production environments where reproducing
    /// errors can be challenging, providing the context needed for effective debugging.
    /// </remarks>
    private static Dictionary<string, object>? ExtractAdditionalMetadata(HttpContext context)
    {
        var additional = new Dictionary<string, object>();

        // Add request size information
        if (context.Request.ContentLength.HasValue)
        {
            additional["RequestSizeBytes"] = context.Request.ContentLength.Value;
        }

        // Add user agent information for client analysis
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            additional["UserAgent"] = userAgent;
        }

        // Add client IP for security analysis
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            additional["ClientIP"] = clientIp;
        }

        // Add custom headers for debugging
        var customHeaders = context.Request.Headers
            .Where(h => h.Key.StartsWith("X-Custom-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, object (h) => h.Value.ToString());

        foreach (var header in customHeaders)
        {
            additional[header.Key] = header.Value;
        }

        // Add authentication information if available (useful for auth-related errors)
        if (context.User?.Identity?.Name != null)
        {
            additional["AuthenticatedUser"] = context.User.Identity.Name;
        }

        return additional.Count > 0 ? additional : null;
    }

    /// <summary>
    /// Extracts or generates a correlation ID for error tracking across distributed systems.
    /// This method ensures error responses maintain the same correlation tracking as successful responses.
    /// </summary>
    /// <param name="context">The HTTP context containing request headers</param>
    /// <returns>The correlation ID for this request</returns>
    /// <remarks>
    /// Correlation ID extraction follows a priority hierarchy to ensure consistent tracing:
    /// 1. Use existing X-Correlation-ID header if provided by the client
    /// 2. Fall back to ASP.NET Core's TraceIdentifier for built-in tracing
    /// 3. Generate a new GUID if no existing identifier is available
    /// 
    /// This approach ensures compatibility with existing distributed tracing systems
    /// while providing fallback identification for standalone applications.
    /// </remarks>
    private static string GetCorrelationId(HttpContext context)
    {
        return context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
               ?? context.TraceIdentifier
               ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Extracts API version information from request headers or query parameters.
    /// This method ensures error responses include version information for API analytics.
    /// </summary>
    /// <param name="context">The HTTP context containing version information</param>
    /// <returns>The API version string</returns>
    /// <remarks>
    /// API version extraction supports multiple common versioning approaches:
    /// 1. Header-based versioning using X-API-Version header
    /// 2. Query parameter versioning using the 'version' parameter
    /// 3. Default to "1.0" if no version information is provided
    /// 
    /// Version information in error responses is valuable for:
    /// - Understanding which API version generated specific error patterns
    /// - Supporting multiple API versions with different error handling behaviors
    /// - Providing version-specific error messaging or handling logic
    /// </remarks>
    private static string GetApiVersion(HttpContext context)
    {
        return context.Request.Headers["X-API-Version"].FirstOrDefault()
               ?? context.Request.Query["version"].FirstOrDefault()
               ?? "1.0";
    }

    /// <summary>
    /// Creates a safe error message from an exception, preventing sensitive information leakage
    /// while providing meaningful error communication. This enhanced version provides more
    /// comprehensive exception type mapping for better user experience.
    /// </summary>
    /// <param name="exception">The exception to create a safe message from</param>
    /// <returns>A safe error message appropriate for API response</returns>
    /// <remarks>
    /// This enhanced safe message strategy provides more comprehensive exception mapping
    /// while maintaining security principles that prevent information disclosure. The method
    /// balances providing helpful error information with protecting sensitive system details.
    /// </remarks>
    private static string GetSafeErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "Invalid request parameters",
            InvalidOperationException => "Operation cannot be completed at this time",
            NotSupportedException => "Requested operation is not supported",
            System.TimeoutException => "Request timed out",
            UnauthorizedAccessException => "Access denied",
            ApplicationException => "Application error occurred",
            _ => "An unexpected error occurred" // Generic message for unknown exceptions
        };
    }

    /// <summary>
    /// Internal data structure for tracking error-specific information throughout the middleware lifecycle.
    /// This class encapsulates all the timing and identification data needed for error metadata generation.
    /// </summary>
    /// <remarks>
    /// The error tracking data provides a centralized structure for managing all error-related
    /// metadata throughout the exception handling process. This includes:
    /// 1. Unique request identification for correlation and debugging
    /// 2. Distributed tracing support through correlation IDs
    /// 3. Timing information for performance analysis of error scenarios
    /// 4. Consistent data structure for metadata generation
    /// 
    /// The class is designed to be lightweight and focused solely on tracking information,
    /// without any behavior or complex logic that might introduce additional error scenarios.
    /// </remarks>
    private class ErrorTrackingData
    {
        /// <summary>
        /// Unique identifier for this request, used for correlation across logs and responses.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// Correlation ID for distributed tracing, may span multiple services.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Timestamp when error processing began, used for metadata consistency.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Stopwatch for tracking error processing time, helps identify performance issues.
        /// </summary>
        public Stopwatch? Stopwatch { get; set; }
    }
}