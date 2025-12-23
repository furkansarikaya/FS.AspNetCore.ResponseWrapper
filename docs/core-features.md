# Core Features

Deep dive into ResponseWrapper's core features and how to use them effectively.

## Table of Contents

- [Response Structure](#response-structure)
- [Smart Interfaces](#smart-interfaces)
  - [IHasStatusCode](#ihasstatuscode)
  - [IHasMessage](#ihasmessage)
  - [IHasMetadata](#ihasmetadata)
- [Exception Handling](#exception-handling)
- [Performance Monitoring](#performance-monitoring)
- [Request Tracing](#request-tracing)

## Response Structure

Every API response follows this standardized structure:

```json
{
  "success": boolean,           // true = success, false = error
  "data": object | null,        // Your business data
  "message": string | null,     // Optional human-readable message
  "statusCode": string | null,  // Application-specific status code
  "errors": string[] | null,    // Error messages (populated on failures)
  "metadata": {                 // Request metadata
    "requestId": "uuid",
    "timestamp": "ISO-8601",
    "executionTimeMs": number,
    "version": "string",
    "correlationId": "string",
    "path": "string",
    "method": "string",
    "pagination": {...},        // Only for paginated responses
    "additional": {...}         // Custom metadata
  }
}
```

### Success Response Example

```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com"
  },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 42,
    "version": "1.0",
    "path": "/api/users/1",
    "method": "GET"
  }
}
```

### Error Response Example

```json
{
  "success": false,
  "data": null,
  "message": "The requested item could not be found",
  "statusCode": "NOT_FOUND",
  "errors": ["User (123) was not found."],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:32:15.456Z",
    "executionTimeMs": 8,
    "path": "/api/users/123",
    "method": "GET"
  }
}
```

## Smart Interfaces

ResponseWrapper provides three powerful interfaces for automatic metadata extraction and promotion.

### IHasStatusCode

Automatically extracts and promotes status codes from your response DTOs to the top-level API response.

**Why use it?**
- Complex workflows need more than success/failure
- Client-side conditional logic based on application state
- Rich UX flows (2FA, email verification, password expiry, etc.)

**Interface:**
```csharp
public interface IHasStatusCode
{
    string? StatusCode { get; }
}
```

**Example: Authentication with Multiple Outcomes**

```csharp
public class LoginResult : IHasStatusCode
{
    public string? Token { get; set; }
    public UserProfile? User { get; set; }
    public string StatusCode { get; set; } = string.Empty;
}

[HttpPost("login")]
public async Task<LoginResult> Login(LoginRequest request)
{
    var result = await _authService.AuthenticateAsync(request);

    return result.Status switch
    {
        AuthStatus.Success => new LoginResult
        {
            Token = result.Token,
            User = result.User,
            StatusCode = "LOGIN_SUCCESS"
        },
        AuthStatus.RequiresTwoFactor => new LoginResult
        {
            StatusCode = "TWO_FACTOR_REQUIRED"
        },
        AuthStatus.PasswordExpired => new LoginResult
        {
            StatusCode = "PASSWORD_EXPIRED"
        },
        AuthStatus.AccountLocked => new LoginResult
        {
            StatusCode = "ACCOUNT_LOCKED"
        },
        _ => new LoginResult
        {
            StatusCode = "INVALID_CREDENTIALS"
        }
    };
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": null,
    "user": null
  },
  "statusCode": "TWO_FACTOR_REQUIRED",
  "metadata": {...}
}
```

**Client-side handling:**
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
        case 'ACCOUNT_LOCKED':
            showAccountLockedMessage();
            break;
    }
}
```

**Important:** The `statusCode` property is automatically removed from the `data` object and promoted to the top level.

### IHasMessage

Automatically extracts and promotes messages to provide user-friendly feedback.

**Interface:**
```csharp
public interface IHasMessage
{
    string? Message { get; }
}
```

**Example: Order Processing**

```csharp
public class OrderResult : IHasStatusCode, IHasMessage
{
    public string? OrderId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

[HttpPost("orders")]
public async Task<OrderResult> CreateOrder(CreateOrderRequest request)
{
    // Check inventory
    if (!await _inventoryService.IsAvailable(request.ProductId, request.Quantity))
    {
        return new OrderResult
        {
            StatusCode = "INSUFFICIENT_INVENTORY",
            Message = "Not enough items in stock. Please reduce quantity or try again later."
        };
    }

    // Create order
    var order = await _orderService.CreateAsync(request);

    return new OrderResult
    {
        OrderId = order.Id,
        StatusCode = "ORDER_CREATED",
        Message = "Your order has been placed successfully! You will receive a confirmation email shortly."
    };
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "orderId": "ORD-12345"
  },
  "statusCode": "ORDER_CREATED",
  "message": "Your order has been placed successfully! You will receive a confirmation email shortly.",
  "metadata": {...}
}
```

### IHasMetadata

Enables injection of custom metadata that gets merged with system metadata.

**Interface:**
```csharp
public interface IHasMetadata
{
    Dictionary<string, object>? Metadata { get; }
}
```

**Example: Complex Workflow with Custom Metadata**

```csharp
public class PaymentResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

[HttpPost("process-payment")]
public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
{
    var result = await _paymentService.ProcessAsync(request);

    return new PaymentResult
    {
        TransactionId = result.TransactionId,
        Amount = result.Amount,
        StatusCode = result.IsPending ? "PAYMENT_PENDING" : "PAYMENT_SUCCESS",
        Message = result.IsPending
            ? "Payment is being processed. You will be notified once completed."
            : "Payment processed successfully!",
        Metadata = new Dictionary<string, object>
        {
            { "paymentMethod", result.PaymentMethod },
            { "processingTime", result.ProcessingTimeSeconds },
            { "canRefund", result.IsRefundable },
            { "refundDeadline", result.RefundDeadline },
            { "requiresConfirmation", result.RequiresConfirmation },
            { "confirmationUrl", result.ConfirmationUrl }
        }
    };
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "transactionId": "TXN-789456",
    "amount": 99.99
  },
  "statusCode": "PAYMENT_PENDING",
  "message": "Payment is being processed. You will be notified once completed.",
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:40:12.789Z",
    "executionTimeMs": 234,
    "path": "/api/process-payment",
    "method": "POST",
    "additional": {
      "paymentMethod": "credit_card",
      "processingTime": 3,
      "canRefund": true,
      "refundDeadline": "2025-01-22T10:40:12.789Z",
      "requiresConfirmation": true,
      "confirmationUrl": "https://payment.example.com/confirm/abc123"
    }
  }
}
```

**Important:** Custom metadata is merged into `metadata.additional` and doesn't conflict with system metadata.

## Exception Handling

ResponseWrapper includes 12 built-in exception types with automatic error code extraction.

### Exception Types

| Exception | HTTP Status | Default Error Code | Use Case |
|-----------|------------|-------------------|----------|
| `ValidationException` | 400 | VALIDATION_ERROR | Input validation failures |
| `NotFoundException` | 404 | NOT_FOUND | Resource not found |
| `BusinessException` | 400 | BUSINESS_RULE_VIOLATION | Business rule violations |
| `ConflictException` | 409 | CONFLICT | Resource conflicts |
| `UnauthorizedException` | 401 | UNAUTHORIZED | Authentication required |
| `ForbiddenAccessException` | 403 | FORBIDDEN | Authorization failed |
| `BadRequestException` | 400 | BAD_REQUEST | Invalid requests |
| `TimeoutException` | 408 | TIMEOUT | Operation timeout |
| `TooManyRequestsException` | 429 | TOO_MANY_REQUESTS | Rate limiting |
| `ServiceUnavailableException` | 503 | SERVICE_UNAVAILABLE | Service down |
| `CustomHttpStatusException` | Custom | HTTP_{code} | Any custom status |
| `ApplicationExceptionBase` | 500 | INTERNAL_ERROR | Base for custom exceptions |

### Usage Examples

**ValidationException:**
```csharp
// Simple validation error
throw new ValidationException("Email is required");

// With custom error code
throw new ValidationException("Invalid format", "INVALID_EMAIL_FORMAT");

// With FluentValidation
var failures = validationResult.Errors;
throw new ValidationException(failures);
```

**NotFoundException:**
```csharp
// Structured format
throw new NotFoundException("User", userId);
// Produces: "User (123) was not found."

// Custom message
throw new NotFoundException("User account not found");

// With custom error code
throw new NotFoundException("Resource archived", "RESOURCE_ARCHIVED");
```

**BusinessException:**
```csharp
// Basic usage
throw new BusinessException("Insufficient inventory for this order");

// With error code
throw new BusinessException(
    "Cannot complete transaction",
    "INSUFFICIENT_FUNDS"
);
```

**Custom Exceptions:**
```csharp
public class PaymentFailedException : ApplicationExceptionBase
{
    public PaymentFailedException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}

// Usage
throw new PaymentFailedException(
    "Payment gateway declined the transaction",
    "PAYMENT_DECLINED"
);
```

### Error Code Extraction

ResponseWrapper automatically extracts error codes from exceptions in this order:

1. **Custom Code**: From `ApplicationExceptionBase.Code` property
2. **Data Dictionary**: From `exception.Data["ErrorCode"]`
3. **Fallback**: Automatic code based on exception type

```csharp
// Method 1: Custom exception with code
throw new BusinessException("Message", "CUSTOM_CODE");

// Method 2: Data dictionary
var ex = new InvalidOperationException("Message");
ex.Data["ErrorCode"] = "CUSTOM_CODE";
throw ex;

// Method 3: Fallback (automatic)
throw new NotFoundException("User", id);
// Auto code: "NOT_FOUND"
```

### ExposeMessage Feature

For security, exception messages are hidden by default. Use `ExposeMessage` to selectively expose safe messages:

```csharp
public async Task ProcessOrder(int orderId)
{
    try
    {
        return await _orderService.ProcessAsync(orderId);
    }
    catch (Exception ex)
    {
        // Mark this message as safe to expose
        ex.Data["ExposeMessage"] = true;
        throw;
    }
}
```

**With ExposeMessage = true:**
- HTTP 400 (not 500)
- Actual exception message shown
- Logged as Information (not Error)

**With ExposeMessage = false (default):**
- HTTP 500
- Generic error message shown
- Logged as Error
- Details hidden for security

## Performance Monitoring

Automatic execution time tracking for every request.

**Enabled by default** - tracks the time from request start to response completion.

```json
{
  "metadata": {
    "executionTimeMs": 42  // Milliseconds
  }
}
```

### Use Cases

**Monitoring slow endpoints:**
```csharp
// Log slow requests
if (executionTimeMs > 1000)
{
    _logger.LogWarning(
        "Slow request: {RequestId} took {ExecutionTime}ms",
        requestId,
        executionTimeMs
    );
}
```

**Performance alerts:**
```csharp
// Set up alerts for performance degradation
if (executionTimeMs > _config.PerformanceThresholdMs)
{
    await _monitoring.SendAlert($"API performance degraded: {executionTimeMs}ms");
}
```

### Disable if Needed

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableExecutionTimeTracking = false; // Disable if not needed
});
```

## Request Tracing

Automatic request ID and correlation ID generation for distributed tracing.

### Request ID

Unique identifier for each request:

```json
{
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

**Use for:**
- Log correlation
- Debugging specific requests
- Support ticket references

### Correlation ID

For tracking requests across multiple services:

```json
{
  "metadata": {
    "correlationId": "abc123-def456-ghi789"
  }
}
```

**How it works:**
1. Client sends `X-Correlation-ID` header
2. ResponseWrapper extracts and includes it
3. Use same ID when calling downstream services
4. Track entire request flow across microservices

**Example:**
```typescript
// Client-side
const correlationId = generateUUID();

await fetch('/api/users', {
    headers: {
        'X-Correlation-ID': correlationId
    }
});
```

```csharp
// Server-side (calling another service)
var correlationId = HttpContext.Request.Headers["X-Correlation-ID"];

await _httpClient.SendAsync(new HttpRequestMessage
{
    Headers = {
        { "X-Correlation-ID", correlationId }
    }
});
```

### Configure Correlation ID

```csharp
builder.Services.AddResponseWrapper(options =>
{
    options.EnableCorrelationId = true; // Enabled by default
});
```

---

[← Back to Getting Started](getting-started.md) | [Next: Pagination →](pagination.md)
