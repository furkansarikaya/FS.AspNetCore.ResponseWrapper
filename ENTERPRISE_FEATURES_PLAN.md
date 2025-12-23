# FS.AspNetCore.ResponseWrapper - Enterprise Features Implementation Plan

## Executive Summary

FS.AspNetCore.ResponseWrapper projesine **modüler NuGet paketleri** stratejisiyle enterprise özellikleri ekleme planı. Core paket aynen kalacak (backward compatible), yeni özellikler ayrı opt-in paketler olarak geliştirilecek.

**Hedef**: Enterprise/Kurumsal uygulamalar için production-ready özellikler
**Strateji**: Ayrı NuGet paketleri (OpenAPI, OpenTelemetry, Caching, Transformation)
**Compatibility**: Minor breaking changes OK (current v10.x → future v11.x possible)

---

## 1. Package Architecture

### 1.1 Paket Yapısı

```
FS.AspNetCore.ResponseWrapper (Core v10.0.1)
├── FS.AspNetCore.ResponseWrapper.OpenApi (v10.0.0)
│   ├── Swashbuckle support
│   ├── NSwag support
│   └── Scalar support
│
├── FS.AspNetCore.ResponseWrapper.OpenTelemetry (v10.0.0)
│   ├── Activity/Span creation
│   ├── Metrics collection
│   └── Microsoft.Extensions.Diagnostics integration
│
├── FS.AspNetCore.ResponseWrapper.Caching (v10.0.0)
│   ├── ETag generation (strong & weak)
│   ├── Cache-Control headers
│   └── 304 Not Modified support
│
├── FS.AspNetCore.ResponseWrapper.Transformation (v10.0.0)
│   ├── Sparse fieldsets (GraphQL-style)
│   ├── Case conversion (camelCase, snake_case, PascalCase)
│   └── Null handling strategies
│
└── FS.AspNetCore.ResponseWrapper.Extensions (v10.0.0)
    └── Meta-package (all above included)
```

**Versioning Strategy (.NET Alignment)**:
- Major version matches .NET version (10.x.x for .NET 10)
- Core: 10.0.1 → 10.0.2 (minor feature addition)
- Extensions: All start at 10.0.0 (aligns with .NET 10)
- Future .NET 11 support: Bump to 11.0.0

### 1.2 Neden Ayrı Paketler?

✅ **Modüler**: Sadece ihtiyaç duyulan özellikler yüklenir
✅ **Bağımlılık izolasyonu**: Swashbuckle/NSwag conflict'leri engellenir
✅ **Versioning esnekliği**: Her paket bağımsız versiyonlanır
✅ **Backward compatibility**: Core paket hiç değişmeden kalır
✅ **Küçük footprint**: Production'da gereksiz dependency yok

---

## 2. Core Package Changes (v10.0.1 → v10.0.2)

### 2.1 Yeni Extensibility Interfaces (NON-BREAKING)

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IResponseEnricher.cs` (NEW)
```csharp
namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Extension packages use this to enrich responses after wrapping
/// </summary>
public interface IResponseEnricher
{
    int Order { get; }  // Execution order (0-49: core, 50-99: caching, 100+: others)
    Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context);
}
```

**Purpose**: OpenTelemetry, Caching, custom enrichers use this hook

---

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IMetadataProvider.cs` (NEW)
```csharp
namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Provides custom metadata to be merged into response metadata
/// </summary>
public interface IMetadataProvider
{
    string Name { get; }
    Task<Dictionary<string, object>?> GetMetadataAsync(HttpContext context);
}
```

**Purpose**: Extension packages inject custom metadata (e.g., cache status, telemetry context)

---

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IResponseTransformer.cs` (NEW)
```csharp
namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Transform response data before wrapping
/// </summary>
public interface IResponseTransformer
{
    bool CanTransform(Type responseType);
    object Transform(object data, HttpContext context);
}
```

**Purpose**: Transformation package uses this (field filtering, case conversion)

---

### 2.2 ResponseWrapperOptions Updates (NON-BREAKING)

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Models/ResponseWrapperOptions.cs` (MODIFY)

**Add these properties** (end of class):
```csharp
/// <summary>
/// Enrichers to execute after response wrapping (extension packages register here)
/// </summary>
public List<IResponseEnricher> ResponseEnrichers { get; set; } = new();

/// <summary>
/// Metadata providers for custom metadata injection
/// </summary>
public List<IMetadataProvider> MetadataProviders { get; set; } = new();

/// <summary>
/// Response transformers for data manipulation before wrapping
/// </summary>
public List<IResponseTransformer> ResponseTransformers { get; set; } = new();
```

---

### 2.3 ApiResponseWrapperFilter Enricher Pipeline (MODIFY)

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Filters/ApiResponseWrapperFilter.cs`

**Location**: `OnActionExecutionAsync` method, after `WrapResponseWithMetadata` call (~line 120)

**Add this code**:
```csharp
// Execute enricher pipeline (added in v10.0.2 for extensibility)
foreach (var enricher in _options.ResponseEnrichers.OrderBy(e => e.Order))
{
    await enricher.EnrichAsync(wrappedResponse, context.HttpContext);
}
```

**Purpose**: Extension packages (OpenTelemetry, Caching) can enrich response after wrapping

---

### 2.4 Metadata Provider Integration (MODIFY)

#### **File**: `/src/FS.AspNetCore.ResponseWrapper/Filters/ApiResponseWrapperFilter.cs`

**Location**: `BuildResponseMetadata` method, before returning metadata (~line 197)

**Add this code**:
```csharp
// Merge custom metadata from providers (added in v10.0.2)
foreach (var provider in _options.MetadataProviders)
{
    var customMetadata = await provider.GetMetadataAsync(httpContext);
    if (customMetadata != null)
    {
        metadata.Additional ??= new Dictionary<string, object>();
        foreach (var kvp in customMetadata)
        {
            metadata.Additional[$"{provider.Name}_{kvp.Key}"] = kvp.Value;
        }
    }
}
```

---

### 2.5 Version Bump

**File**: `/src/FS.AspNetCore.ResponseWrapper/FS.AspNetCore.ResponseWrapper.csproj`

```xml
<Version>10.0.2</Version>
```

**Release Notes**: "Added extensibility interfaces (IResponseEnricher, IMetadataProvider, IResponseTransformer) for extension packages"

---

## 3. Extension Package: OpenAPI

### 3.1 Package Info

**Package**: `FS.AspNetCore.ResponseWrapper.OpenApi`
**Version**: `10.0.0` (.NET 10 alignment)
**Priority**: **HIGH** (most requested feature)

### 3.2 Features

✅ **Swashbuckle Integration**: Automatic OpenAPI schema generation for `ApiResponse<T>`
✅ **NSwag Integration**: NSwag OperationProcessor for response wrapping
✅ **Scalar Support**: Scalar API documentation configuration
✅ **Error Examples**: Pre-built error response examples (400, 404, 500)
✅ **Metadata Schema**: Complete schema for ResponseMetadata

### 3.3 Directory Structure

```
src/FS.AspNetCore.ResponseWrapper.OpenApi/
├── FS.AspNetCore.ResponseWrapper.OpenApi.csproj
├── DependencyInjection.cs
├── Swashbuckle/
│   ├── ResponseWrapperOperationFilter.cs
│   ├── ResponseWrapperSchemaFilter.cs
│   └── ApiResponseExampleProvider.cs
├── NSwag/
│   ├── ResponseWrapperOperationProcessor.cs
│   └── ResponseWrapperSchemaProcessor.cs
├── Scalar/
│   └── ScalarResponseWrapperConfiguration.cs
└── Models/
    └── OpenApiResponseWrapperOptions.cs
```

### 3.4 Key Implementation

#### **ResponseWrapperOperationFilter.cs** (Swashbuckle)
```csharp
public class ResponseWrapperOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 1. Wrap all 2xx responses in ApiResponse<T>
        // 2. Add 400/404/500 error response schemas
        // 3. Include metadata schema in all responses
        // 4. Generate examples for error scenarios
    }
}
```

### 3.5 Usage

```csharp
// Install
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi

// Register (Swashbuckle)
services.AddSwaggerGen(options =>
{
    options.AddResponseWrapper(); // Extension method
});

// Register (NSwag)
services.AddOpenApiDocument(options =>
{
    options.AddResponseWrapper();
});

// Register (Scalar)
services.AddScalar().ConfigureForResponseWrapper();
```

### 3.6 Dependencies

```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.2" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="NSwag.AspNetCore" Version="14.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="1.0.0" />
```

---

## 4. Extension Package: OpenTelemetry

### 4.1 Package Info

**Package**: `FS.AspNetCore.ResponseWrapper.OpenTelemetry`
**Version**: `10.0.0` (.NET 10 alignment)
**Priority**: **HIGH** (enterprise critical)

### 4.2 Features

✅ **Activity/Span Creation**: Automatic tracing for response wrapping
✅ **Metrics Collection**: Request counter, duration histogram, error counter
✅ **Baggage Propagation**: Correlation ID in distributed tracing
✅ **Custom Tags**: Request ID, execution time, success status
✅ **Microsoft.Extensions.Diagnostics**: Health check integration

### 4.3 Directory Structure

```
src/FS.AspNetCore.ResponseWrapper.OpenTelemetry/
├── FS.AspNetCore.ResponseWrapper.OpenTelemetry.csproj
├── DependencyInjection.cs
├── Enrichers/
│   └── OpenTelemetryResponseEnricher.cs
├── Activities/
│   ├── ResponseWrapperActivitySource.cs
│   └── ResponseWrapperTags.cs
├── Metrics/
│   ├── ResponseWrapperMeter.cs
│   └── ResponseWrapperMetrics.cs
└── Models/
    └── OpenTelemetryWrapperOptions.cs
```

### 4.4 Key Implementation

#### **OpenTelemetryResponseEnricher.cs**
```csharp
public class OpenTelemetryResponseEnricher : IResponseEnricher
{
    public int Order => 100; // After caching, before custom

    public Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            // Add tags
            activity.SetTag("response.success", response.Success);
            activity.SetTag("response.request_id", response.Metadata?.RequestId);
            activity.SetTag("response.execution_time_ms", response.Metadata?.ExecutionTimeMs);

            // Baggage for distributed tracing
            if (response.Metadata?.CorrelationId != null)
            {
                Baggage.SetBaggage("correlation_id", response.Metadata.CorrelationId);
            }
        }

        // Record metrics
        _metrics.RecordRequest(response.Success,
            response.Metadata?.ExecutionTimeMs ?? 0,
            context.Request.Path);

        return Task.CompletedTask;
    }
}
```

### 4.5 Usage

```csharp
// Install
dotnet add package FS.AspNetCore.ResponseWrapper.OpenTelemetry

// Register
builder.Services.AddResponseWrapper();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddResponseWrapperInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddResponseWrapperInstrumentation();
    });
```

### 4.6 Metrics Collected

| Metric | Type | Description |
|--------|------|-------------|
| `response_wrapper.requests` | Counter | Total requests processed |
| `response_wrapper.duration` | Histogram | Request duration in ms |
| `response_wrapper.errors` | Counter | Total error responses |

### 4.7 Dependencies

```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.2" />
<PackageReference Include="OpenTelemetry.Api" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
<PackageReference Include="Microsoft.Extensions.Diagnostics" Version="8.0.0" />
```

---

## 5. Extension Package: Caching

### 5.1 Package Info

**Package**: `FS.AspNetCore.ResponseWrapper.Caching`
**Version**: `10.0.0` (.NET 10 alignment)
**Priority**: **MEDIUM**

### 5.2 Features

✅ **ETag Generation**: Strong & weak ETags (SHA256 hash)
✅ **Cache-Control Headers**: Automatic injection
✅ **304 Not Modified**: Conditional request support
✅ **If-None-Match Validation**: Efficient revalidation
✅ **Last-Modified Support**: Timestamp-based caching

### 5.3 Directory Structure

```
src/FS.AspNetCore.ResponseWrapper.Caching/
├── FS.AspNetCore.ResponseWrapper.Caching.csproj
├── DependencyInjection.cs
├── Enrichers/
│   └── CacheControlEnricher.cs
├── ETag/
│   ├── ETagGenerator.cs
│   └── WeakETagGenerator.cs
├── Attributes/
│   ├── CacheableAttribute.cs
│   └── CacheProfileAttribute.cs
├── Middleware/
│   └── ConditionalRequestMiddleware.cs
└── Models/
    └── CacheWrapperOptions.cs
```

### 5.4 Key Implementation

#### **CacheControlEnricher.cs**
```csharp
public class CacheControlEnricher : IResponseEnricher
{
    public int Order => 50; // Before OpenTelemetry

    public Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context)
    {
        var cacheAttribute = context.GetEndpoint()?.Metadata
            .GetMetadata<CacheableAttribute>();

        if (cacheAttribute != null && response.Success)
        {
            // Generate ETag
            var etag = _etagGenerator.Generate(response.Data);
            context.Response.Headers.ETag = etag;

            // Cache-Control header
            context.Response.Headers.CacheControl =
                $"public, max-age={cacheAttribute.MaxAge}";

            // Add to metadata
            response.Metadata.Additional["ETag"] = etag;
            response.Metadata.Additional["CacheMaxAge"] = cacheAttribute.MaxAge;
        }

        return Task.CompletedTask;
    }
}
```

### 5.5 Usage

```csharp
// Install
dotnet add package FS.AspNetCore.ResponseWrapper.Caching

// Register
builder.Services.AddResponseWrapper()
    .AddCaching(options =>
    {
        options.UseWeakETags = true;
        options.DefaultMaxAge = 300; // 5 minutes
    });

// Apply middleware for 304 responses
app.UseConditionalRequests();

// Use in controller
[HttpGet("{id}")]
[Cacheable(MaxAge = 600, VaryBy = "userId")]
public async Task<Product> GetProduct(int id) { }
```

### 5.6 Dependencies

```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.2" />
<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="8.0.0" />
```

---

## 6. Extension Package: Transformation

### 6.1 Package Info

**Package**: `FS.AspNetCore.ResponseWrapper.Transformation`
**Version**: `10.0.0` (.NET 10 alignment)
**Priority**: **MEDIUM**

### 6.2 Features

✅ **Sparse Fieldsets**: GraphQL-style field selection via `?fields=id,name,price`
✅ **Case Conversion**: camelCase, snake_case, PascalCase via `?case=snake`
✅ **Null Handling**: Omit, Include, or ReturnDefault strategies
✅ **Empty Collections**: Return null or empty array configuration

### 6.3 Directory Structure

```
src/FS.AspNetCore.ResponseWrapper.Transformation/
├── FS.AspNetCore.ResponseWrapper.Transformation.csproj
├── DependencyInjection.cs
├── Transformers/
│   ├── FieldFilterTransformer.cs
│   ├── CaseConversionTransformer.cs
│   └── NullHandlingTransformer.cs
├── FieldSelection/
│   └── FieldSelector.cs
└── Models/
    ├── TransformationOptions.cs
    └── NamingConvention.cs
```

### 6.4 Key Implementation

#### **FieldFilterTransformer.cs**
```csharp
public class FieldFilterTransformer : IResponseTransformer
{
    public bool CanTransform(Type responseType) => true;

    public object Transform(object data, HttpContext context)
    {
        // Check for ?fields= query parameter
        if (context.Request.Query.TryGetValue("fields", out var fields))
        {
            var requestedFields = fields.ToString().Split(',');
            return FilterFields(data, requestedFields);
        }
        return data;
    }

    private object FilterFields(object data, string[] fields)
    {
        var json = JsonSerializer.SerializeToElement(data);
        var filtered = new Dictionary<string, object>();

        foreach (var field in fields)
        {
            if (json.TryGetProperty(field, out var value))
            {
                filtered[field] = value;
            }
        }

        return filtered;
    }
}
```

### 6.5 Usage

```csharp
// Install
dotnet add package FS.AspNetCore.ResponseWrapper.Transformation

// Register
builder.Services.AddResponseWrapper()
    .AddTransformation(options =>
    {
        options.EnableFieldFiltering = true;
        options.EnableCaseConversion = true;
        options.DefaultNamingConvention = NamingConvention.CamelCase;
        options.NullHandling = NullHandling.Omit;
    });

// Use in API
// GET /api/products?fields=id,name,price&case=snake
// Returns: { "id": 1, "name": "Product", "price": 99.99 }
```

### 6.6 Security

**Blocked Fields** (prevent sensitive data exposure):
```csharp
options.BlockedFields = new[] { "password", "ssn", "creditCard", "apiKey" };
```

### 6.7 Dependencies

```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.2" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

---

## 7. Meta-Package: Extensions

### 7.1 Package Info

**Package**: `FS.AspNetCore.ResponseWrapper.Extensions`
**Version**: `10.0.0` (.NET 10 alignment)
**Priority**: **LOW** (convenience)

### 7.2 Purpose

All-in-one package that references all extension packages. For users who want everything.

### 7.3 csproj Content

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>FS.AspNetCore.ResponseWrapper.Extensions</PackageId>
    <Version>10.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.2" />
    <PackageReference Include="FS.AspNetCore.ResponseWrapper.OpenApi" Version="10.0.0" />
    <PackageReference Include="FS.AspNetCore.ResponseWrapper.OpenTelemetry" Version="10.0.0" />
    <PackageReference Include="FS.AspNetCore.ResponseWrapper.Caching" Version="10.0.0" />
    <PackageReference Include="FS.AspNetCore.ResponseWrapper.Transformation" Version="10.0.0" />
  </ItemGroup>
</Project>
```

### 7.4 Unified Registration

```csharp
// Single package install
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions

// Unified registration
services.AddResponseWrapperWithExtensions(options =>
{
    // Core
    options.EnableExecutionTimeTracking = true;

    // OpenTelemetry
    options.OpenTelemetry.RecordMetadata = true;

    // Caching
    options.Caching.UseWeakETags = true;

    // Transformation
    options.Transformation.EnableFieldFiltering = true;
});
```

---

## 8. Implementation Priority & Timeline

### Phase 1: Foundation (Weeks 1-2) - CRITICAL
**Goal**: Core v10.0.2 with extensibility

**Tasks**:
1. ✅ Add `IResponseEnricher` interface
2. ✅ Add `IMetadataProvider` interface
3. ✅ Add `IResponseTransformer` interface
4. ✅ Modify `ResponseWrapperOptions` with new lists
5. ✅ Modify `ApiResponseWrapperFilter.OnActionExecutionAsync` (enricher pipeline)
6. ✅ Modify `ApiResponseWrapperFilter.BuildResponseMetadata` (metadata providers)
7. ✅ Unit tests for enricher execution order
8. ✅ Unit tests for metadata provider merging

**Deliverable**: Core v10.0.2 published to NuGet

---

### Phase 2: OpenAPI Package (Weeks 3-4) - HIGH PRIORITY
**Goal**: FS.AspNetCore.ResponseWrapper.OpenApi v10.0.0

**Tasks**:
1. ✅ Swashbuckle `ResponseWrapperOperationFilter`
2. ✅ NSwag `ResponseWrapperOperationProcessor`
3. ✅ Scalar configuration helpers
4. ✅ Error response example generators
5. ✅ Schema generation for `ApiResponse<T>` and `ResponseMetadata`
6. ✅ Integration tests with Swashbuckle/NSwag/Scalar
7. ✅ Sample project with all three tools
8. ✅ Documentation (README, examples)

**Deliverable**: OpenApi v10.0.0 published to NuGet

---

### Phase 3: OpenTelemetry Package (Weeks 5-6) - HIGH PRIORITY
**Goal**: FS.AspNetCore.ResponseWrapper.OpenTelemetry v10.0.0

**Tasks**:
1. ✅ `ResponseWrapperActivitySource` with tags
2. ✅ `ResponseWrapperMeter` with Counter/Histogram
3. ✅ `OpenTelemetryResponseEnricher` implementation
4. ✅ Baggage propagation for correlation ID
5. ✅ Integration with `Microsoft.Extensions.Diagnostics`
6. ✅ Health check metadata integration
7. ✅ Integration tests with Jaeger/Zipkin
8. ✅ Sample project with distributed tracing

**Deliverable**: OpenTelemetry v10.0.0 published to NuGet

---

### Phase 4: Caching Package (Weeks 7-8) - MEDIUM PRIORITY
**Goal**: FS.AspNetCore.ResponseWrapper.Caching v10.0.0

**Tasks**:
1. ✅ Strong ETag generator (SHA256)
2. ✅ Weak ETag generator
3. ✅ `CacheControlEnricher` for header injection
4. ✅ `ConditionalRequestMiddleware` for 304 responses
5. ✅ `[Cacheable]` attribute
6. ✅ Integration tests with If-None-Match
7. ✅ Sample project with caching

**Deliverable**: Caching v10.0.0 published to NuGet

---

### Phase 5: Transformation Package (Weeks 9-10) - MEDIUM PRIORITY
**Goal**: FS.AspNetCore.ResponseWrapper.Transformation v10.0.0

**Tasks**:
1. ✅ `FieldFilterTransformer` (sparse fieldsets)
2. ✅ `CaseConversionTransformer` (camelCase, snake_case)
3. ✅ Null handling strategies
4. ✅ Empty collection handling
5. ✅ Security: blocked fields configuration
6. ✅ Integration tests
7. ✅ Sample project

**Deliverable**: Transformation v10.0.0 published to NuGet

---

### Phase 6: Meta-Package & Polish (Weeks 11-12) - LOW PRIORITY
**Goal**: All packages polished and integrated

**Tasks**:
1. ✅ Create Extensions meta-package
2. ✅ Unified registration API
3. ✅ Comprehensive sample projects
4. ✅ Performance benchmarks (vs core)
5. ✅ Update main README
6. ✅ Create migration guides

**Deliverable**: Extensions v10.0.0 published to NuGet

---

## 9. Breaking Changes & Migration

### 9.1 Core v10.0.1 → v10.0.2 (NO BREAKING CHANGES)

**Changes**:
- ✅ Added extensibility interfaces (opt-in)
- ✅ Added lists to `ResponseWrapperOptions` (defaults empty)
- ✅ Enricher pipeline execution (only if enrichers registered)

**Migration**: None required - existing code works as-is

---

### 9.2 Future v11.0.0 (Optional Breaking Changes)

**If** we want fluent API:

**v10.x (current)**:
```csharp
services.AddResponseWrapper(options => { });
```

**v11.0.0 (future)**:
```csharp
services.AddResponseWrapper()
    .ConfigureCore(options => { })
    .WithOpenTelemetry()
    .WithCaching();
```

**Decision**: Not needed now - can revisit after feedback

---

## 10. Testing Strategy

### 10.1 Unit Tests (Per Package)

**Core** (`FS.AspNetCore.ResponseWrapper.Tests`):
- Enricher pipeline execution order
- Metadata provider merging
- Transformer execution
- Conflict resolution (system vs custom metadata)

**OpenApi** (`FS.AspNetCore.ResponseWrapper.OpenApi.Tests`):
- Swashbuckle schema generation
- NSwag operation processing
- Error example accuracy

**OpenTelemetry** (`FS.AspNetCore.ResponseWrapper.OpenTelemetry.Tests`):
- Activity tag setting
- Metrics recording
- Baggage propagation

**Caching** (`FS.AspNetCore.ResponseWrapper.Caching.Tests`):
- ETag generation consistency
- 304 response logic
- Cache-Control header formatting

**Transformation** (`FS.AspNetCore.ResponseWrapper.Transformation.Tests`):
- Field filtering accuracy
- Case conversion correctness
- Security (blocked fields)

### 10.2 Integration Tests

**Scenario**: All packages together
```csharp
[Fact]
public async Task AllExtensions_WorkTogether()
{
    // Arrange: Register all packages
    // Act: Make request with ?fields=id,name&case=snake + If-None-Match
    // Assert:
    //   - Response has filtered fields
    //   - Case converted to snake_case
    //   - ETag header present
    //   - OpenTelemetry activity recorded
    //   - Metrics incremented
}
```

---

## 11. Documentation Plan

### 11.1 Main README Updates

Add section:
```markdown
## Extension Packages

FS.AspNetCore.ResponseWrapper can be extended with:

- **OpenAPI/Swagger** - Automatic OpenAPI schema generation
  ```bash
  dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi
  ```

- **OpenTelemetry** - Distributed tracing and metrics
  ```bash
  dotnet add package FS.AspNetCore.ResponseWrapper.OpenTelemetry
  ```

- **Caching** - ETag and Cache-Control support
  ```bash
  dotnet add package FS.AspNetCore.ResponseWrapper.Caching
  ```

- **Transformation** - Field filtering and formatting
  ```bash
  dotnet add package FS.AspNetCore.ResponseWrapper.Transformation
  ```

- **Extensions** - All-in-one meta-package
  ```bash
  dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
  ```
```

### 11.2 Per-Package README

Each package needs:
1. Quick Start (3-5 lines)
2. Features list
3. Configuration options
4. Examples
5. Integration guide

### 11.3 Sample Projects

Create `/samples/` directory:
```
samples/
├── 1.Basic/                    (existing)
├── 2.OpenApi.Example/          (Swashbuckle + NSwag + Scalar)
├── 3.OpenTelemetry.Example/    (Jaeger tracing)
├── 4.Caching.Example/          (ETag + 304)
├── 5.Transformation.Example/   (Field filtering)
└── 6.AllFeatures.Example/      (Everything together)
```

---

## 12. Performance Targets

### 12.1 Overhead Benchmarks (vs Core Only)

| Package | Target Overhead | Notes |
|---------|----------------|-------|
| **Core only** | Baseline (0ms) | Current performance |
| **OpenApi** | 0ms | Compile-time only, no runtime impact |
| **OpenTelemetry** | <5ms | Activity creation + metric recording |
| **Caching** | <2ms | ETag generation (SHA256 hash) |
| **Transformation** | <10ms | Field filtering + JSON re-serialization |
| **All Extensions** | <15ms | Combined worst-case |

### 12.2 Optimization Strategies

**Caching**:
- Cache ETag generation results (same data → same ETag)
- Use weak ETags for dynamic content

**Transformation**:
- Only when `?fields=` or `?case=` query params present
- Cache filtered schemas per endpoint

**OpenTelemetry**:
- Async metric recording (non-blocking)
- Batched activity reporting

---

## 13. Security Considerations

### 13.1 Field Filtering Security

**Risk**: Users request sensitive fields

**Mitigation**:
```csharp
options.Transformation.BlockedFields = new[]
{
    "password", "ssn", "creditCard", "apiKey", "secret"
};
```

### 13.2 OpenTelemetry Data Exposure

**Risk**: Request/response bodies in traces

**Mitigation**:
```csharp
options.OpenTelemetry.RecordRequestBodies = false; // Default
options.OpenTelemetry.RecordResponseBodies = false;
options.OpenTelemetry.RecordMetadata = true; // Safe
```

### 13.3 Cache Poisoning

**Risk**: Malicious ETags

**Mitigation**:
- Strong cryptographic hashing (SHA256)
- Validate ETag format before comparison
- Use weak ETags for user-influenced content

---

## 14. Release Strategy

### 14.1 Beta → RC → Stable

**Each package**:
1. `v10.0.0-beta.1` - Community feedback (2 weeks)
2. `v10.0.0-rc.1` - Address feedback (1 week)
3. `v10.0.0` - Stable release

### 14.2 Publishing Order

1. **Week 2**: Core v10.0.2
2. **Week 4**: OpenApi v10.0.0-beta.1
3. **Week 6**: OpenTelemetry v10.0.0-beta.1
4. **Week 8**: Caching v10.0.0-beta.1
5. **Week 10**: Transformation v10.0.0-beta.1
6. **Week 12**: Extensions v10.0.0 (all stable)

---

## 15. Critical Files Summary

### Core Package (v10.0.2)

**NEW FILES**:
- `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IResponseEnricher.cs`
- `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IMetadataProvider.cs`
- `/src/FS.AspNetCore.ResponseWrapper/Extensibility/IResponseTransformer.cs`

**MODIFIED FILES**:
- `/src/FS.AspNetCore.ResponseWrapper/Models/ResponseWrapperOptions.cs`
  - Add: `ResponseEnrichers`, `MetadataProviders`, `ResponseTransformers` lists

- `/src/FS.AspNetCore.ResponseWrapper/Filters/ApiResponseWrapperFilter.cs`
  - Modify: `OnActionExecutionAsync` method (~line 120) - add enricher pipeline
  - Modify: `BuildResponseMetadata` method (~line 197) - add metadata provider integration

- `/src/FS.AspNetCore.ResponseWrapper/FS.AspNetCore.ResponseWrapper.csproj`
  - Change: `<Version>10.0.2</Version>`

### OpenApi Package (v10.0.0)

**NEW PROJECT**:
- `/src/FS.AspNetCore.ResponseWrapper.OpenApi/` (entire new project)

### OpenTelemetry Package (v10.0.0)

**NEW PROJECT**:
- `/src/FS.AspNetCore.ResponseWrapper.OpenTelemetry/` (entire new project)

### Caching Package (v10.0.0)

**NEW PROJECT**:
- `/src/FS.AspNetCore.ResponseWrapper.Caching/` (entire new project)

### Transformation Package (v10.0.0)

**NEW PROJECT**:
- `/src/FS.AspNetCore.ResponseWrapper.Transformation/` (entire new project)

### Extensions Package (v10.0.0)

**NEW PROJECT**:
- `/src/FS.AspNetCore.ResponseWrapper.Extensions/` (entire new project)

---

## 16. Success Criteria

### 16.1 Core v10.0.2
- ✅ All existing tests pass
- ✅ No breaking changes
- ✅ Enricher pipeline works
- ✅ Performance: <1ms overhead

### 16.2 OpenApi v10.0.0
- ✅ Swashbuckle integration works
- ✅ NSwag integration works
- ✅ Scalar integration works
- ✅ Error examples accurate
- ✅ Schema generation correct

### 16.3 OpenTelemetry v10.0.0
- ✅ Activities created with correct tags
- ✅ Metrics recorded accurately
- ✅ Jaeger/Zipkin integration verified
- ✅ Performance: <5ms overhead

### 16.4 Caching v10.0.0
- ✅ ETags generated consistently
- ✅ 304 responses work
- ✅ Cache-Control headers correct
- ✅ Performance: <2ms overhead

### 16.5 Transformation v10.0.0
- ✅ Field filtering accurate
- ✅ Case conversion works
- ✅ Blocked fields enforced
- ✅ Performance: <10ms overhead

---

## Conclusion

Bu plan enterprise özellikleri **modüler, backward-compatible, production-ready** şekilde ekliyor.

**Avantajlar**:
✅ Mevcut kullanıcılar etkilenmiyor
✅ Sadece ihtiyaç duyulan özellikler yükleniyor
✅ Her paket bağımsız versiyonlanıyor
✅ Dependency conflict'leri minimize ediliyor
✅ Test edilebilir, maintainable mimari

**Immediate Next Steps**:
1. Core v10.0.2 implementation (extensibility interfaces)
2. OpenApi package (en çok talep edilen)
3. Community feedback & iteration

**Questions/Concerns**:
- Paket sayısı çok mu? (Hayır - her biri spesifik ihtiyaca cevap veriyor)
- Performance impact? (Her paket <5ms overhead target)
- Maintenance burden? (Modüler yapı bakımı kolaylaştırıyor)
