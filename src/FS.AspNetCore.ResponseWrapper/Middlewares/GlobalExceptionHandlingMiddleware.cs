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
                shouldLogAsError: false); // Validation errors are expected, don't log as errors
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
                shouldLogAsError: false); // Not found is expected, don't log as error
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
                shouldLogAsError: false); // Authorization failures are expected
        }
        catch (UnauthorizedAccessException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetUnauthorizedAccessMessage(),
                [exception.Message],
                HttpStatusCode.Unauthorized,
                errorTrackingData,
                shouldLogAsError: false); // Auth failures are expected
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
            await HandleExceptionAsync(
                context,
                exception,
                _errorMessages.GetUnexpectedErrorMessage(),
                [GetSafeErrorMessage(exception)],
                HttpStatusCode.InternalServerError,
                errorTrackingData,
                shouldLogAsError: true); // Unexpected errors should definitely be logged
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
    /// Handles exceptions by creating structured error responses with appropriate metadata and status codes.
    /// This method serves as the central hub for exception processing and response generation,
    /// using configured error messages for consistent user communication.
    /// </summary>
    /// <param name="context">The HTTP context for the current request</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="userMessage">User-friendly error message from configuration to include in the response</param>
    /// <param name="errors">List of specific error messages providing additional detail</param>
    /// <param name="statusCode">HTTP status code to return based on exception type</param>
    /// <param name="trackingData">Request tracking data for metadata generation</param>
    /// <param name="shouldLogAsError">Whether this exception should be logged as an error level</param>
    /// <returns>A task representing the asynchronous exception handling operation</returns>
    /// <remarks>
    /// This method implements the core error response generation strategy, providing:
    /// 1. Appropriate logging based on exception severity and expected nature
    /// 2. Structured error responses with comprehensive metadata
    /// 3. Consistent error message formatting using configured messages
    /// 4. Robust error handling that prevents infinite exception loops
    /// 5. HTTP status code mapping that follows REST API best practices
    /// 
    /// The logging strategy differentiates between expected errors (validation, not found, etc.)
    /// that are logged at information level and unexpected errors that are logged as errors.
    /// This helps reduce noise in error logs while ensuring critical issues are properly captured.
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
    /// Creates a safe error message from an exception, preventing sensitive information leakage.
    /// This method ensures that internal system details are not exposed to API consumers
    /// while still providing meaningful error information.
    /// </summary>
    /// <param name="exception">The exception to create a safe message from</param>
    /// <returns>A safe error message appropriate for API response</returns>
    /// <remarks>
    /// The safe message strategy prevents information disclosure vulnerabilities while maintaining
    /// useful error communication. The method maps common exception types to user-friendly messages
    /// that provide guidance without exposing system internals.
    /// 
    /// For production environments, this method serves as an additional security layer,
    /// ensuring that unexpected exceptions don't leak sensitive information such as:
    /// - Database connection strings or schema information
    /// - File system paths or internal service details
    /// - Stack traces or internal error codes
    /// - Configuration values or environment variables
    /// 
    /// The mapping can be extended to handle application-specific exception types
    /// while maintaining the principle of minimal information disclosure.
    /// </remarks>
    private static string GetSafeErrorMessage(Exception exception)
    {
        // For production environments, we might want to return generic messages
        // to avoid exposing internal system details. This could be enhanced with
        // environment-specific logic.

        return exception switch
        {
            ArgumentException => "Invalid request parameters",
            InvalidOperationException => "Operation cannot be completed at this time",
            NotSupportedException => "Requested operation is not supported",
            TimeoutException => "Request timed out",
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