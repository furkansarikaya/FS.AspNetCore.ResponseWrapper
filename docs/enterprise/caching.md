# Caching & Performance

Complete guide to response caching with Memory, Redis, and SQL Server support.

## Overview

ResponseWrapper's caching extension provides intelligent response caching with:
- ✅ **Multiple cache providers** (Memory, Redis, SQL Server)
- ✅ **ETag support** for efficient cache validation
- ✅ **Cache-Control headers** automatically managed
- ✅ **Conditional requests** (If-None-Match, If-Modified-Since)
- ✅ **Cache invalidation** strategies
- ✅ **Per-endpoint cache configuration**
- ✅ **Query string-based cache keys**
- ✅ **User-specific caching** (optional)

## Quick Start

### Memory Caching

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Caching
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add memory caching
builder.Services.AddResponseWrapperCaching(options =>
{
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    options.EnableETag = true;
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
```

**Usage:**
```csharp
[HttpGet("{id}")]
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public async Task<User> GetUser(int id)
{
    return await _userService.GetAsync(id);
}
```

**That's it!** Your responses are now cached automatically.

### Redis Caching

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Caching.Redis
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.Caching.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add Redis caching
builder.Services.AddResponseWrapperRedisCaching(
    "localhost:6379", // Redis connection string
    options =>
    {
        options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
        options.EnableETag = true;
        options.InstanceName = "MyAPI:";
    });

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
```

### SQL Server Caching

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Caching.SqlServer
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.Caching.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add SQL Server caching
builder.Services.AddResponseWrapperSqlServerCaching(
    builder.Configuration.GetConnectionString("CacheDatabase")!,
    options =>
    {
        options.SchemaName = "dbo";
        options.TableName = "ApiResponseCache";
        options.DefaultCacheDuration = TimeSpan.FromMinutes(15);
    });

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
```

## Features

### 1. ETag Support

**Automatic ETag generation and validation:**

**First Request:**
```http
GET /api/users/1 HTTP/1.1
```

**Response:**
```http
HTTP/1.1 200 OK
ETag: "5e7d7a5e8b4c9d2f1a3e6b8c4d9f2e1a"
Cache-Control: public, max-age=300

{
  "success": true,
  "data": { "id": 1, "name": "John Doe" }
}
```

**Subsequent Request:**
```http
GET /api/users/1 HTTP/1.1
If-None-Match: "5e7d7a5e8b4c9d2f1a3e6b8c4d9f2e1a"
```

**Response (Not Modified):**
```http
HTTP/1.1 304 Not Modified
ETag: "5e7d7a5e8b4c9d2f1a3e6b8c4d9f2e1a"
Cache-Control: public, max-age=300
```

**Bandwidth saved!** No response body sent when content hasn't changed.

### 2. Cache-Control Headers

**Automatic cache control header management:**

```csharp
[HttpGet]
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<List<Product>> GetProducts()
{
    return await _productService.GetAllAsync();
}
```

**Response headers:**
```http
Cache-Control: public, max-age=300
ETag: "abc123def456"
```

**Advanced cache control:**
```csharp
[HttpGet]
[ResponseCache(
    Duration = 300,
    Location = ResponseCacheLocation.Any,
    VaryByQueryKeys = new[] { "category", "page" }
)]
public async Task<PagedResult<Product>> GetProducts(
    string? category,
    int page = 1)
{
    return await _productService.GetPagedAsync(category, page, 20);
}
```

### 3. Query String-Based Cache Keys

**Automatic cache key generation based on query parameters:**

```csharp
[HttpGet]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
public async Task<PagedResult<Product>> GetProducts(
    [FromQuery] string? category,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? sortBy = null)
{
    // Each unique combination of parameters gets its own cache entry
    return await _productService.GetPagedAsync(category, page, pageSize, sortBy);
}
```

**Cache keys generated:**
```
/api/products?category=electronics&page=1&pageSize=20&sortBy=price
/api/products?category=electronics&page=2&pageSize=20&sortBy=price
/api/products?category=books&page=1&pageSize=20
```

### 4. User-Specific Caching

**Cache responses per user:**

```csharp
builder.Services.AddResponseWrapperCaching(options =>
{
    options.VaryByUser = true; // Enable user-specific caching
});
```

```csharp
[HttpGet("dashboard")]
[ResponseCache(Duration = 60)] // Cache for 1 minute per user
public async Task<Dashboard> GetDashboard()
{
    var userId = User.FindFirst("userId")?.Value;
    return await _dashboardService.GetForUserAsync(userId);
}
```

**Cache keys:**
```
/api/dashboard?user=user123
/api/dashboard?user=user456
```

### 5. Conditional Requests

**If-Modified-Since support:**

```http
GET /api/products HTTP/1.1
If-Modified-Since: Wed, 15 Jan 2025 10:00:00 GMT
```

**Response (if not modified):**
```http
HTTP/1.1 304 Not Modified
Last-Modified: Wed, 15 Jan 2025 10:00:00 GMT
```

### 6. Cache Invalidation

**Manual cache invalidation:**

```csharp
using FS.AspNetCore.ResponseWrapper.Caching;

public class ProductsController : ControllerBase
{
    private readonly ICacheManager _cacheManager;

    [HttpPut("{id}")]
    public async Task<Product> UpdateProduct(int id, UpdateProductRequest request)
    {
        var product = await _productService.UpdateAsync(id, request);

        // Invalidate cache
        await _cacheManager.RemoveAsync($"/api/products/{id}");
        await _cacheManager.RemoveAsync("/api/products"); // Invalidate list

        return product;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteAsync(id);

        // Invalidate related caches
        await _cacheManager.RemoveByPatternAsync("/api/products*");

        return NoContent();
    }
}
```

**Pattern-based invalidation:**
```csharp
// Remove all product caches
await _cacheManager.RemoveByPatternAsync("/api/products*");

// Remove all caches for a specific category
await _cacheManager.RemoveByPatternAsync("/api/products?category=electronics*");
```

## Configuration Options

### Memory Cache Options

```csharp
builder.Services.AddResponseWrapperCaching(options =>
{
    // Default cache duration
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5);

    // Enable ETag generation
    options.EnableETag = true;

    // Maximum cache size (in MB)
    options.SizeLimit = 100;

    // Compact cache when size limit reached
    options.CompactionPercentage = 0.2; // Remove 20% of entries

    // Vary cache by user
    options.VaryByUser = false;

    // Vary cache by headers
    options.VaryByHeader = new[] { "Accept-Language" };

    // Custom cache key generator
    options.CacheKeyGenerator = (context) =>
    {
        var path = context.Request.Path.Value;
        var queryString = context.Request.QueryString.Value;
        return $"{path}{queryString}";
    };

    // Sliding expiration
    options.UseSlidingExpiration = true;
});
```

### Redis Cache Options

```csharp
builder.Services.AddResponseWrapperRedisCaching(
    "localhost:6379,password=secret,ssl=true",
    options =>
    {
        // Instance name (prefix for all keys)
        options.InstanceName = "MyAPI:";

        // Default cache duration
        options.DefaultCacheDuration = TimeSpan.FromMinutes(10);

        // Enable ETag
        options.EnableETag = true;

        // Connection multiplexer options
        options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { "redis-primary:6379", "redis-replica:6379" },
            Password = "secret",
            Ssl = true,
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000
        };

        // Retry policy
        options.RetryCount = 3;
        options.RetryDelay = TimeSpan.FromSeconds(1);
    });
```

### SQL Server Cache Options

```csharp
builder.Services.AddResponseWrapperSqlServerCaching(
    "Server=.;Database=CacheDb;Integrated Security=true",
    options =>
    {
        // Schema and table names
        options.SchemaName = "dbo";
        options.TableName = "ApiResponseCache";

        // Default cache duration
        options.DefaultCacheDuration = TimeSpan.FromMinutes(15);

        // Enable ETag
        options.EnableETag = true;

        // Cleanup interval
        options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);

        // Create table if not exists
        options.CreateTableIfNotExists = true;
    });
```

## Per-Endpoint Configuration

### Basic Caching

```csharp
[HttpGet]
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<List<Product>> GetAll()
{
    return await _productService.GetAllAsync();
}
```

### No Caching

```csharp
[HttpGet("live")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public async Task<LiveData> GetLiveData()
{
    return await _liveDataService.GetAsync();
}
```

### Private Caching (Client-Side Only)

```csharp
[HttpGet("profile")]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
public async Task<UserProfile> GetProfile()
{
    var userId = User.FindFirst("userId")?.Value;
    return await _userService.GetProfileAsync(userId);
}
```

### Vary By Query Keys

```csharp
[HttpGet]
[ResponseCache(
    Duration = 300,
    VaryByQueryKeys = new[] { "category", "page", "pageSize" }
)]
public async Task<PagedResult<Product>> GetProducts(
    string? category,
    int page = 1,
    int pageSize = 20)
{
    return await _productService.GetPagedAsync(category, page, pageSize);
}
```

### Vary By Header

```csharp
[HttpGet]
[ResponseCache(
    Duration = 300,
    VaryByHeader = "Accept-Language"
)]
public async Task<LocalizedContent> GetContent()
{
    var language = HttpContext.Request.Headers["Accept-Language"].ToString();
    return await _contentService.GetLocalizedAsync(language);
}
```

## Cache Providers Comparison

| Feature | Memory | Redis | SQL Server |
|---------|--------|-------|------------|
| **Performance** | Fastest | Fast | Moderate |
| **Distributed** | ❌ | ✅ | ✅ |
| **Persistence** | ❌ | ✅ (optional) | ✅ |
| **Scalability** | Single instance | Multi-instance | Multi-instance |
| **Cost** | Free | Infrastructure cost | Infrastructure cost |
| **Setup Complexity** | None | Medium | Medium |
| **Best For** | Single server | Microservices | Existing SQL infrastructure |

**Recommendations:**
- **Development:** Memory cache (simple, fast)
- **Single-server production:** Memory cache
- **Microservices:** Redis (shared cache)
- **Existing SQL infrastructure:** SQL Server
- **High-performance needs:** Redis
- **Low-cost:** Memory cache or SQL Server

## Real-World Examples

### E-Commerce Product Catalog

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICacheManager _cacheManager;

    // List all products - cache for 5 minutes
    [HttpGet]
    [ResponseCache(
        Duration = 300,
        VaryByQueryKeys = new[] { "category", "page", "pageSize", "sortBy" }
    )]
    public async Task<PagedResult<Product>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null)
    {
        return await _productService.GetPagedAsync(category, page, pageSize, sortBy);
    }

    // Get single product - cache for 10 minutes
    [HttpGet("{id}")]
    [ResponseCache(Duration = 600)]
    public async Task<Product> GetProduct(int id)
    {
        var product = await _productService.GetAsync(id);
        if (product == null)
            throw new NotFoundException("Product", id);
        return product;
    }

    // Update product - invalidate caches
    [HttpPut("{id}")]
    public async Task<Product> UpdateProduct(int id, UpdateProductRequest request)
    {
        var product = await _productService.UpdateAsync(id, request);

        // Invalidate caches
        await _cacheManager.RemoveAsync($"/api/products/{id}");
        await _cacheManager.RemoveByPatternAsync("/api/products?*");

        return product;
    }

    // Delete product - invalidate caches
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteAsync(id);

        // Invalidate all related caches
        await _cacheManager.RemoveAsync($"/api/products/{id}");
        await _cacheManager.RemoveByPatternAsync("/api/products*");

        return NoContent();
    }
}
```

### API with Localization

```csharp
[HttpGet("content")]
[ResponseCache(
    Duration = 600,
    VaryByHeader = "Accept-Language"
)]
public async Task<LocalizedContent> GetContent()
{
    var language = HttpContext.Request.Headers["Accept-Language"]
        .ToString()
        .Split(',')[0] // Get primary language
        .Trim();

    return await _contentService.GetLocalizedAsync(language);
}
```

**Cached separately for each language:**
```
/api/content [Accept-Language: en-US]
/api/content [Accept-Language: fr-FR]
/api/content [Accept-Language: es-ES]
```

### User Dashboard (User-Specific)

```csharp
builder.Services.AddResponseWrapperCaching(options =>
{
    options.VaryByUser = true;
});

[HttpGet("dashboard")]
[ResponseCache(Duration = 60)] // 1 minute per user
public async Task<Dashboard> GetDashboard()
{
    var userId = User.FindFirst("userId")?.Value
        ?? throw new UnauthorizedException();

    return await _dashboardService.GetForUserAsync(userId);
}
```

## Cache Warming

**Preload cache on application startup:**

```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IProductService _productService;
    private readonly ICacheManager _cacheManager;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Warm up popular products
        var popularProducts = await _productService.GetPopularAsync(100);

        foreach (var product in popularProducts)
        {
            var cacheKey = $"/api/products/{product.Id}";
            await _cacheManager.SetAsync(
                cacheKey,
                product,
                TimeSpan.FromMinutes(10));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Register in Program.cs
builder.Services.AddHostedService<CacheWarmupService>();
```

## Cache Monitoring

### Cache Hit/Miss Metrics

```csharp
public class CacheMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Counter<long> _cacheHits =
        Metrics.CreateCounter<long>("api.cache.hits");
    private static readonly Counter<long> _cacheMisses =
        Metrics.CreateCounter<long>("api.cache.misses");

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            await _next(context);

            // Check if response was from cache
            if (context.Response.Headers.ContainsKey("X-Cache"))
            {
                var cacheStatus = context.Response.Headers["X-Cache"];
                if (cacheStatus == "HIT")
                    _cacheHits.Add(1);
                else
                    _cacheMisses.Add(1);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
```

### Cache Size Monitoring

```csharp
public class CacheMonitoringService : BackgroundService
{
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<CacheMonitoringService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var stats = await _cacheManager.GetStatisticsAsync();

            _logger.LogInformation(
                "Cache stats: Size={CacheSize}MB, Entries={EntryCount}, Hit Rate={HitRate}%",
                stats.SizeMB,
                stats.EntryCount,
                stats.HitRate);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## Performance Tuning

### Memory Cache Tuning

```csharp
builder.Services.AddResponseWrapperCaching(options =>
{
    // Limit cache size to 100MB
    options.SizeLimit = 100;

    // Remove 20% of entries when limit reached
    options.CompactionPercentage = 0.2;

    // Use sliding expiration for frequently accessed items
    options.UseSlidingExpiration = true;

    // Shorter default duration to reduce memory usage
    options.DefaultCacheDuration = TimeSpan.FromMinutes(2);
});
```

### Redis Tuning

```csharp
builder.Services.AddResponseWrapperRedisCaching(
    "localhost:6379",
    options =>
    {
        // Use connection pooling
        options.ConfigurationOptions = new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" },
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            // Connection pool size
            ConnectRetry = 3
        };

        // Shorter durations to reduce Redis memory
        options.DefaultCacheDuration = TimeSpan.FromMinutes(5);

        // Enable compression for large responses
        options.EnableCompression = true;
    });
```

## Troubleshooting

### Cache Not Working

**Check configuration:**
```csharp
// Ensure caching is added
builder.Services.AddResponseWrapperCaching();

// Ensure attribute is present
[ResponseCache(Duration = 300)]
```

**Check headers:**
```http
GET /api/products HTTP/1.1

# Should return:
Cache-Control: public, max-age=300
ETag: "..."
```

### ETag Not Generated

**Enable ETag in configuration:**
```csharp
builder.Services.AddResponseWrapperCaching(options =>
{
    options.EnableETag = true; // Ensure this is true
});
```

### Cache Not Invalidating

**Check cache key:**
```csharp
// Ensure cache key matches
await _cacheManager.RemoveAsync("/api/products/123"); // Must match request path
```

**Use pattern matching:**
```csharp
// Remove all product caches
await _cacheManager.RemoveByPatternAsync("/api/products*");
```

### Redis Connection Issues

**Check connection string:**
```csharp
// Test connection
try
{
    var redis = ConnectionMultiplexer.Connect("localhost:6379");
    var db = redis.GetDatabase();
    db.StringSet("test", "value");
    var value = db.StringGet("test");
    Console.WriteLine($"Redis test: {value}");
}
catch (Exception ex)
{
    Console.WriteLine($"Redis connection failed: {ex.Message}");
}
```

## Best Practices

1. **Cache immutable data longer:** Product details, categories (10-60 minutes)
2. **Cache mutable data shorter:** User dashboards, live data (30-60 seconds)
3. **Use Redis for distributed systems:** Share cache across instances
4. **Implement cache invalidation:** Update/delete operations should invalidate
5. **Monitor cache hit rate:** Aim for >80% hit rate
6. **Set appropriate size limits:** Prevent memory exhaustion
7. **Use ETag for bandwidth savings:** Especially for large responses
8. **Warm up critical caches:** Preload on startup
9. **Use sliding expiration for hot data:** Extends cache for frequently accessed items
10. **Compress large responses:** Reduce memory/bandwidth usage

---

[← Back to Telemetry](telemetry.md) | [Next: Transformation →](transformation.md)
