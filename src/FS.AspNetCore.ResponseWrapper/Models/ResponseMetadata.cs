namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Contains comprehensive metadata about API request processing, including timing information,
/// correlation data, pagination details, and performance metrics. This class provides detailed
/// insights into request handling that support monitoring, debugging, and analytics scenarios.
/// </summary>
/// <remarks>
/// ResponseMetadata serves as the central container for all non-business data related to API request
/// processing. It provides valuable information for application monitoring, performance analysis,
/// debugging, and distributed tracing. The metadata is automatically populated by the ResponseWrapper
/// system based on configuration options, allowing teams to control the level of detail included
/// in responses based on their specific needs and performance requirements.
/// 
/// The metadata structure is designed to be extensible and comprehensive, supporting various
/// scenarios from development debugging to production monitoring. Different metadata components
/// can be enabled or disabled through configuration to optimize performance and control
/// information exposure in different environments.
/// </remarks>
public class ResponseMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for this specific request.
    /// This identifier enables correlation of the response with corresponding log entries
    /// and provides a reference point for debugging and support scenarios.
    /// </summary>
    /// <value>
    /// A unique string identifier for the request, typically a GUID. This value remains
    /// consistent across all components involved in processing the request.
    /// </value>
    public string RequestId { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the timestamp when the response was generated.
    /// This timestamp provides timing context for the response and enables time-based
    /// analysis of API usage patterns and performance characteristics.
    /// </summary>
    /// <value>
    /// The UTC timestamp when the response was created. Using UTC ensures consistency
    /// across different time zones and simplifies time-based analysis and correlation.
    /// </value>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the total time spent processing the request, measured in milliseconds.
    /// This metric provides insight into endpoint performance and helps identify
    /// slow operations that may require optimization attention.
    /// </summary>
    /// <value>
    /// The execution time in milliseconds from request start to response completion.
    /// This includes all processing time but excludes network transmission time.
    /// </value>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Gets or sets the API version information for this endpoint.
    /// Version information supports API evolution management and enables clients
    /// to understand which API version they are interacting with.
    /// </summary>
    /// <value>
    /// A string representing the API version, typically in semantic versioning format.
    /// This information helps with API lifecycle management and client compatibility.
    /// </value>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Gets or sets the correlation identifier for distributed tracing across multiple services.
    /// This identifier enables tracking of requests that span multiple microservices
    /// or system components, providing end-to-end visibility in complex architectures.
    /// </summary>
    /// <value>
    /// A correlation identifier that may span multiple services, or null if correlation
    /// tracking is disabled. This value is essential for distributed system debugging.
    /// </value>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Gets or sets the request path that was processed to generate this response.
    /// This information provides context about which endpoint was accessed and
    /// supports routing analysis and endpoint usage monitoring.
    /// </summary>
    /// <value>
    /// The URL path portion of the request, excluding query parameters and domain information.
    /// This enables endpoint-specific analysis and monitoring.
    /// </value>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the HTTP method used for this request.
    /// This information provides context about the type of operation performed
    /// and supports RESTful API analysis and monitoring.
    /// </summary>
    /// <value>
    /// The HTTP method (GET, POST, PUT, DELETE, etc.) used for the request.
    /// This information is valuable for understanding API usage patterns.
    /// </value>
    public string Method { get; set; } = "";
    
    /// <summary>
    /// Gets or sets pagination-related metadata for responses containing paginated data.
    /// This property is populated automatically when the ResponseWrapper system detects
    /// paginated results, providing comprehensive pagination context to API consumers.
    /// </summary>
    /// <value>
    /// Pagination metadata including page numbers, sizes, and navigation indicators,
    /// or null if the response does not contain paginated data.
    /// </value>
    public PaginationMetadata? Pagination { get; set; }
    
    /// <summary>
    /// Gets or sets database query execution statistics and performance metrics.
    /// This property provides detailed insights into database interaction patterns
    /// and performance characteristics for monitoring and optimization purposes.
    /// </summary>
    /// <value>
    /// Query execution statistics including counts, timing, and caching metrics,
    /// or null if query statistics collection is disabled or no database queries were executed.
    /// </value>
    public QueryMetadata? Query { get; set; }
    
    /// <summary>
    /// Gets or sets additional custom metadata that doesn't fit into other predefined categories.
    /// This property provides extensibility for application-specific metadata requirements
    /// and custom monitoring or debugging information.
    /// </summary>
    /// <value>
    /// A dictionary containing custom metadata key-value pairs, or null if no additional
    /// metadata was collected. This enables flexible extension of the metadata structure.
    /// </value>
    public Dictionary<string, object>? Additional { get; set; }
}