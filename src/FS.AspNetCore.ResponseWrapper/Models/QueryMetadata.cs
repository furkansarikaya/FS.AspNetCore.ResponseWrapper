namespace FS.AspNetCore.ResponseWrapper.Models;

/// <summary>
/// Contains database query execution statistics and performance metrics collected during request processing.
/// This class provides detailed insights into database interaction patterns, query performance,
/// and caching effectiveness for monitoring and optimization purposes.
/// </summary>
/// <remarks>
/// QueryMetadata transforms API responses into rich performance monitoring tools, providing visibility
/// into database behavior that is typically hidden from API consumers. Think of it as a performance
/// dashboard for each API request, helping developers understand the cost and efficiency of their
/// operations.
/// 
/// **The Performance Visibility Challenge**: Traditional APIs provide no insight into their internal
/// performance characteristics. Developers and operations teams have limited visibility into whether
/// slow responses are due to business logic, database queries, network issues, or other factors.
/// QueryMetadata solves this by exposing database-level performance metrics.
/// 
/// **Optimization Guidance**: The metrics provided by this class help identify performance bottlenecks
/// and optimization opportunities. High query counts might indicate N+1 query problems, while long
/// execution times might suggest the need for database indexing or query optimization.
/// 
/// **Caching Effectiveness**: The cache hit and miss statistics provide immediate feedback about
/// caching strategy effectiveness, helping teams understand whether their caching implementation
/// is providing the expected performance benefits.
/// 
/// **Development vs Production**: While these metrics are valuable in development and staging
/// environments for optimization work, teams should consider whether to include them in production
/// responses based on security and performance considerations.
/// 
/// **Integration Requirements**: This metadata is populated through database interceptors and
/// requires coordination between the ResponseWrapper system and the data access layer to capture
/// accurate statistics.
/// </remarks>
public class QueryMetadata
{
    /// <summary>
    /// Gets or sets the total number of database queries executed during the request processing.
    /// This count includes all database interactions, providing insight into the complexity
    /// and efficiency of the data access patterns used by the endpoint.
    /// </summary>
    /// <value>
    /// The count of database queries executed. High values may indicate N+1 query problems
    /// or opportunities for query optimization and batching.
    /// </value>
    /// <remarks>
    /// The DatabaseQueriesCount property serves as a primary indicator of database interaction
    /// efficiency and can reveal several important performance patterns:
    /// 
    /// **N+1 Query Detection**: One of the most common performance problems in data access is
    /// the N+1 query pattern, where an initial query is followed by additional queries for
    /// each result item. High query counts relative to the amount of data returned often
    /// indicate this pattern.
    /// 
    /// **Complexity Assessment**: The query count provides insight into the computational
    /// complexity of the endpoint. Simple data retrieval operations should typically require
    /// only a few queries, while complex business operations might legitimately require more.
    /// 
    /// **Optimization Opportunities**: Unexpectedly high query counts suggest opportunities
    /// for optimization through techniques like eager loading, query batching, or better
    /// use of database relationships and joins.
    /// 
    /// **Performance Trending**: Tracking query counts over time helps identify performance
    /// regressions that might be introduced through code changes or data growth.
    /// 
    /// **Caching Strategy**: High query counts for frequently-accessed data suggest
    /// opportunities for implementing or improving caching strategies to reduce database load.
    /// </remarks>
    public int DatabaseQueriesCount { get; set; }

    /// <summary>
    /// Gets or sets the total time spent executing database queries during request processing, measured in milliseconds.
    /// This metric provides insight into database performance and helps identify slow query scenarios.
    /// </summary>
    /// <value>
    /// The cumulative database execution time in milliseconds. High values relative to total request time
    /// may indicate database performance issues or the need for query optimization.
    /// </value>
    /// <remarks>
    /// The DatabaseExecutionTimeMs property provides crucial timing information that helps differentiate
    /// between different types of performance issues:
    /// 
    /// **Performance Bottleneck Identification**: By comparing database execution time to total request
    /// time, teams can determine whether performance issues are database-related or stem from other
    /// parts of the application stack.
    /// 
    /// **Query Efficiency Assessment**: High execution times relative to the amount of data returned
    /// may indicate inefficient queries that could benefit from indexing, query restructuring, or
    /// database schema optimization.
    /// 
    /// **Scalability Planning**: Understanding database execution times helps predict how the system
    /// will perform under increased load and guides decisions about database scaling strategies.
    /// 
    /// **SLA Monitoring**: Database execution times are crucial for maintaining service level
    /// agreements and can trigger alerts when performance degrades beyond acceptable thresholds.
    /// 
    /// **Resource Planning**: Consistent patterns in database execution times help with capacity
    /// planning and resource allocation decisions for database infrastructure.
    /// </remarks>
    public long DatabaseExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of successful cache hits during database query processing.
    /// Cache hits represent queries that were satisfied from cache rather than executing against the database,
    /// indicating effective caching strategy implementation.
    /// </summary>
    /// <value>
    /// The count of queries satisfied from cache. Higher ratios of cache hits to total queries
    /// indicate effective caching strategies and better performance characteristics.
    /// </value>
    /// <remarks>
    /// The CacheHits property provides visibility into caching effectiveness and guides optimization
    /// efforts for improved performance:
    /// 
    /// **Caching Effectiveness Measurement**: The ratio of cache hits to total queries (including
    /// cache misses) provides a direct measure of how well the caching strategy is working.
    /// Higher ratios indicate more effective caching.
    /// 
    /// **Performance Impact Visibility**: Cache hits typically execute much faster than database
    /// queries, so a high cache hit ratio correlates with better overall performance and reduced
    /// database load.
    /// 
    /// **Cost Optimization**: In cloud environments where database operations incur costs, cache
    /// hits directly translate to cost savings by reducing the number of database operations
    /// that need to be executed and billed.
    /// 
    /// **Scalability Benefits**: High cache hit ratios mean that increased load doesn't
    /// proportionally increase database load, improving the scalability characteristics of
    /// the application.
    /// 
    /// **Cache Strategy Validation**: Tracking cache hits over time helps validate whether
    /// caching strategies are working as expected and identifies data access patterns that
    /// might benefit from different caching approaches.
    /// </remarks>
    public int CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses during database query processing.
    /// Cache misses represent queries that required database execution because the results
    /// were not available in cache, providing insight into caching effectiveness.
    /// </summary>
    /// <value>
    /// The count of queries that required database execution due to cache misses. High values
    /// relative to cache hits may indicate opportunities for improved caching strategies.
    /// </value>
    /// <remarks>
    /// The CacheMisses property complements the CacheHits metric to provide a complete picture
    /// of caching performance and optimization opportunities:
    /// 
    /// **Cache Strategy Evaluation**: The ratio of cache misses to total cache attempts reveals
    /// opportunities for improving caching strategies. High miss ratios might indicate that
    /// cache keys are too specific, cache expiration times are too short, or data access
    /// patterns don't align well with current caching approaches.
    /// 
    /// **Cache Warming Opportunities**: High cache miss counts for frequently accessed data
    /// suggest opportunities for cache warming strategies that preload commonly needed data
    /// into cache before it's requested.
    /// 
    /// **Data Access Pattern Analysis**: Cache miss patterns can reveal insights about data
    /// access behavior that might guide architectural decisions about which data should be
    /// cached and how cache strategies should be structured.
    /// 
    /// **Performance Impact Assessment**: Each cache miss typically results in a database
    /// query, so the cache miss count directly correlates with database load and can help
    /// predict the performance impact of caching strategy changes.
    /// 
    /// **Capacity Planning**: Understanding cache miss patterns helps with cache sizing
    /// decisions and infrastructure planning for cache storage requirements.
    /// </remarks>
    public int CacheMisses { get; set; }

    /// <summary>
    /// Gets or sets an array of executed query summaries for debugging and analysis purposes.
    /// This collection provides detailed information about the specific queries executed,
    /// enabling developers to analyze query patterns and identify optimization opportunities.
    /// </summary>
    /// <value>
    /// An array of query descriptions or summaries, or null if query logging is not enabled.
    /// This detailed information is primarily useful in development environments for debugging
    /// and performance analysis.
    /// </value>
    /// <remarks>
    /// The ExecutedQueries property provides the most detailed level of database interaction
    /// visibility, enabling fine-grained analysis and optimization:
    /// 
    /// **Query Pattern Analysis**: By examining the actual queries executed, developers can
    /// identify patterns like repeated queries, inefficient joins, or missing indexes that
    /// might not be obvious from aggregate statistics alone.
    /// 
    /// **Development Debugging**: During development, seeing the exact queries generated by
    /// ORM systems or data access layers helps developers understand the performance
    /// implications of their code and identify opportunities for optimization.
    /// 
    /// **Security Considerations**: In production environments, teams should carefully consider
    /// whether to include actual query text, as it might reveal sensitive information about
    /// database schema, data patterns, or business logic.
    /// 
    /// **Performance Forensics**: When performance issues occur, having access to the actual
    /// queries executed provides the detailed information needed for root cause analysis
    /// and optimization planning.
    /// 
    /// **Educational Value**: For teams learning about database performance, seeing the
    /// relationship between application code and generated queries provides valuable
    /// educational insights into how different coding patterns affect database interaction.
    /// 
    /// **Configuration Control**: The availability of this detailed information should be
    /// controlled through configuration settings, allowing teams to enable it in development
    /// and testing environments while potentially disabling it in production for security
    /// and performance reasons.
    /// </remarks>
    public string[]? ExecutedQueries { get; set; }
}