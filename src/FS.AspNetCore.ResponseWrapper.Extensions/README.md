# FS.AspNetCore.ResponseWrapper.Extensions

**All-in-One Meta Package** - Complete enterprise extensions for ResponseWrapper in a single package.

## Features

This meta package includes all ResponseWrapper extensions:

- ✅ **OpenAPI Integration** (Swashbuckle, NSwag, Scalar)
- ✅ **OpenTelemetry & Distributed Tracing**
- ✅ **Caching & Performance** (Memory, Redis, SQL Server)
- ✅ **Response Transformation & Data Masking**
- ✅ **GDPR Compliance Helpers**
- ✅ **Preset Configurations**

## Quick Start

### 1. Install Package

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

### 2. Choose Your Preset

#### Minimal (Core Only)
```csharp
services.AddResponseWrapperWithPreset(PresetType.Minimal);
```

#### Basic (Core + Memory Cache)
```csharp
services.AddResponseWrapperWithPreset(PresetType.Basic);
```

#### Standard (Core + Cache + Field Selection)
```csharp
services.AddResponseWrapperWithPreset(PresetType.Standard);
```

#### Enterprise (Full Stack)
```csharp
services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyService");
```

#### GDPR Compliant
```csharp
services.AddResponseWrapperWithPreset(PresetType.GDPRCompliant);
```

#### Performance Optimized
```csharp
services.AddResponseWrapperWithPreset(PresetType.Performance);
```

### 3. Configure Middleware

```csharp
app.UseResponseWrapperExtensions();
```

## Advanced Usage

### Full Enterprise Stack
```csharp
services.AddResponseWrapperEnterprise(
    serviceName: "MyEnterpriseAPI",
    redisConfiguration: "localhost:6379",
    enableSwashbuckle: true
);

// Add Swashbuckle
services.AddSwaggerGen(opts =>
{
    opts.AddResponseWrapperSwashbuckle();
});

app.UseResponseWrapperExtensions();
```

### Development Setup
```csharp
services.AddResponseWrapperForDevelopment("MyDevAPI");
app.UseResponseWrapperExtensions();
```

### Production Setup
```csharp
services.AddResponseWrapperForProduction(
    serviceName: "MyProductionAPI",
    redisConfiguration: Configuration.GetConnectionString("Redis")
);
app.UseResponseWrapperExtensions();
```

### GDPR Compliance
```csharp
services.AddResponseWrapperGDPR("MyGDPRAPI");
app.UseResponseWrapperExtensions();
```

### Performance Optimized
```csharp
services.AddResponseWrapperPerformance(
    redisConfiguration: "localhost:6379",
    cacheDurationSeconds: 1800 // 30 minutes
);
app.UseResponseWrapperExtensions();
```

## Preset Comparison

| Preset | Caching | Transformation | Telemetry | Best For |
|--------|---------|----------------|-----------|----------|
| Minimal | ❌ | ❌ | ❌ | Simple APIs |
| Basic | Memory | ❌ | ❌ | Small apps |
| Standard | Memory | Field Selection | ❌ | Medium apps |
| Advanced | Memory | Full | Basic | Large apps |
| Enterprise | Memory/Redis | Full | Full | Enterprise apps |
| GDPR | Memory | Privacy-first | Minimal | GDPR compliance |
| Performance | Aggressive | Field Selection | Metrics | High-traffic APIs |
| Development | Memory | Full | Verbose | Development |
| Production | Memory/Redis | Full | OTLP | Production |

## Individual Extensions

You can also use individual extensions without presets:

```csharp
// Core
services.AddResponseWrapper();

// Caching
services.AddResponseWrapperMemoryCache();
// or
services.AddResponseWrapperRedisCache("localhost:6379");

// Transformation
services.AddResponseWrapperTransformation();
// or
services.AddResponseWrapperDataMasking();
// or
services.AddResponseWrapperFieldSelection();
// or
services.AddResponseWrapperGDPRCompliance();

// OpenTelemetry
services.AddResponseWrapperOpenTelemetry("MyService");
// or
services.AddResponseWrapperOpenTelemetryWithExporters(
    serviceName: "MyService",
    useConsoleExporter: true,
    useOtlpExporter: true
);

// Middleware
app.UseResponseWrapperExtensions(
    useCache: true,
    useTelemetry: true
);
```

## OpenAPI Integration

### Swashbuckle
```csharp
services.AddSwaggerGen(opts =>
{
    opts.AddResponseWrapperSwashbuckle(
        includeErrorExamples: true,
        includeMetadata: true
    );
});
```

### NSwag
```csharp
services.AddOpenApiDocument(settings =>
{
    settings.AddResponseWrapper();
});
```

### Scalar
```csharp
app.MapScalarApiReference(opts =>
{
    opts.ConfigureForResponseWrapper();
});
```

## License

MIT License - See LICENSE file for details
