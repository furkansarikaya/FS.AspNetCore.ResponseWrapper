# FS.AspNetCore.ResponseWrapper

[![NuGet Version](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.AspNetCore.ResponseWrapper)](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.AspNetCore.ResponseWrapper.svg)](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/stargazers)

**Enterprise-grade API response wrapper for ASP.NET Core - standardize your APIs with zero code changes.**

Transform your raw controller responses into rich, metadata-enhanced API responses automatically. Add comprehensive error handling, request tracking, pagination support, and enterprise features without modifying existing code.

## 🎯 Why Use This?

**Problem**: Your API returns inconsistent response formats, lacks proper error handling, and provides no request tracking or metadata.

**Solution**: Add one NuGet package and 2 lines of code - get enterprise-grade API responses automatically.

```csharp
// Before
public async Task<User> GetUser(int id) => await _userService.GetUserAsync(id);

// After (no code changes needed!)
public async Task<User> GetUser(int id) => await _userService.GetUserAsync(id);
```

**Automatic transformation:**
```json
{
  "success": true,
  "data": { "id": 1, "name": "John Doe" },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 42,
    "path": "/api/users/1",
    "method": "GET"
  }
}
```

## ✨ Key Features

- ✅ **Zero Code Changes** - Works with existing controllers automatically
- 🔄 **Automatic Response Wrapping** - Consistent format for all endpoints
- 🚨 **Global Error Handling** - 12 built-in exception types with error codes
- ⏱️ **Performance Monitoring** - Automatic execution time tracking
- 📄 **Universal Pagination** - Works with ANY pagination library (duck typing)
- 🏷️ **Smart Metadata Extraction** - StatusCode, Message, and custom metadata
- 🔍 **Request Tracing** - Correlation IDs for distributed systems
- 🎛️ **Highly Configurable** - Control every aspect of wrapping behavior
- 🛡️ **Production-Ready** - .NET 10.0 support, minimal overhead

## 📦 Quick Start

### 1. Install Package

```bash
dotnet add package FS.AspNetCore.ResponseWrapper
```

### 2. Configure (2 lines of code)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper(); // 👈 Line 1

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // 👈 Line 2 (optional but recommended)
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3. That's It! 🎉

Your existing controllers now return wrapped responses automatically:

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        var user = await _userService.GetUserAsync(id);
        if (user == null)
            throw new NotFoundException("User", id);
        return user;
    }
}
```

**Success Response:**
```json
{
  "success": true,
  "data": { "id": 1, "name": "John Doe", "email": "john@example.com" },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 42
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "data": null,
  "message": "The requested item could not be found",
  "statusCode": "NOT_FOUND",
  "errors": ["User (123) was not found."],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:32:15.456Z"
  }
}
```

## 📚 Documentation

### Core Package
- 📖 [**Getting Started Guide**](docs/getting-started.md) - Step-by-step setup and basic usage
- 🔧 [**Core Features**](docs/core-features.md) - Response structure, interfaces, error handling
- 📄 [**Pagination Support**](docs/pagination.md) - Universal pagination with any library
- ⚙️ [**Configuration Guide**](docs/configuration.md) - All configuration options explained
- 💡 [**Examples**](docs/examples.md) - Real-world usage examples
- 🐛 [**Troubleshooting**](docs/troubleshooting.md) - Common issues and solutions

### Enterprise Extensions
- 🚀 [**Enterprise Features Overview**](docs/enterprise/README.md) - All enterprise packages
- 📊 [**OpenAPI Integration**](docs/enterprise/openapi.md) - Swagger, NSwag, Scalar support
- 📈 [**OpenTelemetry & Tracing**](docs/enterprise/telemetry.md) - Distributed tracing and metrics
- ⚡ [**Caching & Performance**](docs/enterprise/caching.md) - Memory, Redis, SQL Server caching
- 🔒 [**Data Transformation**](docs/enterprise/transformation.md) - Masking, field selection, GDPR
- 🎨 [**Preset Configurations**](docs/enterprise/presets.md) - Quick setup for common scenarios

## 🎯 Common Use Cases

### Basic API with Error Handling
```csharp
builder.Services.AddResponseWrapper();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

### API with Pagination
```csharp
// Works automatically with ANY pagination library!
[HttpGet]
public async Task<PagedResult<Product>> GetProducts(int page = 1)
    => await _service.GetPagedAsync(page, 20);
```

### API with Custom Status Codes
```csharp
public class LoginResult : IHasStatusCode, IHasMessage
{
    public string Token { get; set; }
    public string StatusCode { get; set; } = "LOGIN_SUCCESS";
    public string Message { get; set; } = "Welcome back!";
}
```

### Enterprise API with All Features
```csharp
// Install meta package
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions

// One-line setup
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");
app.UseResponseWrapperExtensions();
```

## 🏢 Enterprise Features

Install the all-in-one enterprise package:

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Extensions
```

**Includes:**
- 📊 **OpenAPI Integration** - Swashbuckle, NSwag, Scalar with enhanced schemas
- 📈 **OpenTelemetry** - W3C trace context, distributed tracing, custom metrics
- ⚡ **Caching** - Memory, Redis, SQL Server with ETag support
- 🔒 **Transformation** - Data masking (PII protection), field selection, GDPR compliance
- 🎨 **Presets** - 9 pre-configured setups (Minimal, Basic, Standard, Advanced, Enterprise, GDPR, Performance, Development, Production)

**Quick Enterprise Setup:**
```csharp
// One line gets you everything
builder.Services.AddResponseWrapperWithPreset(PresetType.Enterprise, "MyAPI");
app.UseResponseWrapperExtensions();
```

See [Enterprise Features Documentation](docs/enterprise/README.md) for details.

## 🎛️ Configuration Example

```csharp
builder.Services.AddResponseWrapper(options =>
{
    // Enable/disable features
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;

    // Exclude specific endpoints
    options.ExcludedPaths = new[] { "/health", "/metrics" };

    // Customize error messages
}, errorMessages =>
{
    errorMessages.ValidationErrorMessage = "Please check your input";
    errorMessages.NotFoundErrorMessage = "Item not found";
});
```

## 🚨 Built-in Exception Types

12 exception types with automatic error code extraction:

- `ValidationException` - Input validation errors (400)
- `NotFoundException` - Resource not found (404)
- `BusinessException` - Business rule violations (400)
- `ConflictException` - Resource conflicts (409)
- `UnauthorizedException` - Authentication required (401)
- `ForbiddenAccessException` - Authorization failed (403)
- `BadRequestException` - Invalid requests (400)
- `TimeoutException` - Operation timeout (408)
- `TooManyRequestsException` - Rate limiting (429)
- `ServiceUnavailableException` - Service down (503)
- `CustomHttpStatusException` - Any custom status code
- `ApplicationExceptionBase` - Base for custom exceptions

All exceptions include automatic error codes for client-side handling.

## 📊 Response Structure

```json
{
  "success": true,           // Operation outcome
  "data": { ... },           // Your business data
  "message": "...",          // Optional message
  "statusCode": "...",       // Application status code
  "errors": [...],           // Error messages (if any)
  "metadata": {              // Request metadata
    "requestId": "...",      // Unique request ID
    "timestamp": "...",      // Response timestamp
    "executionTimeMs": 42,   // Execution time
    "correlationId": "...",  // Distributed tracing
    "path": "/api/users",    // Request path
    "method": "GET",         // HTTP method
    "pagination": { ... },   // Pagination info (if applicable)
    "additional": { ... }    // Custom metadata
  }
}
```

## 🔧 Requirements

- .NET 10.0 or later
- ASP.NET Core Web API project

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details.

## 📞 Support & Links

- **Documentation**: [GitHub Repository](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper)
- **NuGet Package**: [FS.AspNetCore.ResponseWrapper](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
- **Issues**: [Report bugs or request features](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/issues)
- **Enterprise Package**: [FS.AspNetCore.ResponseWrapper.Extensions](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper.Extensions)

## 🌟 What's Next?

1. ✅ Install the package
2. 📖 Read the [Getting Started Guide](docs/getting-started.md)
3. 💡 Check out [Examples](docs/examples.md)
4. 🚀 Explore [Enterprise Features](docs/enterprise/README.md)

---

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**

*Transform your ASP.NET Core APIs with enterprise-grade response wrapping. Install today and elevate your API development!*
