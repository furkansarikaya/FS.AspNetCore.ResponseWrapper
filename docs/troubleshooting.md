# Troubleshooting Guide

Common issues and their solutions when using ResponseWrapper.

## Table of Contents

- [Installation Issues](#installation-issues)
- [Configuration Issues](#configuration-issues)
- [Wrapping Not Working](#wrapping-not-working)
- [Pagination Issues](#pagination-issues)
- [Error Handling Issues](#error-handling-issues)
- [Performance Issues](#performance-issues)
- [OpenAPI/Swagger Issues](#openapiswagger-issues)
- [Enterprise Extensions Issues](#enterprise-extensions-issues)

## Installation Issues

### Package Not Found

**Symptom:** `dotnet add package FS.AspNetCore.ResponseWrapper` fails with package not found error.

**Solutions:**

1. **Clear NuGet cache:**
   ```bash
   dotnet nuget locals all --clear
   ```

2. **Check NuGet sources:**
   ```bash
   dotnet nuget list source
   ```
   Ensure nuget.org is in the list.

3. **Add NuGet.org explicitly:**
   ```bash
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```

4. **Use specific version:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper --version 10.0.0
   ```

### Version Conflicts

**Symptom:** Package restore fails due to version conflicts.

**Solutions:**

1. **Check your .NET version:**
   ```bash
   dotnet --version
   ```
   ResponseWrapper requires .NET 10.0 or later.

2. **Update your project target framework:**
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

3. **Check for conflicting packages:**
   ```bash
   dotnet list package --include-transitive
   ```

## Configuration Issues

### Services Not Registered

**Symptom:** `InvalidOperationException: Unable to resolve service for type 'ILogger<ApiResponseWrapperFilter>'`

**Solution:** Ensure `AddResponseWrapper()` is called:

```csharp
// ❌ Wrong - Missing AddResponseWrapper
builder.Services.AddControllers();

// ✅ Correct
builder.Services.AddControllers();
builder.Services.AddResponseWrapper();
```

### Middleware Order Wrong

**Symptom:** Exceptions not being caught or responses not wrapped.

**Solution:** Ensure correct middleware order:

```csharp
var app = builder.Build();

// ✅ Correct order
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // 1. First
app.UseHttpsRedirection();                              // 2. Then HTTPS
app.UseAuthentication();                                // 3. Then Auth
app.UseAuthorization();                                 // 4. Then Authorization
app.MapControllers();                                   // 5. Finally routing

app.Run();
```

**Wrong order example:**
```csharp
// ❌ Wrong - Exception middleware too late
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // Too late!
app.MapControllers();
```

### Configuration Not Applied

**Symptom:** Custom configuration options not taking effect.

**Solution:** Verify configuration syntax:

```csharp
// ❌ Wrong - Configuration not passed
builder.Services.AddResponseWrapper();
var options = new ResponseWrapperOptions { /* ... */ };

// ✅ Correct - Use lambda
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = true;
    options.ExcludedPaths = new[] { "/health" };
});
```

## Wrapping Not Working

### Responses Not Being Wrapped

**Symptom:** API returns raw responses instead of wrapped format.

**Checklist:**

1. **✅ Controller has `[ApiController]` attribute:**
   ```csharp
   [ApiController] // Required!
   [Route("api/[controller]")]
   public class UsersController : ControllerBase { }
   ```

2. **✅ `AddResponseWrapper()` is called:**
   ```csharp
   builder.Services.AddResponseWrapper();
   ```

3. **✅ Endpoint not in `ExcludedPaths`:**
   ```csharp
   options.ExcludedPaths = new[] { "/health", "/metrics" };
   // Ensure your endpoint is NOT in this list
   ```

4. **✅ Return type not in `ExcludedTypes`:**
   ```csharp
   options.ExcludedTypes = new[] { typeof(FileResult) };
   // Ensure your return type is NOT in this list
   ```

5. **✅ No `[SkipApiResponseWrapper]` attribute:**
   ```csharp
   // ❌ This skips wrapping
   [SkipApiResponseWrapper]
   [HttpGet]
   public User Get() { }

   // ✅ This gets wrapped
   [HttpGet]
   public User Get() { }
   ```

### Specific Endpoint Not Wrapped

**Symptom:** Some endpoints work, others don't.

**Solutions:**

1. **Check if endpoint is excluded:**
   ```csharp
   // If your endpoint is /api/health
   options.ExcludedPaths = new[] { "/api/health" }; // Will be excluded
   ```

2. **Check return type:**
   ```csharp
   // IActionResult and ActionResult are wrapped
   [HttpGet]
   public async Task<User> Get() { } // ✅ Wrapped

   [HttpGet]
   public IActionResult Get() { } // ✅ Wrapped
   ```

3. **Check if it's a Minimal API:**
   ```csharp
   // ❌ Minimal APIs not supported yet
   app.MapGet("/api/users", () => new User());

   // ✅ Controller-based APIs supported
   [HttpGet]
   public User Get() => new User();
   ```

## Pagination Issues

### Pagination Not Detected

**Symptom:** Paginated responses returned as-is, metadata not extracted.

**Solutions:**

1. **Verify all required properties exist:**
   ```csharp
   public class PagedResult<T>
   {
       public List<T> Items { get; set; }      // ✅ Required (or "Data")
       public int Page { get; set; }           // ✅ Required
       public int PageSize { get; set; }       // ✅ Required
       public int TotalPages { get; set; }     // ✅ Required
       public int TotalItems { get; set; }     // ✅ Required (or "TotalCount")
       public bool HasNextPage { get; set; }   // ✅ Required
       public bool HasPreviousPage { get; set; } // ✅ Required
   }
   ```

2. **Check property types:**
   ```csharp
   // ❌ Wrong types
   public string Page { get; set; }  // Should be int
   public int HasNextPage { get; set; } // Should be bool

   // ✅ Correct types
   public int Page { get; set; }
   public bool HasNextPage { get; set; }
   ```

3. **Enable pagination detection:**
   ```csharp
   builder.Services.AddResponseWrapper(options =>
   {
       options.EnablePaginationMetadata = true; // Ensure this is true
   });
   ```

### Items Property Empty

**Symptom:** `data.items` is empty or null.

**Solutions:**

1. **Use supported property names:**
   ```csharp
   // ✅ Supported
   public List<T> Items { get; set; }
   public List<T> Data { get; set; }

   // ❌ Not supported
   public List<T> Results { get; set; }
   public List<T> Records { get; set; }
   ```

2. **Ensure list is initialized:**
   ```csharp
   // ❌ Null list
   public List<T> Items { get; set; }

   // ✅ Initialized list
   public List<T> Items { get; set; } = new();
   ```

### Wrong Page Count

**Symptom:** `TotalPages` calculation is wrong.

**Solution:** Ensure correct calculation:

```csharp
// ✅ Correct calculation
var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

// ❌ Wrong - integer division
var totalPages = totalItems / pageSize; // 93 / 20 = 4 (should be 5)
```

## Error Handling Issues

### Exceptions Not Caught

**Symptom:** Unhandled exceptions, no wrapped error response.

**Solutions:**

1. **Add exception middleware:**
   ```csharp
   app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
   ```

2. **Ensure middleware is first:**
   ```csharp
   // ✅ Correct - First in pipeline
   app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
   app.UseAuthentication();
   app.UseAuthorization();
   ```

3. **Check if exception is caught elsewhere:**
   ```csharp
   // ❌ Wrong - Catching exception before middleware
   try
   {
       return await _service.GetAsync(id);
   }
   catch (Exception ex)
   {
       return BadRequest(ex.Message); // Middleware can't catch this
   }

   // ✅ Correct - Let exception propagate
   return await _service.GetAsync(id);
   // Exception will be caught by middleware
   ```

### Wrong HTTP Status Code

**Symptom:** Getting 500 instead of 404, or wrong status codes.

**Solutions:**

1. **Use correct exception type:**
   ```csharp
   // ❌ Wrong - Generic exception = 500
   throw new Exception("User not found");

   // ✅ Correct - NotFoundException = 404
   throw new NotFoundException("User", userId);
   ```

2. **Check exception mapping:**
   ```csharp
   // Built-in mappings:
   ValidationException        → 400
   NotFoundException         → 404
   BusinessException         → 400
   ConflictException         → 409
   UnauthorizedException     → 401
   ForbiddenAccessException  → 403
   TimeoutException          → 408
   TooManyRequestsException  → 429
   ServiceUnavailableException → 503
   ```

### Error Message Not Showing

**Symptom:** Generic error message shown instead of specific one.

**Solutions:**

1. **Use `ExposeMessage` for safe messages:**
   ```csharp
   try
   {
       // Some operation
   }
   catch (Exception ex)
   {
       ex.Data["ExposeMessage"] = true; // Show actual message
       throw;
   }
   ```

2. **Use built-in exceptions:**
   ```csharp
   // ✅ Message will be shown
   throw new NotFoundException("User", userId);
   throw new ValidationException("Email is required");

   // ❌ Message hidden for security (500 error)
   throw new Exception("Database connection failed");
   ```

3. **Check environment:**
   ```csharp
   // In production, some messages are hidden for security
   // Use custom error messages in configuration
   errorMessages.NotFoundErrorMessage = "Resource not found";
   ```

## Performance Issues

### High Response Time

**Symptom:** API responses are slow after adding ResponseWrapper.

**Solutions:**

1. **Disable unnecessary features:**
   ```csharp
   builder.Services.AddResponseWrapper(options =>
   {
       options.EnableExecutionTimeTracking = false; // Disable if not needed
       options.EnableQueryStatistics = false;        // Disable if not needed
   });
   ```

2. **Check pagination reflection cache:**
   - First request: ~1-2ms overhead (reflection)
   - Subsequent requests: ~0.1ms overhead (cached)
   - This is normal and expected

3. **Profile your actual business logic:**
   ```csharp
   // ResponseWrapper overhead is minimal (<5ms typically)
   // Check your service layer for actual bottlenecks
   var stopwatch = Stopwatch.StartNew();
   var result = await _service.GetDataAsync(); // Check this
   stopwatch.Stop();
   ```

### Memory Usage High

**Symptom:** Increased memory consumption.

**Solutions:**

1. **ResponseWrapper has minimal memory footprint**
   - Reflection cache is per-type, not per-request
   - Metadata objects are small

2. **Check for actual memory leaks in your code:**
   ```csharp
   // ❌ Potential memory leak
   private static List<User> _cache = new(); // Growing indefinitely

   // ✅ Use proper caching
   private readonly IMemoryCache _cache;
   ```

## OpenAPI/Swagger Issues

### Swagger Not Showing Wrapped Responses

**Symptom:** Swagger shows raw response types instead of wrapped format.

**Solutions:**

1. **Install OpenAPI package:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle
   ```

2. **Configure OpenAPI integration:**
   ```csharp
   builder.Services.AddSwaggerGen();
   builder.Services.AddResponseWrapperOpenApi();

   app.UseSwagger();
   app.UseSwaggerUI();
   ```

3. **Ensure correct package for your setup:**
   - Swashbuckle: `FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle`
   - NSwag: `FS.AspNetCore.ResponseWrapper.OpenApi.NSwag`
   - Scalar: `FS.AspNetCore.ResponseWrapper.OpenApi.Scalar`

### Duplicate Properties in Swagger

**Symptom:** Properties appear in both `data` and `metadata` sections.

**Solution:** This is by design! The OpenAPI packages show the full wrapped structure:
- `statusCode` and `message` from your DTO are promoted to top level
- Pagination info is extracted to `metadata.pagination`
- Original properties are still in `data` for clarity

If you want to hide them in your actual API responses, use:
```csharp
public class MyResult : IHasStatusCode
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string StatusCode { get; set; } = string.Empty;
}
```

### Missing Response Headers in Swagger

**Symptom:** X-Request-ID and other headers not shown in Swagger.

**Solution:** Response headers are automatically added by the enhanced OpenAPI packages:
- X-Request-ID
- X-Correlation-ID
- ETag (if caching enabled)
- Cache-Control (if caching enabled)

Ensure you're using the enhanced OpenAPI packages, not just basic Swashbuckle.

## Enterprise Extensions Issues

### Extensions Package Not Working

**Symptom:** `AddResponseWrapperWithPreset` not found.

**Solutions:**

1. **Install the meta package:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
   ```

2. **Add using statement:**
   ```csharp
   using FS.AspNetCore.ResponseWrapper.Extensions;
   ```

3. **Check package version compatibility:**
   ```bash
   dotnet list package
   ```

### Preset Configuration Not Applied

**Symptom:** Preset features not working as expected.

**Solutions:**

1. **Use correct preset type:**
   ```csharp
   // Available presets:
   PresetType.Minimal
   PresetType.Basic
   PresetType.Standard
   PresetType.Advanced
   PresetType.Enterprise
   PresetType.GDPR
   PresetType.Performance
   PresetType.Development
   PresetType.Production
   ```

2. **Add middleware:**
   ```csharp
   builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");

   var app = builder.Build();
   app.UseResponseWrapperExtensions(); // Don't forget this!
   ```

### Caching Not Working

**Symptom:** Responses not cached despite configuration.

**Solutions:**

1. **Install caching package:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper.Caching
   ```

2. **Configure caching:**
   ```csharp
   builder.Services.AddResponseWrapperCaching(options =>
   {
       options.DefaultCacheDuration = TimeSpan.FromMinutes(5);
   });
   ```

3. **Add cache attribute to endpoints:**
   ```csharp
   [HttpGet]
   [ResponseCache(Duration = 300)] // 5 minutes
   public async Task<User> Get(int id) { }
   ```

### OpenTelemetry Not Sending Traces

**Symptom:** No traces appearing in monitoring tools.

**Solutions:**

1. **Install OpenTelemetry package:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper.OpenTelemetry
   ```

2. **Configure exporter:**
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

3. **Check endpoint connectivity:**
   ```bash
   curl http://localhost:4317
   ```

## Common Error Messages

### "Unable to resolve service"

**Full error:** `System.InvalidOperationException: Unable to resolve service for type 'Microsoft.Extensions.Logging.ILogger<FS.AspNetCore.ResponseWrapper.Filters.ApiResponseWrapperFilter>'`

**Cause:** `AddResponseWrapper()` not called.

**Fix:**
```csharp
builder.Services.AddResponseWrapper();
```

### "Sequence contains no elements"

**Cause:** Empty collection accessed with `.First()` or `.Single()`.

**Fix:** Use `.FirstOrDefault()` and check for null:
```csharp
// ❌ Wrong
var user = users.First();

// ✅ Correct
var user = users.FirstOrDefault();
if (user == null)
    throw new NotFoundException("User", id);
```

### "Object reference not set to an instance of an object"

**Cause:** Null reference in pagination detection or response wrapping.

**Fix:** Ensure objects are initialized:
```csharp
// ❌ Wrong
public List<Product> Items { get; set; }

// ✅ Correct
public List<Product> Items { get; set; } = new();
```

## Still Having Issues?

If you're still experiencing issues after trying these solutions:

1. **Check the GitHub Issues:** [https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/issues](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/issues)

2. **Enable detailed logging:**
   ```csharp
   builder.Logging.SetMinimumLevel(LogLevel.Debug);
   ```

3. **Create a minimal reproduction:**
   - Create a new project
   - Add only ResponseWrapper
   - Try to reproduce the issue
   - Share the minimal code

4. **Report the issue:**
   - Provide .NET version
   - Provide ResponseWrapper version
   - Provide steps to reproduce
   - Include error messages and stack traces

---

[← Back to Examples](examples.md) | [Next: Enterprise Features →](enterprise/README.md)
