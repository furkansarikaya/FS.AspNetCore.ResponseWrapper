namespace FS.AspNetCore.ResponseWrapper.Models;

public class ResponseMetadata
{
    /// <summary>
    /// Request unique identifier.
    /// </summary>
    public string RequestId { get; set; } = "";
    
    /// <summary>
    /// Response timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Response execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// API version information.
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Request path.
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// HTTP method.
    /// </summary>
    public string Method { get; set; } = "";
    
    /// <summary>
    /// Pagination metadata (for paged results).
    /// </summary>
    public PaginationMetadata? Pagination { get; set; }
    
    /// <summary>
    /// Query execution statistics.
    /// </summary>
    public QueryMetadata? Query { get; set; }
    
    /// <summary>
    /// Additional custom metadata.
    /// </summary>
    public Dictionary<string, object>? Additional { get; set; }
}