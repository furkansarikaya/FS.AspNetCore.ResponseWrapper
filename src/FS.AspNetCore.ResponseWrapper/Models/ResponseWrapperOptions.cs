namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Configuration options that control ResponseWrapper behavior and feature enablement.
/// This class implements the Options Pattern recommended by Microsoft for .NET configuration,
/// providing strongly-typed configuration with sensible defaults and validation capabilities.
/// </summary>
/// <remarks>
/// The options class is designed with the following principles:
/// 
/// 1. **Sensible Defaults**: All properties have reasonable default values that work
///    well for most applications without requiring explicit configuration.
/// 
/// 2. **Progressive Disclosure**: Basic scenarios work with minimal configuration,
///    while advanced scenarios can access detailed control options.
/// 
/// 3. **Performance Awareness**: Options allow disabling expensive operations
///    when they're not needed, enabling optimal performance tuning.
/// 
/// 4. **Testability**: All behavior-affecting options can be easily modified
///    for testing scenarios, including time and logging behavior.
/// 
/// The configuration follows the principle of "secure by default" where potentially
/// expensive or sensitive operations are disabled by default and must be explicitly enabled.
/// </remarks>
public class ResponseWrapperOptions
{
    /// <summary>
    /// Gets or sets a custom DateTime provider function for time-related operations.
    /// When null, the system will default to DateTime.UtcNow for consistent UTC time handling.
    /// </summary>
    /// <value>
    /// A function that returns the current DateTime, or null to use the default UTC provider.
    /// </value>
    /// <remarks>
    /// This property is particularly valuable for testing scenarios where you need deterministic
    /// time values, or for applications that require specific timezone handling or time sources.
    /// The function approach allows for dependency injection of time providers, which is a
    /// best practice for testable code.
    /// 
    /// Common use cases include:
    /// - Unit testing with fixed time values
    /// - Applications requiring specific timezone behavior
    /// - Integration with external time sources or NTP servers
    /// - Scenarios where system time might not be reliable
    /// </remarks>
    /// <example>
    /// <code>
    /// // For testing with fixed time
    /// options.DateTimeProvider = () => new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    /// 
    /// // For local time instead of UTC  
    /// options.DateTimeProvider = () => DateTime.Now;
    /// 
    /// // For integration with custom time service
    /// options.DateTimeProvider = () => timeService.GetCurrentTime();
    /// </code>
    /// </example>
    public Func<DateTime>? DateTimeProvider { get; set; }

    /// <summary>
    /// Gets or sets whether execution time tracking is enabled in response metadata.
    /// When enabled, each API response will include timing information showing how long
    /// the request took to process, which is valuable for performance monitoring and optimization.
    /// </summary>
    /// <value>
    /// true to include execution time in response metadata; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Execution time tracking provides valuable insights into API performance characteristics
    /// and can help identify slow endpoints or performance regressions. The timing starts
    /// when the filter begins processing and ends when the response is being wrapped.
    /// 
    /// Disabling this option can provide minor performance improvements in high-throughput
    /// scenarios where timing overhead is a concern, though the impact is typically minimal.
    /// The timing information is particularly useful for:
    /// - Performance monitoring and alerting
    /// - API analytics and optimization
    /// - Debugging slow request issues
    /// - SLA monitoring and reporting
    /// </remarks>
    public bool EnableExecutionTimeTracking { get; set; } = true;

    /// <summary>
    /// Gets or sets whether database query statistics are extracted and included in response metadata.
    /// This feature works in conjunction with Entity Framework interceptors to provide detailed
    /// database performance metrics for each request.
    /// </summary>
    /// <value>
    /// true to include database query statistics in response metadata; otherwise, false. Default is false.
    /// </value>
    /// <remarks>
    /// Query statistics provide detailed insights into database interaction patterns and can be
    /// invaluable for identifying performance bottlenecks, N+1 query problems, and optimization
    /// opportunities. The statistics include query counts, execution times, and cache hit ratios.
    /// 
    /// This feature is disabled by default because it requires additional setup with Entity Framework
    /// interceptors and can add overhead in high-throughput scenarios. When enabled, it provides:
    /// - Number of database queries executed per request
    /// - Total database execution time
    /// - Cache hit and miss statistics
    /// - Executed query summaries for debugging
    /// 
    /// Note: This feature requires proper configuration of database interceptors to populate
    /// the query statistics in the HttpContext.
    /// </remarks>
    public bool EnableQueryStatistics { get; set; } = false;

    /// <summary>
    /// Gets or sets whether pagination metadata is automatically extracted from paged result objects.
    /// When enabled, responses implementing IPagedResult will have their pagination information
    /// moved to the metadata section for cleaner API responses.
    /// </summary>
    /// <value>
    /// true to extract and include pagination metadata; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Pagination metadata extraction provides a clean separation between business data and
    /// pagination information in API responses. Instead of mixing pagination data with business
    /// data, the pagination information is moved to a dedicated metadata section.
    /// 
    /// This creates more consistent and predictable API responses where:
    /// - Business data remains clean and focused
    /// - Pagination information is standardized across all endpoints
    /// - Client applications can handle pagination consistently
    /// - API documentation is clearer and more maintainable
    /// 
    /// The feature automatically detects objects implementing IPagedResult and transforms them
    /// into clean data structures while preserving all pagination information in metadata.
    /// </remarks>
    public bool EnablePaginationMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether correlation ID generation and tracking is enabled for distributed tracing.
    /// Correlation IDs help track requests across multiple services and provide crucial debugging
    /// capabilities in microservice architectures.
    /// </summary>
    /// <value>
    /// true to enable correlation ID tracking; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Correlation ID tracking is essential for distributed systems where a single user request
    /// might traverse multiple services. The correlation ID provides a way to correlate log entries,
    /// metrics, and traces across service boundaries.
    /// 
    /// The system checks for existing correlation IDs in request headers (X-Correlation-ID) and
    /// uses them if present, or generates new ones if not found. This enables:
    /// - End-to-end request tracing across services
    /// - Correlation of logs and metrics for debugging
    /// - Request flow analysis and monitoring
    /// - Support for distributed tracing systems
    /// 
    /// Disabling this feature can save minimal overhead in scenarios where distributed tracing
    /// is not required, such as monolithic applications or development environments.
    /// </remarks>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether successful API responses should be wrapped in the standard response format.
    /// When disabled, only error responses will be wrapped, allowing for more flexible response handling.
    /// </summary>
    /// <value>
    /// true to wrap successful responses; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Response wrapping provides consistency across API responses but some scenarios might require
    /// bypassing this for successful responses while maintaining error response consistency.
    /// Common scenarios for disabling success wrapping include:
    /// 
    /// - Legacy API compatibility requirements
    /// - Integration with third-party systems expecting specific formats
    /// - Performance-critical endpoints where wrapper overhead is a concern
    /// - Gradual migration scenarios where only error handling needs immediate consistency
    /// 
    /// When disabled, successful responses will be returned in their original format while
    /// errors will still be wrapped for consistent error handling across the application.
    /// </remarks>
    public bool WrapSuccessResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets whether error responses should be wrapped in the standard response format.
    /// When disabled, errors will be returned in their original format without additional metadata.
    /// </summary>
    /// <value>
    /// true to wrap error responses; otherwise, false. Default is true.
    /// </value>
    /// <remarks>
    /// Error response wrapping provides consistent error handling across the application,
    /// making it easier for client applications to handle errors uniformly. Disabling this
    /// feature might be necessary for:
    /// 
    /// - Compliance with existing error response contracts
    /// - Integration scenarios requiring specific error formats
    /// - Applications where error response format is dictated by external requirements
    /// - Performance-critical error paths where additional processing should be avoided
    /// 
    /// When disabled, errors will be handled by the default ASP.NET Core error handling
    /// mechanisms, potentially resulting in inconsistent error response formats.
    /// </remarks>
    public bool WrapErrorResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets an array of request paths that should be excluded from response wrapping.
    /// This allows fine-grained control over which endpoints receive response transformation.
    /// </summary>
    /// <value>
    /// An array of path strings to exclude from wrapping. Default is an empty array.
    /// </value>
    /// <remarks>
    /// Path exclusion is useful for endpoints that need to return specific response formats
    /// that shouldn't be modified by the response wrapper. Common exclusion scenarios include:
    /// 
    /// - Health check endpoints that must return specific formats for monitoring tools
    /// - Metrics endpoints used by observability platforms
    /// - Static content or file download endpoints
    /// - Third-party integration endpoints with strict format requirements
    /// - Legacy endpoints during migration periods
    /// 
    /// Path matching is performed using case-insensitive prefix matching, so excluding "/health"
    /// will also exclude "/health/detailed" and "/health/ready". For exact matching or more
    /// complex patterns, consider using the ExcludedTypes property instead.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger", "/api/files" };
    /// </code>
    /// </example>
    public string[] ExcludedPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets an array of response types that should be excluded from wrapping.
    /// This provides type-based exclusion for scenarios where specific result types
    /// should bypass the response wrapper entirely.
    /// </summary>
    /// <value>
    /// An array of Type objects representing response types to exclude. Default is an empty array.
    /// </value>
    /// <remarks>
    /// Type-based exclusion is useful when you want to exclude all responses of certain types
    /// regardless of which endpoint they come from. This is particularly valuable for:
    /// 
    /// - File download results (FileResult, StreamResult)
    /// - Redirect responses (RedirectResult, RedirectToActionResult)
    /// - Custom result types with specific formatting requirements
    /// - Third-party library result types that shouldn't be modified
    /// 
    /// The type checking uses exact type matching, so inheritance relationships are not
    /// considered. If you need to exclude a hierarchy of types, you must include each
    /// type explicitly in the array.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ExcludedTypes = new[] 
    /// { 
    ///     typeof(FileResult), 
    ///     typeof(RedirectResult),
    ///     typeof(CustomStreamResult)
    /// };
    /// </code>
    /// </example>
    public Type[] ExcludedTypes { get; set; } = [];
}