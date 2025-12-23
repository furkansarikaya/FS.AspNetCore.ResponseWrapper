# Enterprise Features

Complete guide to ResponseWrapper's enterprise extensions for production-grade APIs.

## Overview

The enterprise extensions provide advanced features for production APIs including OpenAPI integration, distributed tracing, caching, data transformation, and preset configurations.

### Quick Enterprise Setup

Install the all-in-one meta package:

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

**One-line configuration:**

```csharp
using FS.AspNetCore.ResponseWrapper.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One line gets you everything!
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

var app = builder.Build();

// Enable enterprise middleware
app.UseResponseWrapperExtensions();
app.MapControllers();

app.Run();
```

**That's it!** Your API now has:
- ✅ OpenAPI/Swagger with enhanced schemas
- ✅ OpenTelemetry distributed tracing
- ✅ Response caching with ETag support
- ✅ Data transformation and masking
- ✅ All features pre-configured

## Available Packages

### Individual Packages

Install only what you need:

| Package | Purpose | NuGet |
|---------|---------|-------|
| **OpenApi.Swashbuckle** | Swagger/Swashbuckle integration | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle) |
| **OpenApi.NSwag** | NSwag integration | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.OpenApi.NSwag.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.OpenApi.NSwag) |
| **OpenApi.Scalar** | Scalar API documentation | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.OpenApi.Scalar.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.OpenApi.Scalar) |
| **OpenTelemetry** | Distributed tracing & metrics | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.OpenTelemetry.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.OpenTelemetry) |
| **Caching** | Memory/Redis/SQL caching | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.Caching.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.Caching) |
| **Transformation** | Data masking & field selection | [![NuGet](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.Transformation.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.Transformation) |

### Meta Package (Recommended)

The meta package includes ALL enterprise extensions:

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

**Includes:**
- All OpenAPI packages (Swashbuckle, NSwag, Scalar)
- OpenTelemetry integration
- All caching providers (Memory, Redis, SQL Server)
- Data transformation features
- Preset configurations

## Feature Matrix

| Feature | Core | Enterprise | Enterprise+ |
|---------|------|------------|-------------|
| Response wrapping | ✅ | ✅ | ✅ |
| Error handling | ✅ | ✅ | ✅ |
| Pagination | ✅ | ✅ | ✅ |
| Basic metadata | ✅ | ✅ | ✅ |
| **OpenAPI Documentation** | ❌ | ✅ | ✅ |
| **Distributed Tracing** | ❌ | ✅ | ✅ |
| **Response Caching** | ❌ | ✅ | ✅ |
| **Data Masking** | ❌ | ❌ | ✅ |
| **Field Selection** | ❌ | ❌ | ✅ |
| **Preset Configs** | ❌ | ❌ | ✅ |

- **Core**: `FS.AspNetCore.ResponseWrapper`
- **Enterprise**: Individual extension packages
- **Enterprise+**: Meta package (`Extensions`)

## Quick Start by Feature

### 1. OpenAPI Integration

**Install:**
```bash
# Choose one:
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.NSwag
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Scalar
```

**Configure:**
```csharp
builder.Services.AddSwaggerGen();
builder.Services.AddResponseWrapperOpenApi();

app.UseSwagger();
app.UseSwaggerUI();
```

**Result:** Swagger shows wrapped response format automatically!

[📖 Full OpenAPI Documentation →](openapi.md)

### 2. OpenTelemetry & Distributed Tracing

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.OpenTelemetry
```

**Configure:**
```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
    options.ServiceVersion = "1.0.0";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
});
```

**Result:** Full W3C trace context propagation and custom metrics!

[📖 Full Telemetry Documentation →](telemetry.md)

### 3. Response Caching

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Caching
```

**Configure:**
```csharp
// Memory caching
builder.Services.AddResponseWrapperCaching(options =>
{
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
});

// Or Redis
builder.Services.AddResponseWrapperRedisCaching("localhost:6379");

// Or SQL Server
builder.Services.AddResponseWrapperSqlServerCaching("connection-string");
```

**Usage:**
```csharp
[HttpGet("{id}")]
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<User> Get(int id) => await _service.GetAsync(id);
```

**Result:** Automatic caching with ETag support!

[📖 Full Caching Documentation →](caching.md)

### 4. Data Transformation

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Transformation
```

**Configure:**
```csharp
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableDataMasking = true;
    options.EnableFieldSelection = true;
});
```

**Usage:**
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }

    [SensitiveData] // Automatically masked
    public string Email { get; set; }

    [SensitiveData]
    public string Phone { get; set; }
}

// Field selection via query: ?fields=id,name
[HttpGet]
public async Task<User> Get(int id) => await _service.GetAsync(id);
```

**Result:** PII protection and bandwidth optimization!

[📖 Full Transformation Documentation →](transformation.md)

### 5. Preset Configurations

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

**Configure:**
```csharp
// Choose a preset that fits your needs
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");
```

**Available presets:**
- `Minimal` - Basic wrapping only
- `Basic` - Wrapping + error handling
- `Standard` - + metadata tracking
- `Advanced` - + pagination + OpenAPI
- `Enterprise` - + tracing + caching
- `GDPR` - + data masking
- `Performance` - Optimized for speed
- `Development` - Debug-friendly
- `Production` - Production-ready

[📖 Full Presets Documentation →](presets.md)

## Complete Enterprise Example

### Installation

```bash
# Install meta package
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions

# Add OpenTelemetry exporter
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

### Configuration

```csharp
using FS.AspNetCore.ResponseWrapper.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Use preset (recommended)
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

// Option 2: Configure individually
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
    options.ExcludedPaths = new[] { "/health", "/metrics" };
});

// OpenAPI
builder.Services.AddSwaggerGen();
builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
});

// OpenTelemetry
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
    options.ServiceVersion = "1.0.0";
    options.EnableMetrics = true;
    options.EnableTracing = true;
})
.AddOtlpExporter();

// Caching (Redis)
builder.Services.AddResponseWrapperRedisCaching(
    builder.Configuration.GetConnectionString("Redis")!,
    options =>
    {
        options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
        options.EnableETag = true;
    });

// Transformation
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableDataMasking = true;
    options.EnableFieldSelection = true;
    options.MaskingCharacter = '*';
});

var app = builder.Build();

// Middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseResponseWrapperExtensions(); // Enterprise middleware

// OpenAPI
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Controller Example

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Transformation.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace MyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get user by ID (cached, traced, masked)
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300)] // Cached for 5 minutes
    public async Task<User> GetUser(int id)
    {
        var user = await _userService.GetAsync(id);

        if (user == null)
            throw new NotFoundException("User", id);

        return user;
    }

    /// <summary>
    /// Get paginated users (traced, field selection)
    /// </summary>
    [HttpGet]
    public async Task<PagedResult<User>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Field selection: ?fields=id,name
        // Automatically handled by transformation middleware
        return await _userService.GetPagedAsync(page, pageSize);
    }

    /// <summary>
    /// Create user (traced, validated)
    /// </summary>
    [HttpPost]
    public async Task<User> CreateUser(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required");

        var exists = await _userService.ExistsByEmailAsync(request.Email);
        if (exists)
            throw new ConflictException($"User with email {request.Email} already exists");

        return await _userService.CreateAsync(request);
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SensitiveData] // Masked in responses
    public string Email { get; set; } = string.Empty;

    [SensitiveData]
    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
```

### Sample Response

**Request:** `GET /api/users/1`

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "j***@example.com",
    "phone": "***-***-1234",
    "createdAt": "2025-01-01T10:00:00Z"
  },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 42,
    "correlationId": "abc-123-def-456",
    "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
    "spanId": "00f067aa0ba902b7",
    "version": "1.0",
    "path": "/api/users/1",
    "method": "GET"
  }
}
```

**Headers:**
```
X-Request-ID: 550e8400-e29b-41d4-a716-446655440000
X-Correlation-ID: abc-123-def-456
ETag: "5e7d7a5e8b4c9d2f1a3e6b8c4d9f2e1a"
Cache-Control: public, max-age=300
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

## Environment-Specific Configuration

### Development

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseWrapperWithPreset(PresetType.Development, "MyAPI");

    // Or custom
    builder.Services.AddResponseWrapper(options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = true; // Expensive, dev only
    });
}
```

### Production

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddResponseWrapperWithPreset(PresetType.Production, "MyAPI");

    // Or custom
    builder.Services.AddResponseWrapper(options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.ExcludedPaths = new[] { "/health", "/metrics", "/ready" };
    });

    // Production-grade caching
    builder.Services.AddResponseWrapperRedisCaching(
        builder.Configuration.GetConnectionString("Redis")!);

    // Production monitoring
    builder.Services.AddResponseWrapperOpenTelemetry(options =>
    {
        options.ServiceName = "MyAPI";
        options.ServiceVersion = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString() ?? "1.0.0";
    })
    .AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(
            builder.Configuration["OpenTelemetry:Endpoint"]!);
    });
}
```

## Performance Considerations

### Overhead Comparison

| Feature | Overhead | Notes |
|---------|----------|-------|
| Basic wrapping | <1ms | Negligible |
| Execution time tracking | <1ms | Simple timer |
| Pagination detection | ~2ms first, ~0.1ms cached | Reflection caching |
| OpenTelemetry | ~1-2ms | With sampling |
| Memory caching | <1ms | Hash lookup |
| Redis caching | ~5-10ms | Network call |
| Data masking | ~1-2ms | Regex operations |
| Field selection | ~1-3ms | Property filtering |

**Total overhead:** Typically 5-15ms in production with all features enabled.

### Optimization Tips

1. **Use presets** - Pre-configured for optimal performance
2. **Disable unused features** - Only enable what you need
3. **Configure caching** - Reduce backend load
4. **Use Redis for distributed caching** - Share cache across instances
5. **Enable OpenTelemetry sampling** - Reduce tracing overhead

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.EnableMetrics = true;
    options.EnableTracing = true;
    options.SamplingRatio = 0.1; // Sample 10% of requests
});
```

## Migration Guide

### From Core to Enterprise

**Step 1:** Install enterprise package
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

**Step 2:** Replace configuration
```csharp
// Before (Core)
builder.Services.AddResponseWrapper();

// After (Enterprise)
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");
```

**Step 3:** Add middleware
```csharp
app.UseResponseWrapperExtensions();
```

**Step 4:** No code changes needed in controllers! ✅

## Documentation Links

- 📖 [OpenAPI Integration](openapi.md) - Swagger, NSwag, Scalar
- 📈 [OpenTelemetry & Tracing](telemetry.md) - Distributed tracing
- ⚡ [Caching & Performance](caching.md) - Memory, Redis, SQL
- 🔒 [Data Transformation](transformation.md) - Masking, field selection
- 🎨 [Preset Configurations](presets.md) - Quick setup templates

## Support

- **Issues:** [GitHub Issues](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/issues)
- **Documentation:** [GitHub Repository](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper)
- **NuGet:** [Package Page](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.Extensions)

---

[← Back to Core Documentation](../README.md) | [Next: OpenAPI Integration →](openapi.md)
