# Preset Configurations

Complete guide to ResponseWrapper's preset configurations for quick setup.

## Overview

Presets provide pre-configured setups for common scenarios, allowing you to get started with best practices in one line of code.

**Available Presets:**
- ✅ **Minimal** - Basic wrapping only
- ✅ **Basic** - Wrapping + error handling
- ✅ **Standard** - + metadata tracking
- ✅ **Advanced** - + pagination + OpenAPI
- ✅ **Enterprise** - + tracing + caching
- ✅ **GDPR** - + data masking
- ✅ **Performance** - Optimized for speed
- ✅ **Development** - Debug-friendly
- ✅ **Production** - Production-ready

## Quick Start

### Installation

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

### Basic Usage

```csharp
using FS.AspNetCore.ResponseWrapper.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Use a preset - one line!
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

var app = builder.Build();

// Enable enterprise middleware
app.UseResponseWrapperExtensions();
app.MapControllers();

app.Run();
```

**That's it!** Your API is configured with enterprise-grade features.

## Preset Details

### Minimal

**Best for:** Learning, POC, minimal overhead

**Features:**
- ✅ Response wrapping
- ❌ Error handling middleware
- ❌ Execution time tracking
- ❌ Pagination
- ❌ Correlation ID
- ❌ OpenAPI integration
- ❌ Caching
- ❌ Tracing

**Configuration:**
```csharp
builder.Services.AddResponseWrapperWithPreset(PresetType.Minimal);
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = false;
    options.EnablePaginationMetadata = false;
    options.EnableCorrelationId = false;
    options.EnableQueryStatistics = false;
    options.WrapSuccessResponses = true;
    options.WrapErrorResponses = true;
});
```

**Use when:**
- Learning ResponseWrapper
- Proof of concept
- Minimal feature set needed
- Maximum performance required

---

### Basic

**Best for:** Simple APIs, getting started

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Standard error messages
- ❌ Execution time tracking
- ❌ Pagination
- ❌ Correlation ID
- ❌ OpenAPI integration

**Configuration:**
```csharp
builder.Services.AddResponseWrapperWithPreset(PresetType.Basic);

var app = builder.Build();
app.UseResponseWrapperExtensions();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = false;
    options.EnablePaginationMetadata = false;
    options.EnableCorrelationId = false;
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Simple API projects
- Getting started quickly
- Don't need advanced features
- Want consistent error responses

---

### Standard

**Best for:** Most API projects, recommended default

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ❌ OpenAPI integration
- ❌ Caching
- ❌ Tracing

**Configuration:**
```csharp
builder.Services.AddResponseWrapperWithPreset(PresetType.Standard, "MyAPI");

var app = builder.Build();
app.UseResponseWrapperExtensions();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
    options.EnableQueryStatistics = false;
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Standard API projects
- Need metadata tracking
- Want pagination support
- Need distributed tracing basics

---

### Advanced

**Best for:** APIs with documentation, complex features

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ✅ OpenAPI integration (Swashbuckle)
- ❌ Caching
- ❌ OpenTelemetry tracing

**Configuration:**
```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseWrapperWithPreset(PresetType.Advanced, "MyAPI");

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseResponseWrapperExtensions();
app.MapControllers();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
});

builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Public APIs with documentation
- Need Swagger/OpenAPI docs
- Complex response structures
- Multiple developers/teams

---

### Enterprise

**Best for:** Production microservices, distributed systems

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ✅ OpenAPI integration
- ✅ Response caching (Memory)
- ✅ OpenTelemetry tracing
- ✅ Custom metrics

**Configuration:**
```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseResponseWrapperExtensions();
app.MapControllers();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
});

builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
});

builder.Services.AddResponseWrapperCaching(options =>
{
    options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    options.EnableETag = true;
});

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
    options.EnableMetrics = true;
    options.EnableTracing = true;
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Production microservices
- Distributed systems
- Need observability
- Want caching
- Performance monitoring required

---

### GDPR

**Best for:** EU applications, compliance requirements

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ✅ OpenAPI integration
- ✅ Data masking
- ✅ Field selection
- ✅ GDPR compliance features
- ✅ Audit logging

**Configuration:**
```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseWrapperWithPreset(PresetType.GDPR, "MyAPI");

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseResponseWrapperExtensions();
app.MapControllers();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
});

builder.Services.AddResponseWrapperOpenApi();

builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableDataMasking = true;
    options.EnableFieldSelection = true;
    options.EnableGdprCompliance = true;
    options.MaskWithoutConsent = true;
});

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- EU customers
- Healthcare/financial data
- PII protection required
- GDPR compliance needed
- Data subject rights implementation

---

### Performance

**Best for:** High-throughput APIs, latency-sensitive applications

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Response caching (Redis)
- ✅ Minimal metadata
- ❌ Execution time tracking (optional)
- ❌ Query statistics
- ❌ OpenTelemetry (use sampling)

**Configuration:**
```csharp
builder.Services.AddResponseWrapperWithPreset(
    PresetType.Performance,
    "MyAPI",
    redisConnectionString: "localhost:6379");

var app = builder.Build();
app.UseResponseWrapperExtensions();
app.MapControllers();
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = false; // Minimal overhead
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = false;
    options.EnableQueryStatistics = false;
});

builder.Services.AddResponseWrapperRedisCaching(
    "localhost:6379",
    options =>
    {
        options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
        options.EnableETag = true;
    });

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- High-throughput APIs
- Latency-sensitive applications
- Read-heavy workloads
- Need aggressive caching
- Performance is critical

---

### Development

**Best for:** Local development, debugging

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ✅ Query statistics
- ✅ OpenAPI integration
- ✅ Console logging
- ✅ Detailed error messages
- ✅ Request/response logging

**Configuration:**
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen();
    builder.Services.AddResponseWrapperWithPreset(PresetType.Development, "MyAPI-Dev");

    var app = builder.Build();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseResponseWrapperExtensions();
    app.MapControllers();
}
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
    options.EnableQueryStatistics = true; // Development only
});

builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
});

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI-Dev";
    options.SamplingRatio = 1.0; // 100% sampling in dev
})
.AddConsoleExporter(); // Log to console

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Local development
- Debugging issues
- Testing features
- Need detailed logs
- Learning the API

---

### Production

**Best for:** Production deployments

**Features:**
- ✅ Response wrapping
- ✅ Error handling middleware
- ✅ Execution time tracking
- ✅ Pagination metadata
- ✅ Correlation ID
- ✅ OpenAPI integration
- ✅ Redis caching
- ✅ OpenTelemetry (sampled)
- ✅ Security headers
- ✅ Rate limiting ready
- ❌ Query statistics
- ❌ Detailed errors (security)

**Configuration:**
```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen();
    builder.Services.AddResponseWrapperWithPreset(
        PresetType.Production,
        "MyAPI",
        redisConnectionString: builder.Configuration.GetConnectionString("Redis"));

    var app = builder.Build();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseResponseWrapperExtensions();
    app.MapControllers();
}
```

**Equivalent to:**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;
    options.EnableQueryStatistics = false; // Expensive

    // Security
    options.ExcludedPaths = new[] { "/health", "/metrics", "/ready" };
});

builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
});

builder.Services.AddResponseWrapperRedisCaching(
    connectionString,
    options =>
    {
        options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
        options.EnableETag = true;
    });

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
    options.SamplingRatio = 0.1; // 10% sampling
    options.EnableMetrics = true;
    options.EnableTracing = true;
})
.AddOtlpExporter();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

**Use when:**
- Production deployments
- Need all features
- Want best practices
- Security is important
- Scalability required

---

## Preset Comparison

| Feature | Minimal | Basic | Standard | Advanced | Enterprise | GDPR | Performance | Development | Production |
|---------|---------|-------|----------|----------|------------|------|-------------|-------------|------------|
| Response Wrapping | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Error Handling | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Execution Time | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Pagination | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Correlation ID | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| OpenAPI | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Caching | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ✅ |
| OpenTelemetry | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ | ✅ |
| Data Masking | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Field Selection | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Query Stats | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |

## Customizing Presets

### Override Options After Preset

```csharp
// Start with a preset
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

// Override specific options
builder.Services.Configure<ResponseWrapperOptions>(options =>
{
    options.ExcludedPaths = new[] { "/health", "/custom-endpoint" };
    options.DefaultCacheDuration = TimeSpan.FromMinutes(15);
});
```

### Combining Presets with Custom Configuration

```csharp
// Use preset as base
builder.Services.AddResponseWrapperWithPreset(PresetType.Standard, "MyAPI");

// Add custom features
builder.Services.AddResponseWrapperCaching(options =>
{
    options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
});

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
});
```

## Environment-Based Preset Selection

### Automatic Preset Selection

```csharp
var presetType = builder.Environment.EnvironmentName switch
{
    "Development" => PresetType.Development,
    "Staging" => PresetType.Advanced,
    "Production" => PresetType.Production,
    _ => PresetType.Standard
};

builder.Services.AddResponseWrapperWithPreset(presetType, "MyAPI");
```

### Configuration-Based Selection

**appsettings.json:**
```json
{
  "ResponseWrapper": {
    "Preset": "Enterprise",
    "ServiceName": "MyAPI"
  }
}
```

**Program.cs:**
```csharp
var presetName = builder.Configuration["ResponseWrapper:Preset"];
var serviceName = builder.Configuration["ResponseWrapper:ServiceName"];

var presetType = Enum.Parse<PresetType>(presetName);
builder.Services.AddResponseWrapperWithPreset(presetType, serviceName);
```

## Migration Between Presets

### From Minimal to Standard

```csharp
// Before
builder.Services.AddResponseWrapperWithPreset(PresetType.Minimal);

// After
builder.Services.AddResponseWrapperWithPreset(PresetType.Standard, "MyAPI");
```

**Changes:**
- ✅ Execution time tracking enabled
- ✅ Pagination support added
- ✅ Correlation ID tracking enabled

**Impact:** Minimal overhead (~2-3ms per request)

### From Standard to Enterprise

```csharp
// Before
builder.Services.AddResponseWrapperWithPreset(PresetType.Standard, "MyAPI");

// After
builder.Services.AddResponseWrapperWithPreset(
    PresetType.Enterprise,
    "MyAPI",
    redisConnectionString: "localhost:6379");
```

**Changes:**
- ✅ Response caching enabled
- ✅ OpenTelemetry tracing enabled
- ✅ Custom metrics enabled

**Impact:** Requires Redis and OpenTelemetry infrastructure

### From Enterprise to GDPR

```csharp
// Before
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

// After
builder.Services.AddResponseWrapperWithPreset(PresetType.GDPR, "MyAPI");
```

**Changes:**
- ✅ Data masking enabled
- ✅ Field selection enabled
- ✅ GDPR compliance features enabled
- ❌ Caching disabled (for data freshness)
- ❌ OpenTelemetry disabled (for privacy)

**Impact:** Need to add `[SensitiveData]` attributes

## Best Practices

1. **Start with Standard preset** - Good balance for most APIs
2. **Use Development preset locally** - Detailed logging and debugging
3. **Use Production preset in production** - Security and performance optimized
4. **Customize after selection** - Override specific options as needed
5. **Document your choice** - Explain why you chose a preset
6. **Test before production** - Verify preset works for your use case
7. **Monitor after deployment** - Ensure performance is acceptable
8. **Use environment-based selection** - Different presets for different environments

## Troubleshooting

### Preset Not Loading

**Check package installation:**
```bash
dotnet list package | grep ResponseWrapper.Extensions
```

**Verify using statement:**
```csharp
using FS.AspNetCore.ResponseWrapper.Extensions;
```

### Features Not Working

**Ensure middleware is added:**
```csharp
app.UseResponseWrapperExtensions(); // Required!
```

**Check preset includes feature:**
- See comparison table above
- Some features only in specific presets

### Configuration Conflicts

**Preset overrides manual config:**
```csharp
// This gets overridden by preset
builder.Services.AddResponseWrapper(options => { ... });

// Use this to override preset
builder.Services.Configure<ResponseWrapperOptions>(options => { ... });
```

## Decision Guide

### Which Preset Should I Use?

**Use Minimal if:**
- Learning ResponseWrapper
- POC/prototype
- Need absolute minimum overhead

**Use Basic if:**
- Simple API
- Just want error handling
- Don't need advanced features

**Use Standard if:**
- Standard API project
- Want common features
- Good starting point

**Use Advanced if:**
- Public API with docs
- Need OpenAPI/Swagger
- Multiple developers

**Use Enterprise if:**
- Production microservices
- Need observability
- Want caching
- Distributed system

**Use GDPR if:**
- EU customers
- Healthcare/finance
- PII protection required
- Compliance needed

**Use Performance if:**
- High-throughput API
- Latency-sensitive
- Read-heavy workload

**Use Development if:**
- Local development
- Debugging
- Testing

**Use Production if:**
- Production deployment
- Need all features
- Want best practices

---

[← Back to Transformation](transformation.md) | [Back to Enterprise Overview](README.md)
