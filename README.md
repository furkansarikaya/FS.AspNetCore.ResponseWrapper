# FS.AspNetCore.ResponseWrapper

[![NuGet Version](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Automatic API response wrapping with metadata injection for ASP.NET Core applications.**

FS.AspNetCore.ResponseWrapper provides a consistent, standardized response format for your ASP.NET Core APIs with zero boilerplate code. Transform your raw controller responses into rich, metadata-enhanced API responses that include execution timing, pagination details, correlation IDs, and comprehensive error handling.

## 🎯 Why ResponseWrapper?

Building robust APIs means handling consistent response formats, error management, timing information, and pagination metadata. Without a standardized approach, you end up with:

- **Inconsistent Response Formats**: Different endpoints returning data in different structures
- **Manual Error Handling**: Writing repetitive error response logic in every controller
- **Missing Metadata**: No execution timing, correlation IDs, or request tracking
- **Complex Pagination**: Mixing business data with pagination information
- **Debugging Difficulties**: Limited insight into request processing and performance

ResponseWrapper solves all these challenges by automatically wrapping your API responses with a consistent structure, comprehensive metadata, and intelligent error handling.

## ✨ Key Features

### 🔄 Automatic Response Wrapping
Transform any controller response into a standardized format without changing your existing code.

### ⏱️ Performance Monitoring
Built-in execution time tracking and database query statistics for performance optimization.

### 🔍 Request Tracing
Automatic correlation ID generation and tracking for distributed systems debugging.

### 📄 Smart Pagination
Automatic detection and clean separation of pagination metadata from business data using duck typing.

### 🚨 Comprehensive Error Handling
Global exception handling with customizable error messages and consistent error response format.

### 🎛️ Flexible Configuration
Extensive configuration options for customizing behavior, excluding specific endpoints, and controlling metadata generation.

### 🦆 Duck Typing Support
Works with ANY pagination implementation - no need to change existing pagination interfaces.

## 📦 Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package FS.AspNetCore.ResponseWrapper
```

Or via Package Manager Console:

```powershell
Install-Package FS.AspNetCore.ResponseWrapper
```

Or add directly to your `.csproj` file:

```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="9.0.0" />
```

## 🚀 Quick Start

Getting started with ResponseWrapper is incredibly simple. Add it to your ASP.NET Core application in just two steps:

### Step 1: Register ResponseWrapper Services

In your `Program.cs` file, add ResponseWrapper to your service collection:

```csharp
using FS.AspNetCore.ResponseWrapper;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add ResponseWrapper with default configuration
builder.Services.AddResponseWrapper();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Step 2: Add Global Exception Handling (Optional but Recommended)

For comprehensive error handling, add the middleware:

```csharp
var app = builder.Build();

// Add ResponseWrapper middleware for global exception handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

That's it! Your API responses are now automatically wrapped. Let's see what this means in practice.

## 📊 Before and After

### Before: Raw Controller Response
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<List<User>> GetUsers()
    {
        return await _userService.GetUsersAsync();
    }
}
```

**Raw Response:**
```json
[
  {"id": 1, "name": "John Doe", "email": "john@example.com"},
  {"id": 2, "name": "Jane Smith", "email": "jane@example.com"}
]
```

### After: ResponseWrapper Enhanced
**Same Controller Code** - No changes needed!

**Enhanced Response:**
```json
{
  "success": true,
  "data": [
    {"id": 1, "name": "John Doe", "email": "john@example.com"},
    {"id": 2, "name": "Jane Smith", "email": "jane@example.com"}
  ],
  "message": null,
  "errors": [],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-15T10:30:45.123Z",
    "executionTimeMs": 42,
    "version": "1.0",
    "correlationId": "abc123",
    "path": "/api/users",
    "method": "GET",
    "additional": {
      "requestSizeBytes": 0,
      "clientIP": "192.168.1.1"
    }
  }
}
```

## 🎛️ Configuration Options

ResponseWrapper provides extensive configuration options to customize behavior according to your needs.

### Basic Configuration

```csharp
builder.Services.AddResponseWrapper(options =>
{
    // Enable/disable execution time tracking
    options.EnableExecutionTimeTracking = true;
    
    // Enable/disable pagination metadata extraction
    options.EnablePaginationMetadata = true;
    
    // Enable/disable correlation ID tracking
    options.EnableCorrelationId = true;
    
    // Enable/disable database query statistics (requires EF interceptors)
    options.EnableQueryStatistics = false;
    
    // Control which responses to wrap
    options.WrapSuccessResponses = true;
    options.WrapErrorResponses = true;
    
    // Exclude specific paths from wrapping
    options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger" };
    
    // Exclude specific result types from wrapping
    options.ExcludedTypes = new[] { typeof(FileResult), typeof(RedirectResult) };
});
```

### Advanced Configuration with Custom Error Messages

```csharp
builder.Services.AddResponseWrapper(
    options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = true;
        options.ExcludedPaths = new[] { "/health", "/metrics" };
    },
    errorMessages =>
    {
        errorMessages.ValidationErrorMessage = "Please check your input and try again";
        errorMessages.NotFoundErrorMessage = "The requested item could not be found";
        errorMessages.UnauthorizedAccessMessage = "Please log in to access this resource";
        errorMessages.ForbiddenAccessMessage = "You don't have permission to access this resource";
        errorMessages.BusinessRuleViolationMessage = "This operation violates business rules";
        errorMessages.ApplicationErrorMessage = "We're experiencing technical difficulties";
        errorMessages.UnexpectedErrorMessage = "An unexpected error occurred. Our team has been notified";
    });
```

### Expert Configuration with Custom Dependencies

```csharp
builder.Services.AddResponseWrapper<CustomApiLogger>(
    dateTimeProvider: () => DateTime.UtcNow, // Custom time provider for testing
    configureOptions: options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = true;
    },
    configureErrorMessages: errorMessages =>
    {
        errorMessages.ValidationErrorMessage = "Custom validation message";
        errorMessages.NotFoundErrorMessage = "Custom not found message";
    });
```

## 🚨 Error Handling

ResponseWrapper provides comprehensive error handling that transforms exceptions into consistent API responses.

### Built-in Exception Types

ResponseWrapper includes several exception types for common scenarios:

```csharp
// For validation errors
throw new ValidationException(validationFailures);

// For missing resources
throw new NotFoundException("User", userId);

// For business rule violations
throw new BusinessException("Insufficient inventory for this order");

// For authorization failures
throw new ForbiddenAccessException("Access denied to this resource");
```

### Exception Response Examples

**Validation Error Response:**
```json
{
  "success": false,
  "data": null,
  "message": "Please check your input and try again",
  "errors": [
    "Email is required",
    "Password must be at least 8 characters"
  ],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2024-01-15T10:30:45.123Z",
    "executionTimeMs": 15,
    "path": "/api/users",
    "method": "POST"
  }
}
```

**Not Found Error Response:**
```json
{
  "success": false,
  "data": null,
  "message": "The requested item could not be found",
  "errors": ["User (123) was not found."],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440001",
    "timestamp": "2024-01-15T10:32:15.456Z",
    "executionTimeMs": 8,
    "path": "/api/users/123",
    "method": "GET"
  }
}
```

### Custom Error Messages

Customize error messages for different environments or languages:

```csharp
// English messages
errorMessages.ValidationErrorMessage = "Please check your input and try again";
errorMessages.NotFoundErrorMessage = "The requested item could not be found";

// Turkish messages
errorMessages.ValidationErrorMessage = "Lütfen girdiğiniz bilgileri kontrol edin";
errorMessages.NotFoundErrorMessage = "Aradığınız öğe bulunamadı";

// Developer-friendly messages for development environment
if (environment.IsDevelopment())
{
    errorMessages.ValidationErrorMessage = "Validation failed - check detailed errors";
    errorMessages.ApplicationErrorMessage = "Application error - check logs for stack trace";
}
```

## 📄 Pagination Support

One of ResponseWrapper's most powerful features is its intelligent pagination handling using duck typing.

### The Problem with Traditional Pagination

Most pagination libraries mix business data with pagination metadata:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalItems": 47
}
```

This creates inconsistent API responses and makes client development more complex.

### ResponseWrapper's Solution: Clean Separation

ResponseWrapper automatically detects pagination objects and separates business data from pagination metadata:

**Clean Response with Separated Metadata:**
```json
{
  "success": true,
  "data": {
    "items": [
      {"id": 1, "name": "Product 1"},
      {"id": 2, "name": "Product 2"}
    ]
  },
  "metadata": {
    "pagination": {
      "page": 1,
      "pageSize": 10,
      "totalPages": 5,
      "totalItems": 47,
      "hasNextPage": true,
      "hasPreviousPage": false
    },
    "requestId": "...",
    "executionTimeMs": 25
  }
}
```

### Duck Typing: Works with ANY Pagination Library

ResponseWrapper uses duck typing, which means it works with ANY pagination implementation that has the required properties. You don't need to change your existing code!

**Works with your existing pagination classes:**

```csharp
// Your existing pagination class - no changes needed!
public class MyCustomPagedResult<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// Your controller - no changes needed!
[HttpGet]
public async Task<MyCustomPagedResult<Product>> GetProducts()
{
    return await _productService.GetPagedProductsAsync();
}
```

**Also works with third-party libraries:**

```csharp
// Works with EntityFramework extensions
public async Task<PagedList<User>> GetUsers()
{
    return await context.Users.ToPagedListAsync(page, pageSize);
}

// Works with any library that follows the pagination pattern
public async Task<PaginatedResult<Order>> GetOrders()
{
    return await _orderService.GetPaginatedOrdersAsync();
}
```

### Supported Pagination Patterns

ResponseWrapper automatically detects any object with these properties:

- `Items` (List<T>) - The business data
- `Page` (int) - Current page number
- `PageSize` (int) - Items per page
- `TotalPages` (int) - Total number of pages
- `TotalItems` (int) - Total number of items
- `HasNextPage` (bool) - Whether next page exists
- `HasPreviousPage` (bool) - Whether previous page exists

## 🎯 Real-World Usage Examples

### E-Commerce API Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // Simple product listing - automatically wrapped
    [HttpGet]
    public async Task<List<Product>> GetProducts()
    {
        return await _productService.GetActiveProductsAsync();
    }

    // Paginated products - pagination metadata automatically extracted
    [HttpGet("paged")]
    public async Task<PagedResult<Product>> GetPagedProducts(int page = 1, int pageSize = 10)
    {
        return await _productService.GetPagedProductsAsync(page, pageSize);
    }

    // Product creation - automatically wrapped with 201 status
    [HttpPost]
    public async Task<Product> CreateProduct(CreateProductRequest request)
    {
        // Validation happens automatically via ValidationException
        if (!ModelState.IsValid)
        {
            var failures = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new ValidationFailure("", e.ErrorMessage));
            throw new ValidationException(failures);
        }

        return await _productService.CreateProductAsync(request);
    }

    // Product by ID - automatic 404 handling
    [HttpGet("{id}")]
    public async Task<Product> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        
        // This automatically becomes a 404 with proper error structure
        if (product == null)
            throw new NotFoundException("Product", id);
            
        return product;
    }

    // File download - automatically excluded from wrapping
    [HttpGet("{id}/image")]
    public async Task<IActionResult> GetProductImage(int id)
    {
        var imageData = await _productService.GetProductImageAsync(id);
        return new CustomExportResult(imageData, "product.jpg", "image/jpeg");
    }

    // Exclude specific endpoint from wrapping
    [HttpGet("raw")]
    [SkipApiResponseWrapper("Legacy endpoint for backward compatibility")]
    public async Task<List<Product>> GetProductsRaw()
    {
        return await _productService.GetActiveProductsAsync();
    }
}
```

### User Management with Custom Business Logic

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("{id}/activate")]
    public async Task<User> ActivateUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            throw new NotFoundException("User", id);

        // Business rule validation
        if (user.IsActive)
            throw new BusinessException("User is already active");

        if (user.IsSuspended)
            throw new BusinessException("Cannot activate suspended user");

        return await _userService.ActivateUserAsync(id);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Authorization check
        if (!User.IsInRole("Admin"))
            throw new ForbiddenAccessException("Only administrators can delete users");

        await _userService.DeleteUserAsync(id);
        
        // Empty successful response
        return Ok();
    }
}
```

## 🔧 Advanced Scenarios

### Environment-Specific Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddResponseWrapper(
        options =>
        {
            options.EnableExecutionTimeTracking = true;
            options.EnablePaginationMetadata = true;
            
            // Enable detailed query stats only in development
            options.EnableQueryStatistics = Environment.IsDevelopment();
            
            // Different excluded paths per environment
            if (Environment.IsProduction())
            {
                options.ExcludedPaths = new[] { "/health" };
            }
            else
            {
                options.ExcludedPaths = new[] { "/health", "/swagger", "/debug" };
            }
        },
        errorMessages =>
        {
            if (Environment.IsDevelopment())
            {
                // Detailed messages for development
                errorMessages.ValidationErrorMessage = "Validation failed - check detailed error list";
                errorMessages.ApplicationErrorMessage = "Application error - check logs for full stack trace";
            }
            else
            {
                // User-friendly messages for production
                errorMessages.ValidationErrorMessage = "Please check your information and try again";
                errorMessages.ApplicationErrorMessage = "We're experiencing technical difficulties";
            }
        });
}
```

### Integration with Entity Framework for Query Statistics

```csharp
// Create a custom interceptor for query statistics
public class QueryStatisticsInterceptor : DbConnectionInterceptor
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
            var queryStats = GetOrCreateQueryStats(httpContext);
            queryStats["QueriesCount"] = (int)queryStats.GetValueOrDefault("QueriesCount", 0) + 1;
            
            var executedQueries = (List<string>)queryStats.GetValueOrDefault("ExecutedQueries", new List<string>());
            executedQueries.Add(command.CommandText);
            queryStats["ExecutedQueries"] = executedQueries.ToArray();
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

// Register the interceptor
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new QueryStatisticsInterceptor());
});
```

### Custom Logger Implementation

```csharp
public class CustomApiLogger : ILogger<ApiResponseWrapperFilter>
{
    private readonly ILogger<ApiResponseWrapperFilter> _innerLogger;
    private readonly IMetrics _metrics;

    public CustomApiLogger(ILogger<ApiResponseWrapperFilter> innerLogger, IMetrics metrics)
    {
        _innerLogger = innerLogger;
        _metrics = metrics;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        // Custom logging logic
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        
        // Send metrics
        if (logLevel >= LogLevel.Warning)
        {
            _metrics.Counter("api_errors").Increment();
        }
    }

    // Implement other ILogger methods...
}

// Register with ResponseWrapper
services.AddResponseWrapper<CustomApiLogger>(
    () => DateTime.UtcNow,
    options => options.EnableExecutionTimeTracking = true);
```

## 📋 Best Practices

### 1. Configure for Your Environment

```csharp
// Development: Enable everything for debugging
if (env.IsDevelopment())
{
    services.AddResponseWrapper(options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = true;
        options.EnablePaginationMetadata = true;
        options.EnableCorrelationId = true;
    });
}

// Production: Optimize for performance
if (env.IsProduction())
{
    services.AddResponseWrapper(options =>
    {
        options.EnableExecutionTimeTracking = true;
        options.EnableQueryStatistics = false; // Disable if not needed
        options.EnablePaginationMetadata = true;
        options.EnableCorrelationId = true;
        options.ExcludedPaths = new[] { "/health", "/metrics" };
    });
}
```

### 2. Use Specific Exception Types

```csharp
// Good: Specific exception types
if (user == null)
    throw new NotFoundException("User", id);

if (!user.IsActive)
    throw new BusinessException("User account is not active");

if (!User.IsInRole("Admin"))
    throw new ForbiddenAccessException("Administrator access required");

// Avoid: Generic exceptions
// throw new Exception("Something went wrong");
```

### 3. Leverage Custom Error Messages

```csharp
// Customize messages for better UX
errorMessages.ValidationErrorMessage = "Please review the highlighted fields and try again";
errorMessages.NotFoundErrorMessage = "We couldn't find what you're looking for";
errorMessages.UnauthorizedAccessMessage = "Please sign in to continue";
```

### 4. Exclude Appropriate Endpoints

```csharp
options.ExcludedPaths = new[]
{
    "/health",        // Health checks
    "/metrics",       // Metrics endpoints
    "/swagger",       // API documentation
    "/api/files",     // File download endpoints
    "/webhooks"       // Webhook endpoints
};
```

### 5. Monitor Performance Impact

```csharp
// Enable detailed monitoring in development
options.EnableQueryStatistics = Environment.IsDevelopment();

// Log execution times for performance monitoring
if (options.EnableExecutionTimeTracking)
{
    // Monitor slow requests
    if (executionTimeMs > 1000)
    {
        logger.LogWarning("Slow request detected: {RequestId} took {ExecutionTime}ms", 
            requestId, executionTimeMs);
    }
}
```

## 🐛 Troubleshooting

### Common Issues and Solutions

#### 1. Responses Not Being Wrapped

**Problem**: Some controller responses are not wrapped.

**Solutions**:
- Ensure controllers have the `[ApiController]` attribute
- Check that the endpoint is not in `ExcludedPaths`
- Verify the result type is not in `ExcludedTypes`
- Make sure `WrapSuccessResponses` is enabled

```csharp
[ApiController] // Required for automatic wrapping
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // This will be wrapped
    [HttpGet]
    public async Task<List<User>> GetUsers() { ... }
}
```

#### 2. Pagination Not Detected

**Problem**: Pagination metadata is not extracted from custom pagination classes.

**Solution**: Ensure your pagination class has all required properties:

```csharp
public class MyPagedResult<T>
{
    // All these properties are required
    public List<T> Items { get; set; }     // Required
    public int Page { get; set; }          // Required
    public int PageSize { get; set; }      // Required
    public int TotalPages { get; set; }    // Required
    public int TotalItems { get; set; }    // Required
    public bool HasNextPage { get; set; }  // Required
    public bool HasPreviousPage { get; set; } // Required
}
```

#### 3. Error Messages Not Showing

**Problem**: Custom error messages are not displayed.

**Solutions**:
- Ensure `GlobalExceptionHandlingMiddleware` is registered
- Add the middleware early in the pipeline
- Check that `WrapErrorResponses` is enabled

```csharp
var app = builder.Build();

// Add this EARLY in the pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

#### 4. Performance Issues

**Problem**: API responses are slower after adding ResponseWrapper.

**Solutions**:
- Disable query statistics if not needed: `options.EnableQueryStatistics = false`
- Exclude high-frequency endpoints: `options.ExcludedPaths = new[] { "/api/high-frequency" }`
- Disable execution time tracking for production: `options.EnableExecutionTimeTracking = false`

#### 5. File Downloads Being Wrapped

**Problem**: File download endpoints are returning JSON instead of files.

**Solutions**:
- Use `ISpecialResult` interface for custom file results
- Add file result types to `ExcludedTypes`
- Use the `[SkipApiResponseWrapper]` attribute

```csharp
// Option 1: Use ISpecialResult
public class FileDownloadResult : ActionResult, ISpecialResult { ... }

// Option 2: Exclude file types
options.ExcludedTypes = new[] { typeof(FileResult), typeof(FileStreamResult) };

// Option 3: Skip specific endpoints
[SkipApiResponseWrapper("File download endpoint")]
public async Task<IActionResult> DownloadFile(int id) { ... }
```

## 🤝 Contributing

We welcome contributions to FS.AspNetCore.ResponseWrapper! Here's how you can help:

### Reporting Issues

1. Check existing issues to avoid duplicates
2. Provide detailed reproduction steps
3. Include relevant code examples
4. Specify your .NET and ASP.NET Core versions

### Submitting Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes with tests
4. Ensure all tests pass
5. Update documentation if needed
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Development Setup

```bash
# Clone the repository
git clone https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper.git

# Navigate to the project directory
cd FS.AspNetCore.ResponseWrapper

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test
```

### Coding Standards

- Follow Microsoft's C# coding conventions
- Add comprehensive XML documentation for public APIs
- Write unit tests for new features
- Ensure backward compatibility when possible

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Microsoft for the excellent ASP.NET Core framework
- The open-source community for inspiration and feedback
- All contributors who help make this project better

## 📞 Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/issues)
- **NuGet Package**: [FS.AspNetCore.ResponseWrapper](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
- **Documentation**: [GitHub Repository](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper)

---

**Made with ❤️ by [Furkan Sarıkaya](https://github.com/furkansarikaya)**

*Transform your ASP.NET Core APIs with consistent, metadata-rich responses. Install FS.AspNetCore.ResponseWrapper today and experience the difference!*