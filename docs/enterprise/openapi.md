# OpenAPI Integration

Complete guide to integrating ResponseWrapper with OpenAPI documentation tools (Swagger, NSwag, Scalar).

## Overview

ResponseWrapper provides first-class OpenAPI integration that automatically transforms your API documentation to show the wrapped response format. All your endpoints automatically display the complete response structure including metadata, pagination, and error formats.

**Supported Tools:**
- ✅ Swashbuckle (Swagger)
- ✅ NSwag
- ✅ Scalar

## Quick Start

### Swashbuckle (Swagger)

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add Swagger
builder.Services.AddSwaggerGen();

// Add ResponseWrapper OpenAPI integration
builder.Services.AddResponseWrapperOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
```

**That's it!** Your Swagger documentation now shows wrapped responses automatically.

### NSwag

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.NSwag
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.OpenApi.NSwag;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add NSwag
builder.Services.AddOpenApiDocument();

// Add ResponseWrapper OpenAPI integration
builder.Services.AddResponseWrapperNSwag();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseOpenApi();
app.UseSwaggerUi();
app.MapControllers();

app.Run();
```

### Scalar

**Install:**
```bash
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Scalar
dotnet add package Scalar.AspNetCore
```

**Configure:**
```csharp
using FS.AspNetCore.ResponseWrapper.OpenApi.Scalar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add Scalar
builder.Services.AddOpenApi();

// Add ResponseWrapper OpenAPI integration
builder.Services.AddResponseWrapperScalar();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();
```

## Features

### 1. Automatic Response Wrapping in Documentation

**Your Controller:**
```csharp
[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    return await _userService.GetAsync(id);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

**What Swagger Shows (Before Integration):**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john@example.com"
}
```

**What Swagger Shows (After Integration):**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com"
  },
  "message": null,
  "statusCode": null,
  "errors": null,
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 42,
    "version": "1.0",
    "correlationId": null,
    "path": "/api/users/1",
    "method": "GET",
    "pagination": null,
    "additional": null
  }
}
```

### 2. Enhanced Error Responses

**Automatically adds error response examples for common status codes:**

**400 Bad Request:**
```json
{
  "success": false,
  "data": null,
  "message": "Please check your input and try again",
  "statusCode": "VALIDATION_ERROR",
  "errors": ["Email is required", "Password must be at least 8 characters"],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440001",
    "timestamp": "2025-01-15T10:31:22.456Z",
    "executionTimeMs": 5
  }
}
```

**404 Not Found:**
```json
{
  "success": false,
  "data": null,
  "message": "The requested item could not be found",
  "statusCode": "NOT_FOUND",
  "errors": ["User (123) was not found."],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440002",
    "timestamp": "2025-01-15T10:32:10.789Z",
    "executionTimeMs": 3
  }
}
```

**500 Internal Server Error:**
```json
{
  "success": false,
  "data": null,
  "message": "We're experiencing technical difficulties",
  "statusCode": "INTERNAL_ERROR",
  "errors": ["An unexpected error occurred"],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440003",
    "timestamp": "2025-01-15T10:33:45.123Z",
    "executionTimeMs": 125
  }
}
```

### 3. Response Headers Documentation

**Automatically documents response headers:**

| Header | Type | Description |
|--------|------|-------------|
| X-Request-ID | string (uuid) | Unique request identifier for tracking and correlation |
| X-Correlation-ID | string | Correlation ID for distributed tracing (if provided in request) |
| ETag | string | Entity tag for cache validation (if caching enabled) |
| Cache-Control | string | Cache control directives (if caching enabled) |

**Example:**
```http
HTTP/1.1 200 OK
X-Request-ID: req-8a4f2e1d9c7b6a3e
X-Correlation-ID: trace-abc-123-def-456
ETag: "5e7d7a5e8b4c9d2f1a3e6b8c4d9f2e1a"
Cache-Control: public, max-age=300
```

### 4. Pagination Metadata

**Automatically extracts and documents pagination metadata:**

**Your Controller:**
```csharp
[HttpGet]
public async Task<PagedResult<Product>> GetProducts(int page = 1, int pageSize = 20)
{
    return await _productService.GetPagedAsync(page, pageSize);
}
```

**Swagger Response Schema:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Product 1",
        "price": 29.99
      }
    ]
  },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 45,
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalPages": 5,
      "totalItems": 93,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  }
}
```

### 5. Detailed Property Descriptions

**All properties include helpful descriptions:**

**ApiResponse Properties:**
- `success` - Indicates whether the request was successful
- `data` - The response data returned by the endpoint
- `message` - Optional message providing additional context
- `statusCode` - Application-specific status code for complex workflow handling
- `errors` - List of error messages (populated on failures)
- `metadata` - Metadata about the request and response

**ResponseMetadata Properties:**
- `requestId` - Unique identifier for this request
- `timestamp` - ISO 8601 timestamp when the response was generated
- `executionTimeMs` - Time taken to execute the request in milliseconds
- `version` - API version
- `correlationId` - Correlation ID for distributed tracing
- `path` - Request path
- `method` - HTTP method used
- `pagination` - Pagination information (if applicable)
- `additional` - Additional custom metadata

**PaginationMetadata Properties:**
- `page` - Current page number (1-based)
- `pageSize` - Number of items per page
- `totalPages` - Total number of pages
- `totalItems` - Total number of items across all pages
- `hasNextPage` - Indicates if there is a next page
- `hasPreviousPage` - Indicates if there is a previous page

## Configuration Options

### Swashbuckle Options

```csharp
builder.Services.AddResponseWrapperOpenApi(options =>
{
    // Include example responses (default: true)
    options.IncludeExamples = true;

    // Include error response examples (default: true)
    options.IncludeErrorExamples = true;

    // Include metadata schema documentation (default: true)
    options.IncludeMetadataSchema = true;

    // Automatically wrap all response types (default: true)
    options.AutoWrapResponses = true;

    // Exclude specific status codes from wrapping
    options.ExcludedStatusCodes = new HashSet<int> { 204, 301, 302 };
});
```

### NSwag Options

```csharp
builder.Services.AddResponseWrapperNSwag(options =>
{
    options.AutoWrapResponses = true;
    options.IncludeErrorExamples = true;
    options.IncludeMetadataSchema = true;
});
```

### Scalar Options

```csharp
builder.Services.AddResponseWrapperScalar(options =>
{
    options.AutoWrapResponses = true;
    options.IncludeMetadataSchema = true;
});
```

## Advanced Scenarios

### Custom Response Examples

**Add custom examples to your endpoints:**

```csharp
using Swashbuckle.AspNetCore.Annotations;

[HttpGet("{id}")]
[SwaggerResponse(200, "User found", typeof(User))]
[SwaggerResponse(404, "User not found")]
public async Task<User> GetUser(int id)
{
    var user = await _userService.GetAsync(id);
    if (user == null)
        throw new NotFoundException("User", id);
    return user;
}
```

**The OpenAPI integration will automatically wrap these examples!**

### Excluding Specific Endpoints

**Option 1: Using attribute**
```csharp
[HttpGet("raw")]
[SkipApiResponseWrapper] // This endpoint won't show wrapped format in docs
public User GetRawUser(int id)
{
    return _userService.Get(id);
}
```

**Option 2: Using configuration**
```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.ExcludedPaths = new[] { "/api/raw", "/api/legacy" };
});
```

### Status Code-Specific Configuration

**Exclude specific status codes from wrapping:**

```csharp
builder.Services.AddResponseWrapperOpenApi(options =>
{
    // Don't wrap these status codes
    options.ExcludedStatusCodes = new HashSet<int>
    {
        204, // No Content
        301, // Moved Permanently
        302, // Found (Redirect)
        304  // Not Modified
    };
});
```

### Custom Error Messages in Documentation

**Configure custom error messages that appear in OpenAPI docs:**

```csharp
builder.Services.AddResponseWrapper(
    options => { },
    errorMessages =>
    {
        errorMessages.ValidationErrorMessage = "Your custom validation message";
        errorMessages.NotFoundErrorMessage = "Your custom not found message";
        errorMessages.UnauthorizedAccessMessage = "Your custom unauthorized message";
    });
```

**These messages will appear in the error response examples in Swagger!**

## Integration with XML Comments

**Enable XML documentation comments:**

**Step 1: Enable XML documentation in your project**

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

**Step 2: Add XML comments to your controllers**

```csharp
/// <summary>
/// Gets a user by their unique identifier
/// </summary>
/// <param name="id">The unique identifier of the user</param>
/// <returns>The user with the specified ID</returns>
/// <response code="200">User found and returned successfully</response>
/// <response code="404">User with the specified ID was not found</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(User), 200)]
[ProducesResponseType(404)]
public async Task<User> GetUser(int id)
{
    var user = await _userService.GetAsync(id);
    if (user == null)
        throw new NotFoundException("User", id);
    return user;
}
```

**Step 3: Configure Swagger to use XML comments**

```csharp
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
```

**The ResponseWrapper OpenAPI integration will automatically preserve and enhance these comments!**

## Comparison: Before vs After

### Before ResponseWrapper OpenAPI Integration

**Swagger shows:**
- Raw return types from controllers
- No standardized error responses
- No metadata documentation
- Inconsistent response formats

**Example:**
```json
// GET /api/users/1
{
  "id": 1,
  "name": "John"
}

// GET /api/products?page=1
{
  "items": [...],
  "totalCount": 100
}

// Error responses completely undocumented
```

### After ResponseWrapper OpenAPI Integration

**Swagger shows:**
- Standardized wrapped responses
- Complete error response documentation
- Metadata schemas with descriptions
- Pagination metadata structure
- Response headers
- Consistent format across all endpoints

**Example:**
```json
// All endpoints follow the same structure
{
  "success": true,
  "data": { ... },
  "metadata": {
    "requestId": "...",
    "timestamp": "...",
    "executionTimeMs": 42,
    "pagination": { ... }
  }
}

// Error responses fully documented
{
  "success": false,
  "data": null,
  "message": "...",
  "statusCode": "...",
  "errors": [...],
  "metadata": { ... }
}
```

## Client Code Generation

### TypeScript Client

**With ResponseWrapper OpenAPI integration, generated clients automatically include the wrapped type:**

```typescript
// Generated TypeScript client
interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message?: string;
  statusCode?: string;
  errors?: string[];
  metadata: ResponseMetadata;
}

interface ResponseMetadata {
  requestId: string;
  timestamp: string;
  executionTimeMs: number;
  version?: string;
  correlationId?: string;
  path?: string;
  method?: string;
  pagination?: PaginationMetadata;
  additional?: Record<string, any>;
}

interface PaginationMetadata {
  page: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Usage
const response = await api.getUser(1);
if (response.success) {
  console.log(response.data); // Strongly typed!
  console.log(response.metadata.executionTimeMs);
  console.log(response.metadata.requestId);
} else {
  console.error(response.errors);
}
```

### C# Client (NSwag)

```csharp
// Generated C# client
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public string StatusCode { get; set; }
    public List<string> Errors { get; set; }
    public ResponseMetadata Metadata { get; set; }
}

// Usage
var response = await client.GetUserAsync(1);
if (response.Success)
{
    var user = response.Data;
    var requestId = response.Metadata.RequestId;
}
else
{
    foreach (var error in response.Errors)
    {
        Console.WriteLine(error);
    }
}
```

## Troubleshooting

### Swagger Not Showing Wrapped Format

**Problem:** Swagger still shows raw response types.

**Solutions:**

1. **Ensure integration package is installed:**
   ```bash
   dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle
   ```

2. **Verify configuration:**
   ```csharp
   builder.Services.AddSwaggerGen();
   builder.Services.AddResponseWrapperOpenApi(); // Must be called!
   ```

3. **Check service registration order:**
   ```csharp
   // ✅ Correct order
   builder.Services.AddSwaggerGen();
   builder.Services.AddResponseWrapperOpenApi();

   // ❌ Wrong order
   builder.Services.AddResponseWrapperOpenApi();
   builder.Services.AddSwaggerGen(); // Too late!
   ```

### Duplicate Properties in Swagger

**Problem:** Properties appear in both `data` and `metadata`.

**Explanation:** This is by design for clarity:
- `statusCode` and `message` from DTOs implementing `IHasStatusCode`/`IHasMessage` are promoted to top level
- Original properties remain in `data` in OpenAPI docs for transparency
- At runtime, they're removed from `data` to avoid duplication

**If you want to hide them in docs:**
```csharp
public class MyResult : IHasStatusCode
{
    [JsonIgnore] // Hide in both docs and runtime
    public string StatusCode { get; set; } = string.Empty;
}
```

### Error Examples Not Showing

**Problem:** Error response examples missing in Swagger.

**Solution:** Enable error examples:
```csharp
builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.IncludeErrorExamples = true; // Ensure this is true
});
```

### Response Headers Missing

**Problem:** X-Request-ID and other headers not documented.

**Solution:** Headers are automatically added. Ensure you're using the enhanced OpenAPI package:
```csharp
// This package includes header documentation
dotnet add package FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle
```

## Best Practices

1. **Always use XML comments** for better documentation
2. **Use ProducesResponseType** to document status codes
3. **Enable examples** for better developer experience
4. **Document all error cases** in XML comments
5. **Use consistent error messages** in configuration
6. **Test generated clients** to ensure type safety
7. **Keep OpenAPI packages updated** for latest features

## Examples

### Complete Swagger Configuration

```csharp
using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Configure Swagger with full options
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API with ResponseWrapper integration",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Add authorization
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Add ResponseWrapper OpenAPI integration
builder.Services.AddResponseWrapperOpenApi(options =>
{
    options.AutoWrapResponses = true;
    options.IncludeExamples = true;
    options.IncludeErrorExamples = true;
    options.IncludeMetadataSchema = true;
    options.ExcludedStatusCodes = new HashSet<int> { 204, 304 };
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = string.Empty; // Serve Swagger at root
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

[← Back to Enterprise Overview](README.md) | [Next: OpenTelemetry →](telemetry.md)
