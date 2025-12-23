# Configuration Guide

Complete reference for all ResponseWrapper configuration options.

## Table of Contents

- [Basic Configuration](#basic-configuration)
- [ResponseWrapperOptions](#responsewrapperoptions)
- [ErrorMessageConfiguration](#errormessageconfiguration)
- [Advanced Configuration](#advanced-configuration)
- [Environment-Specific Configuration](#environment-specific-configuration)

## Basic Configuration

### Minimal Setup (Default Options)

```csharp
builder.Services.AddResponseWrapper();
```

This uses all default settings:
- ✅ Execution time tracking enabled
- ✅ Pagination metadata enabled
- ✅ Correlation ID tracking enabled
- ✅ Success and error responses wrapped
- ⚠️ Query statistics disabled
- ⚠️ No excluded paths
- ⚠️ No excluded types

### With Custom Options

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.ExcludedPaths = new[] { "/health", "/metrics" };
});
```

### With Custom Options and Error Messages

```csharp
builder.Services.AddResponseWrapper(
    options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = true;
    },
    errorMessages =>
    {
        errorMessages.ValidationErrorMessage = "Invalid input";
        errorMessages.NotFoundErrorMessage = "Not found";
    });
```

## ResponseWrapperOptions

Complete reference for all options:

### Feature Toggles

```csharp
builder.Services.AddResponseWrapper(options =>
{
    // Execution Time Tracking
    options.EnableExecutionTimeTracking = true; // Default: true
    // Adds executionTimeMs to metadata

    // Pagination Metadata
    options.EnablePaginationMetadata = true; // Default: true
    // Automatically detects and extracts pagination info

    // Correlation ID
    options.EnableCorrelationId = true; // Default: true
    // Extracts X-Correlation-ID header for distributed tracing

    // Query Statistics
    options.EnableQueryStatistics = false; // Default: false
    // Adds database query stats (requires custom setup)

    // Wrap Success Responses
    options.WrapSuccessResponses = true; // Default: true
    // Wraps 2xx responses

    // Wrap Error Responses
    options.WrapErrorResponses = true; // Default: true
    // Wraps error responses from middleware
});
```

### Path Exclusions

Exclude specific paths from wrapping:

```csharp
options.ExcludedPaths = new[]
{
    "/health",
    "/metrics",
    "/swagger",
    "/api/legacy",
    "/webhooks"
};
```

**Use cases:**
- Health check endpoints
- Metrics endpoints
- Swagger/OpenAPI endpoints
- Legacy endpoints (backward compatibility)
- Webhook endpoints (external system expectations)

### Type Exclusions

Exclude specific result types from wrapping:

```csharp
options.ExcludedTypes = new[]
{
    typeof(FileResult),
    typeof(FileStreamResult),
    typeof(FileContentResult),
    typeof(RedirectResult),
    typeof(PhysicalFileResult)
};
```

**Use cases:**
- File downloads
- Redirects
- Streaming responses
- Special result types

### Custom DateTime Provider

For testing or custom time handling:

```csharp
// Option 1: Via options
options.DateTimeProvider = () => DateTime.UtcNow;

// Option 2: Via advanced registration
builder.Services.AddResponseWrapper<CustomLogger>(
    dateTimeProvider: () => DateTime.UtcNow,
    configureOptions: options => { ... }
);
```

**Use cases:**
- Unit testing (deterministic timestamps)
- Custom timezone handling
- Time mocking for integration tests

## ErrorMessageConfiguration

Customize error messages shown to users:

```csharp
builder.Services.AddResponseWrapper(
    options => { },
    errorMessages =>
    {
        // Validation Errors (400)
        errorMessages.ValidationErrorMessage = "Please check your input and try again";

        // Not Found (404)
        errorMessages.NotFoundErrorMessage = "The requested resource was not found";

        // Unauthorized (401)
        errorMessages.UnauthorizedAccessMessage = "Please log in to access this resource";

        // Forbidden (403)
        errorMessages.ForbiddenAccessMessage = "You don't have permission to access this";

        // Business Rule Violations (400)
        errorMessages.BusinessRuleViolationMessage = "This operation violates business rules";

        // Conflict (409)
        errorMessages.ConflictErrorMessage = "A conflict occurred with existing data";

        // Bad Request (400)
        errorMessages.BadRequestMessage = "Invalid request";

        // Timeout (408)
        errorMessages.TimeoutMessage = "Request timeout";

        // Too Many Requests (429)
        errorMessages.TooManyRequestsMessage = "Rate limit exceeded";

        // Service Unavailable (503)
        errorMessages.ServiceUnavailableMessage = "Service temporarily unavailable";

        // Application Errors (500)
        errorMessages.ApplicationErrorMessage = "We're experiencing technical difficulties";

        // Unexpected Errors (500)
        errorMessages.UnexpectedErrorMessage = "An unexpected error occurred";
    });
```

### Localization Example

```csharp
// Turkish error messages
errorMessages.ValidationErrorMessage = "Lütfen girdiğiniz bilgileri kontrol edin";
errorMessages.NotFoundErrorMessage = "Aradığınız kayıt bulunamadı";
errorMessages.UnauthorizedAccessMessage = "Bu işlem için giriş yapmanız gerekiyor";
errorMessages.ForbiddenAccessMessage = "Bu işlem için yetkiniz bulunmuyor";

// Spanish error messages
errorMessages.ValidationErrorMessage = "Por favor, compruebe su entrada";
errorMessages.NotFoundErrorMessage = "Recurso no encontrado";
errorMessages.UnauthorizedAccessMessage = "Debe iniciar sesión";
errorMessages.ForbiddenAccessMessage = "No tiene permiso para acceder";
```

## Advanced Configuration

### With Custom Logger

```csharp
public class CustomApiLogger : ILogger<ApiResponseWrapperFilter>
{
    // Your custom logger implementation
}

builder.Services.AddResponseWrapper<CustomApiLogger>(
    dateTimeProvider: () => DateTime.UtcNow,
    configureOptions: options =>
    {
        options.EnableExecutionTimeTracking = true;
    },
    configureErrorMessages: errorMessages =>
    {
        errorMessages.ValidationErrorMessage = "Custom validation message";
    }
);
```

### Query Statistics Integration

Requires custom implementation with EF Core interceptors:

```csharp
// 1. Enable in configuration
options.EnableQueryStatistics = true;

// 2. Create custom interceptor
public class QueryStatisticsInterceptor : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        // Track query execution
        var httpContext = GetHttpContext();
        if (httpContext != null)
        {
            var stats = GetOrCreateStats(httpContext);
            stats["QueriesCount"] = (int)stats.GetValueOrDefault("QueriesCount", 0) + 1;
        }
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

// 3. Register interceptor
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new QueryStatisticsInterceptor());
});
```

## Environment-Specific Configuration

### Development Configuration

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseWrapper(
        options =>
        {
            options.EnableExecutionTimeTracking = true;
            options.EnablePaginationMetadata = true;
            options.EnableQueryStatistics = true; // ✅ Enable in dev
            options.EnableCorrelationId = true;

            // Don't exclude anything in dev
            options.ExcludedPaths = Array.Empty<string>();
        },
        errorMessages =>
        {
            // Detailed messages in dev
            errorMessages.ValidationErrorMessage = "Validation failed - check error details";
            errorMessages.ApplicationErrorMessage = "Application error - check logs";
            errorMessages.UnexpectedErrorMessage = "Unexpected error - check stack trace";
        });
}
```

### Production Configuration

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddResponseWrapper(
        options =>
        {
            options.EnableExecutionTimeTracking = true;
            options.EnablePaginationMetadata = true;
            options.EnableQueryStatistics = false; // ❌ Disable in prod (if not needed)
            options.EnableCorrelationId = true;

            // Exclude monitoring endpoints
            options.ExcludedPaths = new[]
            {
                "/health",
                "/metrics",
                "/readiness",
                "/liveness"
            };

            // Exclude file types
            options.ExcludedTypes = new[]
            {
                typeof(FileResult),
                typeof(RedirectResult)
            };
        },
        errorMessages =>
        {
            // User-friendly messages in prod
            errorMessages.ValidationErrorMessage = "Please check your information and try again";
            errorMessages.ApplicationErrorMessage = "We're experiencing technical difficulties. Please try again later.";
            errorMessages.UnexpectedErrorMessage = "An unexpected error occurred. Our team has been notified.";
        });
}
```

### Staging Configuration

```csharp
if (builder.Environment.IsStaging())
{
    builder.Services.AddResponseWrapper(
        options =>
        {
            options.EnableExecutionTimeTracking = true;
            options.EnablePaginationMetadata = true;
            options.EnableQueryStatistics = true; // ✅ Enable for testing
            options.EnableCorrelationId = true;

            options.ExcludedPaths = new[] { "/health" };
        },
        errorMessages =>
        {
            // Slightly more detailed for staging
            errorMessages.ValidationErrorMessage = "Validation error - please review your input";
            errorMessages.ApplicationErrorMessage = "Technical error occurred (staging)";
        });
}
```

## Configuration by Use Case

### Minimal API Wrapper

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = false;
    options.EnablePaginationMetadata = false;
    options.EnableQueryStatistics = false;
    options.EnableCorrelationId = false;
});
```

### High-Performance API

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;  // Minimal overhead
    options.EnablePaginationMetadata = true;     // Minimal overhead
    options.EnableQueryStatistics = false;       // Skip if not needed
    options.EnableCorrelationId = true;          // No overhead

    // Exclude high-traffic endpoints
    options.ExcludedPaths = new[] { "/api/metrics/track" };
});
```

### Microservice with Distributed Tracing

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableQueryStatistics = true;
    options.EnableCorrelationId = true; // ✅ Critical for distributed tracing
});
```

### Public API with Rate Limiting

```csharp
builder.Services.AddResponseWrapper(
    options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnablePaginationMetadata = true;
    },
    errorMessages =>
    {
        errorMessages.TooManyRequestsMessage = "API rate limit exceeded. Please try again in a few minutes.";
        errorMessages.UnauthorizedAccessMessage = "API key required. Please include X-API-Key header.";
    });
```

### Internal API with Detailed Logging

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableQueryStatistics = true; // ✅ Track all queries
    options.EnableCorrelationId = true;
});
```

## Complete Example

Putting it all together:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure ResponseWrapper based on environment
builder.Services.AddResponseWrapper(
    options =>
    {
        // Feature toggles
        options.EnableExecutionTimeTracking = true;
        options.EnablePaginationMetadata = true;
        options.EnableCorrelationId = true;
        options.EnableQueryStatistics = builder.Environment.IsDevelopment();

        // Response wrapping control
        options.WrapSuccessResponses = true;
        options.WrapErrorResponses = true;

        // Exclusions
        options.ExcludedPaths = builder.Environment.IsProduction()
            ? new[] { "/health", "/metrics", "/swagger" }
            : new[] { "/health" };

        options.ExcludedTypes = new[]
        {
            typeof(FileResult),
            typeof(FileStreamResult),
            typeof(RedirectResult)
        };

        // Custom DateTime provider (optional)
        if (builder.Environment.IsDevelopment())
        {
            options.DateTimeProvider = () => DateTime.UtcNow;
        }
    },
    errorMessages =>
    {
        if (builder.Environment.IsProduction())
        {
            // User-friendly messages
            errorMessages.ValidationErrorMessage = "Please check your information";
            errorMessages.NotFoundErrorMessage = "Resource not found";
            errorMessages.UnauthorizedAccessMessage = "Please log in";
            errorMessages.ApplicationErrorMessage = "Technical difficulties";
        }
        else
        {
            // Detailed messages for dev/staging
            errorMessages.ValidationErrorMessage = "Validation failed";
            errorMessages.NotFoundErrorMessage = "Resource not found";
            errorMessages.ApplicationErrorMessage = "Application error";
        }
    });

var app = builder.Build();

// Add middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapControllers();
app.Run();
```

---

[← Back to Pagination](pagination.md) | [Next: Examples →](examples.md)
