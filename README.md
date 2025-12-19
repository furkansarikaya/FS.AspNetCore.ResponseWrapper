# FS.AspNetCore.ResponseWrapper

[![NuGet Version](https://img.shields.io/nuget/v/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![NuGet Downloads](https://img.shields.io/nuget/dt/FS.AspNetCore.ResponseWrapper.svg)](https://www.nuget.org/packages/FS.AspNetCore.ResponseWrapper)
[![GitHub License](https://img.shields.io/github/license/furkansarikaya/FS.AspNetCore.ResponseWrapper)](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/blob/main/LICENSE)
[![GitHub Stars](https://img.shields.io/github/stars/furkansarikaya/FS.AspNetCore.ResponseWrapper.svg)](https://github.com/furkansarikaya/FS.AspNetCore.ResponseWrapper/stargazers)

**Enterprise-grade API response wrapper for ASP.NET Core with zero code changes.**

FS.AspNetCore.ResponseWrapper provides a powerful, production-ready solution for standardizing API responses across your ASP.NET Core applications. Transform your raw controller responses into rich, metadata-enhanced API responses with automatic status code extraction, custom metadata injection, comprehensive error handling, and intelligent pagination support—all without modifying your existing code.

## 🎯 Why ResponseWrapper?

Building robust APIs requires consistent response formats, sophisticated error handling, request tracking, status code management, and pagination metadata. Without a standardized approach, you face:

- **Inconsistent Response Formats**: Different endpoints returning data in different structures
- **Manual Error Handling**: Writing repetitive error response logic in every controller
- **Missing Metadata**: No execution timing, correlation IDs, or request tracking
- **Complex Status Management**: Mixing HTTP status codes with application-specific workflow states
- **Pagination Chaos**: Mixing business data with pagination information across different libraries
- **Error Code Inconsistency**: No standardized error identification for client-side handling
- **Debugging Difficulties**: Limited insight into request processing and performance

ResponseWrapper solves all these challenges by automatically transforming your API responses with enterprise-grade features and zero boilerplate code.

## ✨ Key Features

### 🔄 Automatic Response Wrapping
Transform any controller response into a standardized format without changing your existing code. Supports all response types including single objects, collections, and paginated results.

### 📋 Comprehensive Response Structure
Every API response includes:
- **Success Indicator**: Clear boolean flag for operation outcome
- **Business Data**: Your actual response data, clean and focused
- **Status Codes**: Application-specific status codes for workflow management
- **Messages**: Human-readable messages for user communication
- **Error Details**: Structured error information with validation details
- **Rich Metadata**: Complete request/response lifecycle information

### 🏷️ Intelligent Status Code & Message Management
Automatic extraction and promotion of status codes and messages using three dedicated interfaces:

- **IHasStatusCode**: Automatically extracts and promotes status codes from response DTOs to the top-level API response
- **IHasMessage**: Automatically extracts and promotes messages from response DTOs for consistent user communication
- **IHasMetadata**: Enables custom metadata injection from your response objects into the metadata structure

This powerful trinity of interfaces enables:
- Complex workflow management beyond simple success/failure
- Rich client-side conditional logic and user experience flows
- Clean separation between business data and metadata
- Application-specific metadata that enhances client capabilities

### 🎨 Custom Metadata Support
Extend API responses with application-specific metadata using the **IHasMetadata** interface:
- Workflow state information
- Feature flags and permissions
- Business process indicators
- Any custom data your clients need
- Automatic merging with system metadata
- Intelligent conflict resolution

### ⏱️ Performance Monitoring
Built-in execution time tracking and database query statistics:
- Request execution timing
- Database query counts and execution times
- Cache hit/miss ratios
- Individual query tracking for debugging

### 🔍 Request Tracing
Automatic correlation ID generation and tracking:
- End-to-end request tracking across services
- Distributed system debugging support
- Log correlation and request flow analysis
- Custom or automatic correlation ID handling

### 📄 Universal Pagination Support
Automatic pagination detection using **duck typing** principles:
- Works with **ANY** pagination library or custom implementation
- No interface implementation required
- Automatic metadata extraction and separation
- Clean business data without pagination clutter
- Supports: PagedList, X.PagedList, custom pagination, third-party libraries

### 🚨 Comprehensive Error Handling
Global exception handling with rich error information:
- **12 Built-in Exception Types**: ValidationException, NotFoundException, BusinessException, ConflictException, UnauthorizedException, ForbiddenAccessException, BadRequestException, TimeoutException, TooManyRequestsException, ServiceUnavailableException, CustomHttpStatusException, ApplicationExceptionBase
- **Automatic Error Code Extraction**: Every exception provides machine-readable error codes
- **Error Code Promotion**: Error codes automatically promoted to ApiResponse level
- **Customizable Error Messages**: Full localization and branding support
- **Structured Error Responses**: Consistent error format across all endpoints
- **Validation Error Details**: Detailed property-level validation error information
- **ExposeMessage Support**: Selectively expose detailed exception messages with exception.Data["ExposeMessage"] = true
- **Security-First Design**: Hides sensitive error details by default, shows user-friendly messages

### 🎛️ Flexible Configuration
Extensive configuration options:
- Enable/disable specific features (execution tracking, pagination, correlation IDs)
- Exclude specific paths or response types
- Custom DateTime providers for testing
- Custom logger integration
- Separate success/error wrapping control
- Database query statistics integration

### 🦆 Duck Typing Philosophy
ResponseWrapper uses duck typing for maximum compatibility:
- **Pagination**: "If it has Page, PageSize, TotalPages, etc., it's paginated"
- **No Interface Requirements**: Works with your existing code
- **Library Agnostic**: Compatible with any pagination library
- **Reflection Caching**: High performance with minimal overhead

### 🛡️ Production-Ready
Built for enterprise applications:
- **.NET 10.0** support
- Minimal performance overhead
- Comprehensive logging
- Extensive error handling
- Defensive programming patterns
- Thread-safe operations
- NuGet package ready

## 📦 Installation

### Via .NET CLI
```bash
dotnet add package FS.AspNetCore.ResponseWrapper
```

### Via Package Manager Console
```powershell
Install-Package FS.AspNetCore.ResponseWrapper
```

### Via PackageReference
```xml
<PackageReference Include="FS.AspNetCore.ResponseWrapper" Version="10.0.1" />
```

**Requirements:**
- .NET 10.0 or later
- ASP.NET Core Web API project

## 🚀 Quick Start

### Step 1: Register ResponseWrapper Services

In your `Program.cs`:

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

### Step 2: Add Global Exception Handling (Recommended)

```csharp
var app = builder.Build();

// Add ResponseWrapper middleware for global exception handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Step 3: Start Using - No Code Changes Needed!

Your existing controllers work automatically:

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

**That's it!** Your responses are now automatically wrapped with comprehensive metadata.

## 📊 Response Structure

### Basic Response Example

**Your Controller:**
```csharp
[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    return await _userService.GetUserAsync(id);
}
```

**Wrapped Response:**
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
    "correlationId": "abc123",
    "path": "/api/users/1",
    "method": "GET",
    "additional": {
      "requestSizeBytes": 0,
      "clientIP": "192.168.1.1"
    }
  }
}
```

### Response with Status Code & Message

**Your Controller:**
```csharp
public class UserRegistrationResult : IHasStatusCode, IHasMessage
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string StatusCode { get; set; } = "EMAIL_VERIFICATION_REQUIRED";
    public string Message { get; set; } = "Account created successfully. Please verify your email.";
}

[HttpPost("register")]
public async Task<UserRegistrationResult> RegisterUser(RegisterRequest request)
{
    return await _userService.RegisterAsync(request);
}
```

**Wrapped Response:**
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "email": "user@example.com"
  },
  "statusCode": "EMAIL_VERIFICATION_REQUIRED",
  "message": "Account created successfully. Please verify your email.",
  "errors": null,
  "metadata": {
    "requestId": "...",
    "executionTimeMs": 58
  }
}
```

Notice how `statusCode` and `message` are automatically promoted to the top level while being removed from the data object for clean separation!

### Response with Custom Metadata

**Your Controller:**
```csharp
public class PaymentResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string TransactionId { get; set; }
    public decimal Amount { get; set; }

    public string StatusCode { get; set; } = "PAYMENT_PENDING";
    public string Message { get; set; } = "Payment is being processed";

    public Dictionary<string, object> Metadata { get; set; } = new()
    {
        { "paymentMethod", "credit_card" },
        { "processingTime", 3 },
        { "requiresConfirmation", true }
    };
}

[HttpPost("process-payment")]
public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
{
    return await _paymentService.ProcessAsync(request);
}
```

**Wrapped Response:**
```json
{
  "success": true,
  "data": {
    "transactionId": "TXN-12345",
    "amount": 99.99
  },
  "statusCode": "PAYMENT_PENDING",
  "message": "Payment is being processed",
  "metadata": {
    "requestId": "...",
    "executionTimeMs": 125,
    "additional": {
      "paymentMethod": "credit_card",
      "processingTime": 3,
      "requiresConfirmation": true,
      "clientIP": "192.168.1.1"
    }
  }
}
```

Custom metadata is automatically extracted and merged with system metadata!

### Paginated Response Example

**Your Controller:**
```csharp
// Works with ANY pagination implementation!
[HttpGet("paged")]
public async Task<PagedResult<Product>> GetProducts(int page = 1, int pageSize = 10)
{
    return await _productService.GetPagedProductsAsync(page, pageSize);
}
```

**Wrapped Response:**
```json
{
  "success": true,
  "data": {
    "items": [
      { "id": 1, "name": "Product 1" },
      { "id": 2, "name": "Product 2" }
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

Pagination metadata is automatically extracted and separated from business data!

### Error Response Example

**When Exception Occurs:**
```csharp
[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    var user = await _userService.GetUserAsync(id);
    if (user == null)
        throw new NotFoundException("User", id);
    return user;
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
    "requestId": "...",
    "timestamp": "2025-01-15T10:32:15.456Z",
    "executionTimeMs": 8,
    "path": "/api/users/123",
    "method": "GET"
  }
}
```

## 🎛️ Configuration

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

    // Enable/disable database query statistics
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
        errorMessages.UnexpectedErrorMessage = "An unexpected error occurred";
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
    });
```

## 🏷️ Interfaces for Metadata Extraction

ResponseWrapper provides three powerful interfaces for automatic metadata extraction:

### IHasStatusCode Interface

Automatically extracts and promotes status codes to the ApiResponse level:

```csharp
public interface IHasStatusCode
{
    string? StatusCode { get; }
}
```

**Usage Example:**
```csharp
public class LoginResult : IHasStatusCode
{
    public string Token { get; set; }
    public UserProfile User { get; set; }
    public string StatusCode { get; set; }
}

// Different status codes for different scenarios
[HttpPost("login")]
public async Task<LoginResult> Login(LoginRequest request)
{
    var result = await _authService.AuthenticateAsync(request);

    return result.Status switch
    {
        AuthStatus.Success => new LoginResult
        {
            Token = token,
            User = user,
            StatusCode = "LOGIN_SUCCESS"
        },
        AuthStatus.RequiresTwoFactor => new LoginResult
        {
            StatusCode = "TWO_FACTOR_REQUIRED"
        },
        AuthStatus.PasswordExpired => new LoginResult
        {
            StatusCode = "PASSWORD_EXPIRED"
        }
    };
}
```

**Client-Side Benefits:**
```typescript
const response = await api.post('/auth/login', credentials);

if (response.success) {
    switch (response.statusCode) {
        case 'LOGIN_SUCCESS':
            router.push('/dashboard');
            break;
        case 'TWO_FACTOR_REQUIRED':
            showTwoFactorDialog();
            break;
        case 'PASSWORD_EXPIRED':
            router.push('/change-password');
            break;
    }
}
```

### IHasMessage Interface

Automatically extracts and promotes messages to the ApiResponse level:

```csharp
public interface IHasMessage
{
    string? Message { get; }
}
```

**Usage Example:**
```csharp
public class OrderResult : IHasStatusCode, IHasMessage
{
    public string OrderId { get; set; }
    public string StatusCode { get; set; }
    public string Message { get; set; }
}

[HttpPost("orders")]
public async Task<OrderResult> CreateOrder(OrderRequest request)
{
    if (!await _inventoryService.IsAvailable(request.ProductId, request.Quantity))
    {
        return new OrderResult
        {
            StatusCode = "INSUFFICIENT_INVENTORY",
            Message = "Not enough items in stock. Please reduce quantity."
        };
    }

    var order = await _orderService.CreateAsync(request);
    return new OrderResult
    {
        OrderId = order.Id,
        StatusCode = "ORDER_CREATED",
        Message = "Your order has been placed successfully"
    };
}
```

### IHasMetadata Interface

Enables custom metadata injection:

```csharp
public interface IHasMetadata
{
    Dictionary<string, object>? Metadata { get; }
}
```

**Usage Example:**
```csharp
public class ProcessResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string ProcessId { get; set; }
    public string StatusCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

[HttpPost("process")]
public async Task<ProcessResult> StartProcess(ProcessRequest request)
{
    var result = await _processService.StartAsync(request);

    return new ProcessResult
    {
        ProcessId = result.Id,
        StatusCode = "PROCESS_STARTED",
        Message = "Process initiated successfully",
        Metadata = new Dictionary<string, object>
        {
            { "estimatedCompletionTime", result.EstimatedTime },
            { "priority", result.Priority },
            { "assignedWorker", result.Worker },
            { "canCancel", true }
        }
    };
}
```

**Combined Response:**
```json
{
  "success": true,
  "data": {
    "processId": "PROC-12345"
  },
  "statusCode": "PROCESS_STARTED",
  "message": "Process initiated successfully",
  "metadata": {
    "requestId": "...",
    "executionTimeMs": 45,
    "additional": {
      "estimatedCompletionTime": 120,
      "priority": "high",
      "assignedWorker": "worker-03",
      "canCancel": true,
      "clientIP": "192.168.1.1"
    }
  }
}
```

## 🚨 Exception Handling & Error Codes

ResponseWrapper includes comprehensive exception handling with automatic error code extraction and promotion.

### Built-in Exception Types

All exceptions derive from `ApplicationExceptionBase` which provides automatic error code support:

#### 1. ValidationException

For input validation errors with detailed property-level information:

```csharp
// With FluentValidation
var failures = validationResult.Errors;
throw new ValidationException(failures);

// With custom message
throw new ValidationException("Validation failed");

// With custom error code
throw new ValidationException("Invalid input", "CUSTOM_VALIDATION_ERROR");
```

**Response:**
```json
{
  "success": false,
  "statusCode": "VALIDATION_ERROR",
  "message": "Please check your input and try again",
  "errors": ["Email is required", "Password must be at least 8 characters"]
}
```

#### 2. NotFoundException

For missing resources:

```csharp
// Structured format
throw new NotFoundException("User", userId);

// Custom message
throw new NotFoundException("User account not found");

// Custom error code
throw new NotFoundException("Resource unavailable", "RESOURCE_ARCHIVED");
```

**Response (HTTP 404):**
```json
{
  "success": false,
  "statusCode": "NOT_FOUND",
  "message": "The requested item could not be found",
  "errors": ["User (123) was not found."]
}
```

#### 3. BusinessException

For business rule violations:

```csharp
// Basic usage
throw new BusinessException("Insufficient inventory for this order");

// With error code
throw new BusinessException(
    "Cannot complete transaction",
    "INSUFFICIENT_FUNDS"
);
```

**Response (HTTP 400):**
```json
{
  "success": false,
  "statusCode": "BUSINESS_RULE_VIOLATION",
  "message": "This operation violates business rules",
  "errors": ["Insufficient inventory for this order"]
}
```

#### 4. ConflictException

For resource conflicts:

```csharp
throw new ConflictException("Email address is already in use");
```

**Response (HTTP 409):**
```json
{
  "success": false,
  "statusCode": "CONFLICT",
  "errors": ["Email address is already in use"]
}
```

#### 5. UnauthorizedException

For authentication failures:

```csharp
throw new UnauthorizedException("Invalid credentials");
```

**Response (HTTP 401):**
```json
{
  "success": false,
  "statusCode": "UNAUTHORIZED",
  "message": "Authentication required"
}
```

#### 6. ForbiddenAccessException

For authorization failures:

```csharp
throw new ForbiddenAccessException("Admin access required");
```

**Response (HTTP 403):**
```json
{
  "success": false,
  "statusCode": "FORBIDDEN",
  "message": "Access forbidden"
}
```

#### 7. BadRequestException

For general bad requests:

```csharp
throw new BadRequestException("Invalid request format");
```

**Response (HTTP 400):**
```json
{
  "success": false,
  "statusCode": "BAD_REQUEST",
  "errors": ["Invalid request format"]
}
```

#### 8. TimeoutException

For timeout scenarios:

```csharp
throw new TimeoutException("Operation timed out");
```

**Response (HTTP 408):**
```json
{
  "success": false,
  "statusCode": "TIMEOUT",
  "errors": ["Operation timed out"]
}
```

#### 9. TooManyRequestsException

For rate limiting:

```csharp
throw new TooManyRequestsException("Rate limit exceeded");
```

**Response (HTTP 429):**
```json
{
  "success": false,
  "statusCode": "TOO_MANY_REQUESTS",
  "errors": ["Rate limit exceeded"]
}
```

#### 10. ServiceUnavailableException

For service unavailability:

```csharp
throw new ServiceUnavailableException("Database is temporarily unavailable");
```

**Response (HTTP 503):**
```json
{
  "success": false,
  "statusCode": "SERVICE_UNAVAILABLE",
  "errors": ["Database is temporarily unavailable"]
}
```

#### 11. CustomHttpStatusException

For any custom HTTP status code:

```csharp
// With auto-generated error code
throw new CustomHttpStatusException("Payment required", 402);

// With custom error code
throw new CustomHttpStatusException(
    "Resource locked",
    423,
    "RESOURCE_LOCKED"
);
```

**Response (HTTP 402/423):**
```json
{
  "success": false,
  "statusCode": "HTTP_402", // or "RESOURCE_LOCKED"
  "errors": ["Payment required"]
}
```

### Error Code Extraction System

ResponseWrapper automatically extracts and promotes error codes from ALL exception types:

1. **Custom Exception Codes**: From ApplicationExceptionBase.Code property
2. **Data Dictionary Codes**: From exception.Data["ErrorCode"]
3. **Fallback Codes**: Automatic codes based on exception type

```csharp
// Example: Error code extraction in action
public class CustomBusinessException : ApplicationExceptionBase
{
    public CustomBusinessException(string message, string code)
        : base(message, code) { }
}

throw new CustomBusinessException(
    "Custom business rule violated",
    "CUSTOM_BUSINESS_001"
);
```

**Response:**
```json
{
  "success": false,
  "statusCode": "CUSTOM_BUSINESS_001",
  "message": "This operation violates business rules",
  "errors": ["Custom business rule violated"]
}
```

### Customizing Error Messages

```csharp
builder.Services.AddResponseWrapper(
    options => { },
    errorMessages =>
    {
        // Customize for your application or locale
        errorMessages.ValidationErrorMessage = "Girdiğiniz bilgileri kontrol edin";
        errorMessages.NotFoundErrorMessage = "Aradığınız kayıt bulunamadı";
        errorMessages.UnauthorizedAccessMessage = "Giriş yapmanız gerekiyor";
        errorMessages.ForbiddenAccessMessage = "Bu işlem için yetkiniz bulunmuyor";
        errorMessages.BusinessRuleViolationMessage = "İş kuralları ihlali";
        errorMessages.ApplicationErrorMessage = "Teknik bir sorun oluştu";
        errorMessages.UnexpectedErrorMessage = "Beklenmeyen bir hata oluştu";
    });
```

### Exposing Exception Messages (ExposeMessage)

For security reasons, ResponseWrapper hides detailed exception messages from users by default and shows configured user-friendly messages instead. However, you can selectively expose specific exception messages using the `ExposeMessage` feature:

```csharp
// Example: Expose a specific exception message to users
public async Task<Product> ProcessOrder(int orderId)
{
    try
    {
        return await _orderService.ProcessAsync(orderId);
    }
    catch (Exception ex)
    {
        // Mark this exception message as safe to expose
        ex.Data["ExposeMessage"] = true;
        throw;
    }
}
```

**When ExposeMessage is true:**
- The actual exception message is shown to users
- HTTP 400 Bad Request is returned (instead of 500)
- The error is logged as information (not as error)

**When ExposeMessage is false (default):**
- Configured user-friendly error messages are shown
- HTTP 500 Internal Server Error is returned
- The error is logged as error level
- Actual exception details are hidden for security

**Use Cases:**
- **Development/Testing**: Expose detailed error information for debugging
- **Business Validation**: Show specific business rule violation messages
- **Third-Party Integrations**: Display detailed error messages from external APIs
- **User Input Errors**: Provide specific feedback about what went wrong

**Example with Business Logic:**
```csharp
public async Task<Order> PlaceOrder(OrderRequest request)
{
    var inventory = await _inventoryService.CheckAvailabilityAsync(request.ProductId);

    if (inventory.Quantity < request.Quantity)
    {
        var exception = new InvalidOperationException(
            $"Only {inventory.Quantity} items available. You requested {request.Quantity}."
        );

        // This is a business validation message safe to show users
        exception.Data["ExposeMessage"] = true;
        throw exception;
    }

    return await _orderService.CreateAsync(request);
}
```

**Response with ExposeMessage = true:**
```json
{
  "success": false,
  "statusCode": "INVALID_OPERATION",
  "message": "Only 5 items available. You requested 10.",
  "errors": ["Only 5 items available. You requested 10."],
  "metadata": {
    "requestId": "...",
    "executionTimeMs": 23
  }
}
```

**Response with ExposeMessage = false (default):**
```json
{
  "success": false,
  "statusCode": "INTERNAL_ERROR",
  "message": "An unexpected error occurred. Our team has been notified",
  "errors": ["An unexpected error occurred"],
  "metadata": {
    "requestId": "...",
    "executionTimeMs": 23
  }
}
```

**Security Note:**
⚠️ Only use `ExposeMessage = true` for exceptions that don't contain sensitive information like:
- Database connection strings
- Internal file paths
- Stack traces with code details
- Authentication credentials
- System configuration details

## 📄 Pagination Support

ResponseWrapper provides universal pagination support using **duck typing** - it works with ANY pagination implementation!

### How It Works

ResponseWrapper automatically detects pagination by looking for these properties:
- `Items` (List<T>)
- `Page` (int)
- `PageSize` (int)
- `TotalPages` (int)
- `TotalItems` (int)
- `HasNextPage` (bool)
- `HasPreviousPage` (bool)

**No interface implementation required!**

### Supported Pagination Libraries

Works with ALL of these (and more):

```csharp
// ✅ Your custom pagination class
public class MyPagedResult<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// ✅ Third-party libraries (X.PagedList, etc.)
public class PagedList<T>
{
    public List<T> Items { get; set; }
    // ... same properties
}

// ✅ Any class with the right properties!
```

### Usage Example

```csharp
[HttpGet("products")]
public async Task<MyPagedResult<Product>> GetProducts(int page = 1, int pageSize = 10)
{
    // Use ANY pagination library or custom implementation
    return await _productService.GetPagedProductsAsync(page, pageSize);
}
```

**Automatic Transformation:**

**Before (Your paginated object):**
```json
{
  "items": [{"id": 1, "name": "Product 1"}],
  "page": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalItems": 47,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

**After (ResponseWrapper transformation):**
```json
{
  "success": true,
  "data": {
    "items": [{"id": 1, "name": "Product 1"}]
  },
  "metadata": {
    "pagination": {
      "page": 1,
      "pageSize": 10,
      "totalPages": 5,
      "totalItems": 47,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  }
}
```

Clean separation of business data from pagination metadata!

### Performance Optimization

ResponseWrapper uses **reflection caching** for optimal performance:
- First request: Analyzes type structure
- Subsequent requests: Uses cached information
- Zero performance penalty after warm-up

## 🔧 Advanced Features

### Query Statistics Integration

Track database query performance:

```csharp
// Enable in configuration
options.EnableQueryStatistics = true;

// Create Entity Framework interceptor (example)
public class QueryStatisticsInterceptor : DbConnectionInterceptor
{
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        if (httpContext != null)
        {
            var queryStats = GetOrCreateQueryStats(httpContext);
            queryStats["QueriesCount"] = (int)queryStats.GetValueOrDefault("QueriesCount", 0) + 1;
        }
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

// Register interceptor
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new QueryStatisticsInterceptor());
});
```

**Response with Query Statistics:**
```json
{
  "metadata": {
    "query": {
      "databaseQueriesCount": 3,
      "databaseExecutionTimeMs": 45,
      "cacheHits": 5,
      "cacheMisses": 2
    }
  }
}
```

### Excluding Endpoints

#### Method 1: Using Attribute

```csharp
[SkipApiResponseWrapper("Legacy endpoint for backward compatibility")]
[HttpGet("raw")]
public async Task<List<Product>> GetProductsRaw()
{
    return await _productService.GetProductsAsync();
}
```

#### Method 2: Using Configuration

```csharp
options.ExcludedPaths = new[]
{
    "/health",
    "/metrics",
    "/swagger",
    "/api/legacy"
};
```

#### Method 3: Using Type Exclusion

```csharp
options.ExcludedTypes = new[]
{
    typeof(FileResult),
    typeof(RedirectResult),
    typeof(CustomStreamResult)
};
```

### Custom Export Results

For file downloads and special responses:

```csharp
public class CustomExportResult : ActionResult, ISpecialResult
{
    private readonly byte[] _data;
    private readonly string _fileName;
    private readonly string _contentType;

    public CustomExportResult(byte[] data, string fileName, string contentType)
    {
        _data = data;
        _fileName = fileName;
        _contentType = contentType;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.ContentType = _contentType;
        context.HttpContext.Response.Headers.Add(
            "Content-Disposition",
            $"attachment; filename={_fileName}"
        );
        await context.HttpContext.Response.Body.WriteAsync(_data);
    }
}

[HttpGet("{id}/export")]
public async Task<IActionResult> ExportData(int id)
{
    var data = await _service.ExportAsync(id);
    return new CustomExportResult(data, "export.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
}
```

### Environment-Specific Configuration

```csharp
builder.Services.AddResponseWrapper(
    options =>
    {
        options.EnableExecutionTimeTracking = true;

        // Enable detailed query stats only in development
        options.EnableQueryStatistics = env.IsDevelopment();

        // Different exclusions per environment
        options.ExcludedPaths = env.IsProduction()
            ? new[] { "/health" }
            : new[] { "/health", "/swagger", "/debug" };
    },
    errorMessages =>
    {
        if (env.IsDevelopment())
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
```

### Error Message Control with ExposeMessage

Control which exception messages are shown to users using the ExposeMessage feature:

```csharp
[HttpPost("transfer")]
public async Task<TransferResult> TransferFunds(TransferRequest request)
{
    try
    {
        // Validate business rules
        var account = await _accountService.GetAccountAsync(request.FromAccountId);

        if (account.Balance < request.Amount)
        {
            // This is a business validation message safe to expose
            var ex = new InvalidOperationException(
                $"Insufficient funds. Available: ${account.Balance}, Requested: ${request.Amount}"
            );
            ex.Data["ExposeMessage"] = true;
            throw ex;
        }

        // Process transfer
        return await _transferService.ExecuteAsync(request);
    }
    catch (DatabaseException dbEx)
    {
        // Database errors should NOT be exposed to users
        // ExposeMessage is false by default, so this will show generic error
        _logger.LogError(dbEx, "Database error during transfer");
        throw; // Will show: "An unexpected error occurred"
    }
}
```

**Development vs Production:**
```csharp
// In development, you might want to expose all errors for debugging
if (env.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            // Expose all errors in development
            ex.Data["ExposeMessage"] = true;
            throw;
        }
    });
}
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
// ✅ Good: Specific exception types with error codes
if (user == null)
    throw new NotFoundException("User", id);

if (!user.IsActive)
    throw new BusinessException("User account is not active", "ACCOUNT_INACTIVE");

if (!User.IsInRole("Admin"))
    throw new ForbiddenAccessException("Administrator access required");

// ❌ Avoid: Generic exceptions
// throw new Exception("Something went wrong");
```

### 3. Leverage Application Status Codes

```csharp
// ✅ Good: Rich status information
public class PaymentResult : IHasStatusCode, IHasMessage
{
    public string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string StatusCode { get; set; }
    public string Message { get; set; }
}

// Different status codes for different outcomes
switch (paymentResponse.Status)
{
    case PaymentStatus.Success:
        return new PaymentResult
        {
            StatusCode = "PAYMENT_SUCCESS",
            Message = "Payment processed successfully"
        };
    case PaymentStatus.InsufficientFunds:
        return new PaymentResult
        {
            StatusCode = "INSUFFICIENT_FUNDS",
            Message = "Insufficient funds in account"
        };
}
```

### 4. Use Custom Metadata for Enhanced UX

```csharp
// ✅ Good: Provide context for client decisions
public class OperationResult : IHasStatusCode, IHasMetadata
{
    public string ResultId { get; set; }
    public string StatusCode { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new()
    {
        { "canRetry", true },
        { "retryAfterSeconds", 30 },
        { "supportContact", "support@company.com" }
    };
}
```

### 5. Implement Pagination Consistently

```csharp
// ✅ Good: Consistent pagination across all endpoints
public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// Works automatically with ResponseWrapper!
[HttpGet]
public async Task<PagedResponse<Product>> GetProducts(int page = 1, int size = 20)
{
    return await _service.GetPagedAsync(page, size);
}
```

### 6. Monitor Performance Impact

```csharp
// Enable detailed monitoring in development
options.EnableQueryStatistics = Environment.IsDevelopment();

// Log slow requests
if (options.EnableExecutionTimeTracking)
{
    if (executionTimeMs > 1000)
    {
        logger.LogWarning(
            "Slow request detected: {RequestId} took {ExecutionTime}ms",
            requestId,
            executionTimeMs
        );
    }
}
```

### 7. Control Error Message Exposure

```csharp
// ✅ Good: Expose only safe business validation messages
public async Task ProcessPayment(PaymentRequest request)
{
    if (request.Amount <= 0)
    {
        var ex = new ArgumentException("Payment amount must be greater than zero");
        ex.Data["ExposeMessage"] = true; // Safe to show
        throw ex;
    }

    try
    {
        await _paymentGateway.ChargeAsync(request);
    }
    catch (PaymentGatewayException pgEx)
    {
        // Don't expose payment gateway errors (may contain sensitive info)
        _logger.LogError(pgEx, "Payment gateway error");
        throw new ServiceUnavailableException("Payment service is temporarily unavailable");
    }
}

// ❌ Avoid: Exposing sensitive system errors
public async Task GetUserData(int userId)
{
    try
    {
        return await _database.Query("SELECT * FROM Users WHERE Id = " + userId);
    }
    catch (Exception ex)
    {
        ex.Data["ExposeMessage"] = true; // ❌ BAD: May expose SQL injection details, connection strings
        throw;
    }
}

// ✅ Good: Use specific exceptions for business logic
if (order.Status == "Shipped")
{
    var ex = new BusinessException(
        "Cannot cancel order that has already been shipped",
        "ORDER_ALREADY_SHIPPED"
    );
    // No need for ExposeMessage - BusinessException messages are designed to be user-friendly
    throw ex;
}
```

## 🎯 Real-World Examples

### E-Commerce API

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    // Create order with workflow status
    [HttpPost]
    public async Task<OrderResult> CreateOrder(CreateOrderRequest request)
    {
        // Validation happens automatically via ValidationException
        if (!ModelState.IsValid)
        {
            var failures = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new ValidationFailure("", e.ErrorMessage));
            throw new ValidationException(failures);
        }

        var result = await _orderService.CreateOrderAsync(request);

        return new OrderResult
        {
            OrderId = result.Id,
            StatusCode = result.RequiresPayment ? "PAYMENT_REQUIRED" : "ORDER_CONFIRMED",
            Message = result.RequiresPayment
                ? "Please complete payment to process order"
                : "Order confirmed and will be shipped soon",
            Metadata = new Dictionary<string, object>
            {
                { "estimatedDelivery", result.EstimatedDeliveryDate },
                { "paymentUrl", result.PaymentUrl },
                { "canCancel", true }
            }
        };
    }

    // Get orders with pagination
    [HttpGet]
    public async Task<PagedResult<Order>> GetOrders(int page = 1, int pageSize = 20)
    {
        return await _orderService.GetPagedOrdersAsync(page, pageSize);
    }

    // Get order details
    [HttpGet("{id}")]
    public async Task<Order> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null)
            throw new NotFoundException("Order", id);
        return order;
    }

    // Cancel order with business rules
    [HttpPost("{id}/cancel")]
    public async Task<OrderResult> CancelOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null)
            throw new NotFoundException("Order", id);

        if (!order.CanBeCancelled)
            throw new BusinessException(
                "Order cannot be cancelled after shipping",
                "ORDER_ALREADY_SHIPPED"
            );

        await _orderService.CancelOrderAsync(id);

        return new OrderResult
        {
            OrderId = id,
            StatusCode = "ORDER_CANCELLED",
            Message = "Order has been cancelled successfully"
        };
    }

    // Export orders (excluded from wrapping)
    [HttpGet("export")]
    public async Task<IActionResult> ExportOrders()
    {
        var data = await _orderService.ExportOrdersAsync();
        return new CustomExportResult(data, "orders.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}

// DTOs
public class OrderResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string OrderId { get; set; }
    public string StatusCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Authentication API

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<LoginResult> Login(LoginRequest request)
    {
        var authResult = await _authService.AuthenticateAsync(request.Email, request.Password);

        return authResult.Status switch
        {
            AuthStatus.Success => new LoginResult
            {
                Token = authResult.Token,
                User = authResult.User,
                StatusCode = "LOGIN_SUCCESS",
                Message = "Welcome back!",
                Metadata = new Dictionary<string, object>
                {
                    { "tokenExpiresIn", 3600 },
                    { "refreshAvailable", true }
                }
            },
            AuthStatus.RequiresTwoFactor => new LoginResult
            {
                StatusCode = "TWO_FACTOR_REQUIRED",
                Message = "Please enter your authentication code",
                Metadata = new Dictionary<string, object>
                {
                    { "verificationMethod", "sms" },
                    { "canResend", true }
                }
            },
            AuthStatus.PasswordExpired => new LoginResult
            {
                StatusCode = "PASSWORD_EXPIRED",
                Message = "Your password has expired. Please update it.",
                Metadata = new Dictionary<string, object>
                {
                    { "mustChange", true }
                }
            },
            AuthStatus.AccountLocked => throw new ForbiddenAccessException(
                "Account is temporarily locked due to multiple failed login attempts"
            ),
            _ => throw new UnauthorizedException("Invalid credentials")
        };
    }

    [HttpPost("verify-2fa")]
    public async Task<LoginResult> VerifyTwoFactor(TwoFactorRequest request)
    {
        var result = await _authService.VerifyTwoFactorAsync(request.Code);

        if (!result.Success)
            throw new UnauthorizedException("Invalid verification code");

        return new LoginResult
        {
            Token = result.Token,
            User = result.User,
            StatusCode = "LOGIN_SUCCESS",
            Message = "Two-factor authentication successful"
        };
    }
}

public class LoginResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string Token { get; set; }
    public UserProfile User { get; set; }
    public string StatusCode { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

## 🐛 Troubleshooting

### Issue: Responses Not Being Wrapped

**Symptoms**: Some controller responses are not wrapped.

**Solutions**:
- ✅ Ensure controllers have the `[ApiController]` attribute
- ✅ Check that the endpoint is not in `ExcludedPaths`
- ✅ Verify the result type is not in `ExcludedTypes`
- ✅ Make sure `WrapSuccessResponses` is enabled
- ✅ Check for `[SkipApiResponseWrapper]` attribute

```csharp
[ApiController] // ✅ Required for automatic wrapping
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<List<User>> GetUsers() { ... }
}
```

### Issue: Pagination Not Detected

**Symptoms**: Pagination metadata is not extracted from custom pagination classes.

**Solution**: Ensure your pagination class has ALL required properties with correct types:

```csharp
public class MyPagedResult<T>
{
    // ✅ All these properties are required
    public List<T> Items { get; set; }     // Required
    public int Page { get; set; }          // Required (must be int)
    public int PageSize { get; set; }      // Required (must be int)
    public int TotalPages { get; set; }    // Required (must be int)
    public int TotalItems { get; set; }    // Required (must be int)
    public bool HasNextPage { get; set; }  // Required (must be bool)
    public bool HasPreviousPage { get; set; } // Required (must be bool)
}
```

### Issue: Status Codes Not Being Extracted

**Symptoms**: Application status codes are not appearing in responses.

**Solution**: Implement the `IHasStatusCode` interface:

```csharp
public class MyResponse : IHasStatusCode
{
    public string Data { get; set; }
    public string StatusCode { get; set; } // ✅ This will be promoted
}
```

### Issue: Custom Metadata Not Appearing

**Symptoms**: Custom metadata is not in the response.

**Solution**: Implement the `IHasMetadata` interface:

```csharp
public class MyResponse : IHasMetadata
{
    public string Data { get; set; }
    public Dictionary<string, object> Metadata { get; set; } // ✅ This will be merged
}
```

### Issue: Error Messages Not Showing

**Symptoms**: Custom error messages are not displayed.

**Solutions**:
- ✅ Ensure `GlobalExceptionHandlingMiddleware` is registered
- ✅ Add the middleware EARLY in the pipeline
- ✅ Check that `WrapErrorResponses` is enabled

```csharp
var app = builder.Build();

// ✅ Add this EARLY in the pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### Issue: File Downloads Being Wrapped

**Symptoms**: File download endpoints are returning JSON instead of files.

**Solutions**:

```csharp
// Option 1: Implement ISpecialResult
public class FileDownloadResult : ActionResult, ISpecialResult { ... }

// Option 2: Exclude file types
options.ExcludedTypes = new[] { typeof(FileResult), typeof(FileStreamResult) };

// Option 3: Use attribute
[SkipApiResponseWrapper("File download endpoint")]
public async Task<IActionResult> DownloadFile(int id) { ... }
```

## 🤝 Contributing

We welcome contributions! Here's how you can help:

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
5. Update documentation
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

*Transform your ASP.NET Core APIs with enterprise-grade response wrapping, intelligent status code management, universal pagination support, and comprehensive error handling. Install FS.AspNetCore.ResponseWrapper today and elevate your API development experience!*
