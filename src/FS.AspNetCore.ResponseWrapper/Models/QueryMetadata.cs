namespace FS.AspNetCore.ResponseWrapper.Models;


public class QueryMetadata
{
    public int DatabaseQueriesCount { get; set; }
    public long DatabaseExecutionTimeMs { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public string[]? ExecutedQueries { get; set; }
}