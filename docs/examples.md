# Real-World Examples

Practical examples showing ResponseWrapper in real-world scenarios.

## Table of Contents

- [E-Commerce API](#e-commerce-api)
- [Authentication & Authorization](#authentication--authorization)
- [Multi-Tenant SaaS](#multi-tenant-saas)
- [File Upload & Processing](#file-upload--processing)
- [Payment Processing](#payment-processing)
- [Microservices Integration](#microservices-integration)
- [Webhook Handling](#webhook-handling)

## E-Commerce API

Complete example of a product catalog API with pagination, filtering, and inventory management.

### Models

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

public class UpdateStockRequest
{
    public int Quantity { get; set; }
}
```

### Controller

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of products with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<PagedResult<Product>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? inStock = null)
    {
        // Validate pagination parameters
        if (page < 1)
            throw new ValidationException("Page must be greater than 0", "INVALID_PAGE");

        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");

        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        if (inStock.HasValue && inStock.Value)
            query = query.Where(p => p.StockQuantity > 0);

        // Get total count
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        // Get page data
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalItems = totalItems,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<Product> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            throw new NotFoundException("Product", id);

        return product;
    }

    /// <summary>
    /// Create new product
    /// </summary>
    [HttpPost]
    public async Task<Product> CreateProduct(CreateProductRequest request)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Product name is required");

        if (request.Price <= 0)
            throw new ValidationException("Price must be greater than 0", "INVALID_PRICE");

        if (request.StockQuantity < 0)
            throw new ValidationException("Stock quantity cannot be negative", "INVALID_STOCK");

        // Check for duplicate
        var exists = await _context.Products.AnyAsync(p => p.Name == request.Name);
        if (exists)
            throw new ConflictException($"Product with name '{request.Name}' already exists");

        // Create product
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            StockQuantity = request.StockQuantity,
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        return product;
    }

    /// <summary>
    /// Update stock quantity
    /// </summary>
    [HttpPatch("{id}/stock")]
    public async Task<Product> UpdateStock(int id, UpdateStockRequest request)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            throw new NotFoundException("Product", id);

        if (request.Quantity < 0)
            throw new ValidationException("Quantity cannot be negative", "INVALID_QUANTITY");

        product.StockQuantity = request.Quantity;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock updated for product {ProductId}: {Quantity}", id, request.Quantity);

        return product;
    }

    /// <summary>
    /// Delete product (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            throw new NotFoundException("Product", id);

        // Soft delete
        product.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId}", id);

        return NoContent();
    }
}
```

### Sample Responses

**Success - Get Products (paginated):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Wireless Mouse",
        "description": "Ergonomic wireless mouse",
        "price": 29.99,
        "category": "Electronics",
        "stockQuantity": 150,
        "isActive": true
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

**Error - Duplicate Product:**
```json
{
  "success": false,
  "data": null,
  "message": "A conflict occurred with existing data",
  "statusCode": "CONFLICT",
  "errors": ["Product with name 'Wireless Mouse' already exists"],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440001",
    "timestamp": "2025-01-15T10:31:22.456Z",
    "executionTimeMs": 12
  }
}
```

## Authentication & Authorization

Complex authentication flow with multiple outcomes using IHasStatusCode.

### Models

```csharp
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
}

public class LoginResult : IHasStatusCode, IHasMessage
{
    public string? Token { get; set; }
    public UserProfile? User { get; set; }
    public string? TwoFactorToken { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UserProfile
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
```

### Controller

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<LoginResult> Login(LoginRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Password is required");

        // Authenticate
        var result = await _authService.AuthenticateAsync(
            request.Email,
            request.Password,
            request.TwoFactorCode);

        return result.Status switch
        {
            AuthStatus.Success => new LoginResult
            {
                Token = result.Token,
                User = result.User,
                StatusCode = "LOGIN_SUCCESS",
                Message = $"Welcome back, {result.User?.FullName}!"
            },

            AuthStatus.RequiresTwoFactor => new LoginResult
            {
                TwoFactorToken = result.TwoFactorToken,
                StatusCode = "TWO_FACTOR_REQUIRED",
                Message = "Please enter the verification code sent to your device"
            },

            AuthStatus.PasswordExpired => new LoginResult
            {
                PasswordExpiryDate = result.PasswordExpiryDate,
                StatusCode = "PASSWORD_EXPIRED",
                Message = "Your password has expired. Please change it to continue."
            },

            AuthStatus.AccountLocked => new LoginResult
            {
                StatusCode = "ACCOUNT_LOCKED",
                Message = "Your account has been locked due to multiple failed login attempts. Please contact support."
            },

            AuthStatus.AccountDisabled => new LoginResult
            {
                StatusCode = "ACCOUNT_DISABLED",
                Message = "Your account has been disabled. Please contact support."
            },

            AuthStatus.EmailNotVerified => new LoginResult
            {
                StatusCode = "EMAIL_NOT_VERIFIED",
                Message = "Please verify your email address before logging in."
            },

            _ => new LoginResult
            {
                StatusCode = "INVALID_CREDENTIALS",
                Message = "Invalid email or password"
            }
        };
    }

    [HttpPost("refresh")]
    public async Task<LoginResult> RefreshToken([FromBody] string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ValidationException("Refresh token is required");

        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.IsValid)
            throw new UnauthorizedException("Invalid or expired refresh token");

        return new LoginResult
        {
            Token = result.Token,
            User = result.User,
            StatusCode = "TOKEN_REFRESHED",
            Message = "Token refreshed successfully"
        };
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst("userId")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await _authService.LogoutAsync(int.Parse(userId));
            _logger.LogInformation("User {UserId} logged out", userId);
        }

        return NoContent();
    }
}
```

### Client-Side Handling (TypeScript)

```typescript
interface LoginResponse {
    success: boolean;
    data: {
        token?: string;
        user?: UserProfile;
        twoFactorToken?: string;
        passwordExpiryDate?: string;
    };
    statusCode?: string;
    message?: string;
}

async function handleLogin(email: string, password: string) {
    const response = await api.post<LoginResponse>('/auth/login', {
        email,
        password
    });

    if (response.success) {
        switch (response.statusCode) {
            case 'LOGIN_SUCCESS':
                // Store token and redirect
                localStorage.setItem('token', response.data.token!);
                localStorage.setItem('user', JSON.stringify(response.data.user));
                router.push('/dashboard');
                break;

            case 'TWO_FACTOR_REQUIRED':
                // Show 2FA dialog
                show2FADialog(response.data.twoFactorToken!);
                break;

            case 'PASSWORD_EXPIRED':
                // Redirect to change password
                router.push('/change-password');
                showWarning(response.message);
                break;

            case 'ACCOUNT_LOCKED':
                showError(response.message);
                break;

            case 'EMAIL_NOT_VERIFIED':
                router.push('/verify-email');
                showWarning(response.message);
                break;

            case 'INVALID_CREDENTIALS':
                showError(response.message);
                break;
        }
    } else {
        showError(response.message || 'Login failed');
    }
}
```

## Multi-Tenant SaaS

Multi-tenant application with tenant isolation and custom metadata.

### Models

```csharp
public class TenantAwareResult<T> : IHasMetadata
{
    public T Data { get; set; } = default!;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class Subscription
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty; // Free, Starter, Pro, Enterprise
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxUsers { get; set; }
    public long StorageQuotaBytes { get; set; }
    public int ApiCallsPerMonth { get; set; }
}
```

### Controller

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace SaasAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ITenantContext _tenantContext;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ITenantContext tenantContext)
    {
        _subscriptionService = subscriptionService;
        _tenantContext = tenantContext;
    }

    [HttpGet("current")]
    public async Task<TenantAwareResult<Subscription>> GetCurrentSubscription()
    {
        var tenantId = _tenantContext.GetTenantId();
        var subscription = await _subscriptionService.GetByTenantIdAsync(tenantId);

        if (subscription == null)
            throw new NotFoundException("Subscription", tenantId);

        // Calculate usage
        var usage = await _subscriptionService.GetUsageAsync(tenantId);
        var usagePercentage = new
        {
            Users = (usage.ActiveUsers / (double)subscription.MaxUsers) * 100,
            Storage = (usage.StorageUsedBytes / (double)subscription.StorageQuotaBytes) * 100,
            ApiCalls = (usage.ApiCallsThisMonth / (double)subscription.ApiCallsPerMonth) * 100
        };

        return new TenantAwareResult<Subscription>
        {
            Data = subscription,
            Metadata = new Dictionary<string, object>
            {
                { "tenantId", tenantId },
                { "currentUsers", usage.ActiveUsers },
                { "maxUsers", subscription.MaxUsers },
                { "storageUsedMB", usage.StorageUsedBytes / 1024 / 1024 },
                { "storageQuotaMB", subscription.StorageQuotaBytes / 1024 / 1024 },
                { "apiCallsThisMonth", usage.ApiCallsThisMonth },
                { "apiCallsLimit", subscription.ApiCallsPerMonth },
                { "usagePercentage", usagePercentage },
                { "daysUntilRenewal", (subscription.EndDate - DateTime.UtcNow)?.Days },
                { "isTrialAccount", subscription.Plan == "Free" }
            }
        };
    }

    [HttpPost("upgrade")]
    public async Task<TenantAwareResult<Subscription>> UpgradePlan(
        [FromBody] string newPlan)
    {
        var tenantId = _tenantContext.GetTenantId();

        // Validate plan
        var validPlans = new[] { "Free", "Starter", "Pro", "Enterprise" };
        if (!validPlans.Contains(newPlan))
            throw new ValidationException($"Invalid plan. Must be one of: {string.Join(", ", validPlans)}");

        var currentSubscription = await _subscriptionService.GetByTenantIdAsync(tenantId);

        if (currentSubscription == null)
            throw new NotFoundException("Subscription", tenantId);

        // Check if upgrade
        var planHierarchy = new Dictionary<string, int>
        {
            { "Free", 0 },
            { "Starter", 1 },
            { "Pro", 2 },
            { "Enterprise", 3 }
        };

        if (planHierarchy[newPlan] <= planHierarchy[currentSubscription.Plan])
            throw new BusinessException("Cannot downgrade plan. Please contact support.", "INVALID_PLAN_CHANGE");

        // Upgrade
        var upgraded = await _subscriptionService.UpgradePlanAsync(tenantId, newPlan);

        return new TenantAwareResult<Subscription>
        {
            Data = upgraded,
            Metadata = new Dictionary<string, object>
            {
                { "tenantId", tenantId },
                { "previousPlan", currentSubscription.Plan },
                { "newPlan", newPlan },
                { "upgradedAt", DateTime.UtcNow },
                { "prorationApplied", true }
            }
        };
    }
}
```

### Sample Response

```json
{
  "success": true,
  "data": {
    "data": {
      "id": 123,
      "tenantId": "tenant-abc-123",
      "plan": "Pro",
      "startDate": "2025-01-01T00:00:00Z",
      "endDate": "2026-01-01T00:00:00Z",
      "maxUsers": 50,
      "storageQuotaBytes": 107374182400,
      "apiCallsPerMonth": 100000
    }
  },
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 67,
    "additional": {
      "tenantId": "tenant-abc-123",
      "currentUsers": 23,
      "maxUsers": 50,
      "storageUsedMB": 45678,
      "storageQuotaMB": 102400,
      "apiCallsThisMonth": 45000,
      "apiCallsLimit": 100000,
      "usagePercentage": {
        "users": 46,
        "storage": 44.6,
        "apiCalls": 45
      },
      "daysUntilRenewal": 351,
      "isTrialAccount": false
    }
  }
}
```

## File Upload & Processing

File upload with validation and progress tracking.

### Controller

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FileAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<FileUploadResult> UploadFile(IFormFile file)
    {
        // Validate file exists
        if (file == null || file.Length == 0)
            throw new ValidationException("No file provided", "NO_FILE");

        // Validate size
        if (file.Length > MaxFileSize)
            throw new ValidationException(
                $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024} MB",
                "FILE_TOO_LARGE");

        // Validate extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ValidationException(
                $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}",
                "INVALID_FILE_TYPE");

        // Upload
        var result = await _fileService.UploadAsync(file);

        _logger.LogInformation(
            "File uploaded: {FileName} ({FileSize} bytes) - ID: {FileId}",
            file.FileName,
            file.Length,
            result.FileId);

        return result;
    }

    [HttpGet("{fileId}")]
    [SkipApiResponseWrapper] // Return raw file
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        var file = await _fileService.GetAsync(fileId);

        if (file == null)
            throw new NotFoundException("File", fileId);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        var exists = await _fileService.ExistsAsync(fileId);

        if (!exists)
            throw new NotFoundException("File", fileId);

        await _fileService.DeleteAsync(fileId);

        _logger.LogInformation("File deleted: {FileId}", fileId);

        return NoContent();
    }
}

public class FileUploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
```

## Payment Processing

Payment processing with detailed metadata and error handling.

### Models

```csharp
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethod { get; set; } = string.Empty; // credit_card, paypal, stripe
    public Dictionary<string, string> PaymentDetails { get; set; } = new();
}

public class PaymentResult : IHasStatusCode, IHasMessage, IHasMetadata
{
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### Controller

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using FS.AspNetCore.ResponseWrapper.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace PaymentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        // Validate
        if (request.Amount <= 0)
            throw new ValidationException("Amount must be greater than 0", "INVALID_AMOUNT");

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new ValidationException("Payment method is required", "MISSING_PAYMENT_METHOD");

        try
        {
            var result = await _paymentService.ProcessAsync(request);

            return new PaymentResult
            {
                TransactionId = result.TransactionId,
                Amount = result.Amount,
                Currency = result.Currency,
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
                    { "confirmationUrl", result.ConfirmationUrl ?? string.Empty },
                    { "fee", result.ProcessingFee },
                    { "netAmount", result.Amount - result.ProcessingFee }
                }
            };
        }
        catch (PaymentGatewayException ex)
        {
            _logger.LogError(ex, "Payment gateway error: {ErrorCode}", ex.GatewayErrorCode);

            throw ex.GatewayErrorCode switch
            {
                "insufficient_funds" => new BusinessException(
                    "Insufficient funds in your account",
                    "INSUFFICIENT_FUNDS"),

                "card_declined" => new BusinessException(
                    "Your card was declined. Please try another payment method.",
                    "CARD_DECLINED"),

                "expired_card" => new ValidationException(
                    "Your card has expired",
                    "EXPIRED_CARD"),

                _ => new ServiceUnavailableException(
                    "Payment service is temporarily unavailable. Please try again later.")
            };
        }
    }

    [HttpPost("{transactionId}/refund")]
    public async Task<PaymentResult> RefundPayment(string transactionId)
    {
        var payment = await _paymentService.GetByTransactionIdAsync(transactionId);

        if (payment == null)
            throw new NotFoundException("Payment", transactionId);

        if (!payment.IsRefundable)
            throw new BusinessException(
                "This payment cannot be refunded",
                "REFUND_NOT_ALLOWED");

        if (payment.RefundDeadline < DateTime.UtcNow)
            throw new BusinessException(
                "Refund deadline has passed",
                "REFUND_EXPIRED");

        var result = await _paymentService.RefundAsync(transactionId);

        return new PaymentResult
        {
            TransactionId = result.RefundTransactionId,
            Amount = result.RefundAmount,
            Currency = result.Currency,
            StatusCode = "REFUND_PROCESSED",
            Message = "Refund processed successfully. Amount will be credited within 5-7 business days.",
            Metadata = new Dictionary<string, object>
            {
                { "originalTransactionId", transactionId },
                { "refundedAmount", result.RefundAmount },
                { "refundedAt", DateTime.UtcNow },
                { "estimatedCreditDate", DateTime.UtcNow.AddDays(7) }
            }
        };
    }
}
```

## Microservices Integration

Distributed tracing across microservices using correlation IDs.

### Service A (Order Service)

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IInventoryClient _inventoryClient;
    private readonly IPaymentClient _paymentClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    [HttpPost]
    public async Task<OrderResult> CreateOrder(CreateOrderRequest request)
    {
        // Get or generate correlation ID
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Check inventory (Service B)
        var inventoryAvailable = await _inventoryClient.CheckAvailabilityAsync(
            request.ProductId,
            request.Quantity,
            correlationId);

        if (!inventoryAvailable)
            throw new BusinessException("Product out of stock", "OUT_OF_STOCK");

        // Process payment (Service C)
        var paymentResult = await _paymentClient.ProcessPaymentAsync(
            request.PaymentDetails,
            correlationId);

        if (!paymentResult.Success)
            throw new BusinessException("Payment failed", "PAYMENT_FAILED");

        // Create order
        var order = await _orderService.CreateAsync(request);

        return new OrderResult
        {
            OrderId = order.Id,
            StatusCode = "ORDER_CREATED",
            Message = "Order created successfully"
        };
    }
}
```

### Service B (Inventory Service)

```csharp
public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;

    public async Task<bool> CheckAvailabilityAsync(
        int productId,
        int quantity,
        string correlationId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/inventory/{productId}?quantity={quantity}");

        // Pass correlation ID
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

        return result?.Data ?? false;
    }
}
```

**All logs will share the same correlation ID across services for easy tracing!**

## Webhook Handling

Exclude webhook endpoints from wrapping for external system compatibility.

### Configuration

```csharp
builder.Services.AddResponseWrapper(options =>
{
    // Exclude webhook endpoints
    options.ExcludedPaths = new[]
    {
        "/webhooks/stripe",
        "/webhooks/paypal",
        "/webhooks/github"
    };
});
```

### Controller

```csharp
[ApiController]
[Route("webhooks")]
public class WebhooksController : ControllerBase
{
    [HttpPost("stripe")]
    [SkipApiResponseWrapper] // Explicitly skip wrapping
    public async Task<IActionResult> StripeWebhook()
    {
        // Return raw response expected by Stripe
        return Ok(new { received = true });
    }

    [HttpPost("paypal")]
    [SkipApiResponseWrapper]
    public async Task<IActionResult> PayPalWebhook()
    {
        // Return raw response expected by PayPal
        return Ok();
    }
}
```

---

[← Back to Configuration](configuration.md) | [Next: Troubleshooting →](troubleshooting.md)
