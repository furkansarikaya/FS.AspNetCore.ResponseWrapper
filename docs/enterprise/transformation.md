# Data Transformation

Complete guide to data masking, field selection, and GDPR-compliant response transformation.

## Overview

ResponseWrapper's transformation extension provides:
- ✅ **Data Masking** - Automatic PII protection
- ✅ **Field Selection** - Client-controlled response shaping
- ✅ **GDPR Compliance** - Built-in sensitive data handling
- ✅ **Custom Transformers** - Extensible transformation pipeline
- ✅ **Attribute-Based** - Simple attribute decoration
- ✅ **Zero Performance Impact** - Efficient transformation

## Quick Start

### Installation

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.Transformation
```

### Basic Configuration

```csharp
using FS.AspNetCore.ResponseWrapper.Transformation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add transformation
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableDataMasking = true;
    options.EnableFieldSelection = true;
    options.MaskingCharacter = '*';
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
```

### Usage

```csharp
using FS.AspNetCore.ResponseWrapper.Transformation.Attributes;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SensitiveData] // Automatically masked
    public string Email { get; set; } = string.Empty;

    [SensitiveData]
    public string Phone { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    return await _userService.GetAsync(id);
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "j***@example.com",
    "phone": "***-***-1234",
    "createdAt": "2025-01-15T10:30:45.123Z"
  }
}
```

## Features

### 1. Data Masking

**Automatic PII protection with multiple masking strategies:**

#### Email Masking

```csharp
public class User
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.Email)]
    public string Email { get; set; } = string.Empty;
}
```

**Input:** `john.doe@example.com`
**Output:** `j***@example.com`

#### Phone Masking

```csharp
public class User
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.Phone)]
    public string Phone { get; set; } = string.Empty;
}
```

**Input:** `+1-555-123-4567`
**Output:** `***-***-4567`

#### Credit Card Masking

```csharp
public class PaymentMethod
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.CreditCard)]
    public string CardNumber { get; set; } = string.Empty;
}
```

**Input:** `4532-1234-5678-9010`
**Output:** `****-****-****-9010`

#### Partial Masking

```csharp
public class User
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 4)]
    public string SSN { get; set; } = string.Empty;
}
```

**Input:** `123-45-6789`
**Output:** `***-**-6789`

#### Full Masking

```csharp
public class User
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.Full)]
    public string Password { get; set; } = string.Empty;
}
```

**Input:** `MyP@ssw0rd!`
**Output:** `***********`

#### Custom Masking

```csharp
public class User
{
    [SensitiveData(MaskingStrategy = MaskingStrategy.Custom, CustomMask = "REDACTED")]
    public string SecretKey { get; set; } = string.Empty;
}
```

**Input:** `sk-1234567890abcdef`
**Output:** `REDACTED`

### 2. Field Selection

**Client-controlled response shaping:**

**Request:**
```http
GET /api/users/1?fields=id,name,email
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "j***@example.com"
  }
}
```

**All other fields excluded!**

**Multiple endpoints:**
```http
GET /api/products?fields=id,name,price
GET /api/orders?fields=id,orderId,totalAmount,createdAt
```

**Nested field selection:**
```http
GET /api/orders/1?fields=id,customer.name,customer.email,items.name,items.price
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "customer": {
      "name": "John Doe",
      "email": "j***@example.com"
    },
    "items": [
      {
        "name": "Product 1",
        "price": 29.99
      }
    ]
  }
}
```

### 3. GDPR Compliance

**Built-in GDPR-compliant data handling:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SensitiveData(GdprCategory = GdprCategory.PersonalData)]
    public string Email { get; set; } = string.Empty;

    [SensitiveData(GdprCategory = GdprCategory.PersonalData)]
    public string Phone { get; set; } = string.Empty;

    [SensitiveData(GdprCategory = GdprCategory.SensitiveData)]
    public string HealthInfo { get; set; } = string.Empty;

    [SensitiveData(GdprCategory = GdprCategory.FinancialData)]
    public string BankAccount { get; set; } = string.Empty;
}
```

**GDPR categories:**
- `PersonalData` - Name, email, phone (standard masking)
- `SensitiveData` - Health, biometric data (full masking)
- `FinancialData` - Bank accounts, credit cards (partial masking)

**Automatic handling based on user consent:**
```csharp
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableGdprCompliance = true;
    options.MaskWithoutConsent = true; // Mask if no consent
});
```

### 4. Role-Based Masking

**Different masking for different roles:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SensitiveData(
        MaskingStrategy = MaskingStrategy.Email,
        ExcludeRoles = new[] { "Admin", "Manager" }
    )]
    public string Email { get; set; } = string.Empty;
}
```

**For regular users:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "j***@example.com"
}
```

**For admins:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com"
}
```

### 5. Conditional Masking

**Mask based on conditions:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [SensitiveData(
        MaskingStrategy = MaskingStrategy.Email,
        MaskWhen = "!User.IsOwner"
    )]
    public string Email { get; set; } = string.Empty;
}
```

**When viewing own profile:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com"
}
```

**When viewing other profiles:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "j***@example.com"
}
```

## Configuration Options

### Complete Configuration

```csharp
builder.Services.AddResponseWrapperTransformation(options =>
{
    // Enable/disable features
    options.EnableDataMasking = true;
    options.EnableFieldSelection = true;
    options.EnableGdprCompliance = true;

    // Masking configuration
    options.MaskingCharacter = '*';
    options.MaskWithoutConsent = true;

    // Field selection configuration
    options.FieldSelectionQueryParameter = "fields"; // Default
    options.AllowNestedFieldSelection = true;
    options.CaseSensitiveFieldNames = false;

    // Performance
    options.CacheTransformations = true;
    options.CacheDuration = TimeSpan.FromMinutes(10);

    // Custom transformers
    options.RegisterTransformer<CustomDataTransformer>();
});
```

## Custom Transformers

### Creating a Custom Transformer

```csharp
using FS.AspNetCore.ResponseWrapper.Transformation;

public class CustomDataTransformer : IDataTransformer
{
    public object Transform(object data, HttpContext context)
    {
        // Your transformation logic
        if (data is User user)
        {
            // Custom transformation
            user.InternalId = null; // Remove internal ID
            user.CreatedBy = "System"; // Anonymize creator
        }

        return data;
    }
}

// Register
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.RegisterTransformer<CustomDataTransformer>();
});
```

### Audit Log Transformer

```csharp
public class AuditLogTransformer : IDataTransformer
{
    private readonly IAuditLogger _auditLogger;

    public AuditLogTransformer(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public object Transform(object data, HttpContext context)
    {
        // Log data access
        if (data is ISensitiveData sensitiveData)
        {
            var userId = context.User.FindFirst("userId")?.Value;
            _auditLogger.LogDataAccess(userId, sensitiveData.GetType().Name);
        }

        return data;
    }
}
```

## Real-World Examples

### Healthcare Application

```csharp
public class Patient
{
    public int Id { get; set; }

    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 0)]
    public string FullName { get; set; } = string.Empty;

    [SensitiveData(MaskingStrategy = MaskingStrategy.Email)]
    public string Email { get; set; } = string.Empty;

    [SensitiveData(MaskingStrategy = MaskingStrategy.Full)]
    public string SSN { get; set; } = string.Empty;

    [SensitiveData(
        MaskingStrategy = MaskingStrategy.Full,
        GdprCategory = GdprCategory.SensitiveData,
        ExcludeRoles = new[] { "Doctor", "Nurse" }
    )]
    public string MedicalHistory { get; set; } = string.Empty;

    [SensitiveData(
        MaskingStrategy = MaskingStrategy.Full,
        ExcludeRoles = new[] { "Doctor" }
    )]
    public string Diagnosis { get; set; } = string.Empty;
}

[HttpGet("{id}")]
public async Task<Patient> GetPatient(int id)
{
    return await _patientService.GetAsync(id);
}
```

**Response (for admin user):**
```json
{
  "id": 1,
  "fullName": "***********",
  "email": "j***@example.com",
  "ssn": "***********",
  "medicalHistory": "***********",
  "diagnosis": "***********"
}
```

**Response (for doctor):**
```json
{
  "id": 1,
  "fullName": "***********",
  "email": "j***@example.com",
  "ssn": "***********",
  "medicalHistory": "High blood pressure, diabetes",
  "diagnosis": "Type 2 diabetes mellitus"
}
```

### Financial Application

```csharp
public class Account
{
    public int Id { get; set; }

    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 4)]
    public string AccountNumber { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    [SensitiveData(MaskingStrategy = MaskingStrategy.CreditCard)]
    public string LinkedCard { get; set; } = string.Empty;

    [SensitiveData(
        MaskingStrategy = MaskingStrategy.Full,
        GdprCategory = GdprCategory.FinancialData
    )]
    public string RoutingNumber { get; set; } = string.Empty;
}

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 4)]
    public string SourceAccount { get; set; } = string.Empty;

    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 4)]
    public string DestinationAccount { get; set; } = string.Empty;
}
```

**Response:**
```json
{
  "id": 1,
  "accountNumber": "******7890",
  "balance": 5432.10,
  "linkedCard": "****-****-****-1234",
  "routingNumber": "***********"
}
```

### E-Commerce Application

```csharp
public class Order
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }

    [SensitiveData(MaskingStrategy = MaskingStrategy.Email)]
    public string CustomerEmail { get; set; } = string.Empty;

    [SensitiveData(MaskingStrategy = MaskingStrategy.Phone)]
    public string CustomerPhone { get; set; } = string.Empty;

    public Address ShippingAddress { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    [SensitiveData(MaskingStrategy = MaskingStrategy.Partial, VisibleCharacters = 3)]
    public string ZipCode { get; set; } = string.Empty;
}

// Field selection
[HttpGet("{id}")]
public async Task<Order> GetOrder(int id)
{
    return await _orderService.GetAsync(id);
}
```

**Request:**
```http
GET /api/orders/1?fields=id,orderId,totalAmount,shippingAddress.city,shippingAddress.state
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "orderId": "ORD-12345",
    "totalAmount": 99.99,
    "shippingAddress": {
      "city": "New York",
      "state": "NY"
    }
  }
}
```

## Performance Considerations

### Transformation Overhead

| Operation | Overhead | Notes |
|-----------|----------|-------|
| Data masking | ~0.5-1ms | Per response |
| Field selection | ~1-2ms | Depends on complexity |
| Custom transformers | Varies | Based on implementation |
| Caching | ~0.1ms | Cached transformations |

**Total overhead:** Typically 1-3ms per response

### Optimization Tips

1. **Enable caching:**
```csharp
options.CacheTransformations = true;
options.CacheDuration = TimeSpan.FromMinutes(10);
```

2. **Use simple masking strategies:**
```csharp
// Faster
[SensitiveData(MaskingStrategy = MaskingStrategy.Full)]

// Slower
[SensitiveData(MaskingStrategy = MaskingStrategy.Custom, CustomMask = "...")]
```

3. **Limit nested field selection depth:**
```csharp
options.MaxFieldSelectionDepth = 3; // Limit nesting
```

4. **Use role-based masking wisely:**
```csharp
// Only check roles when necessary
[SensitiveData(ExcludeRoles = new[] { "Admin" })]
```

## GDPR Compliance Checklist

### Data Subject Rights

✅ **Right to Access**
- Field selection allows data subjects to request specific data
- Transformation ensures only necessary data is exposed

✅ **Right to Erasure**
- Masking can anonymize data instead of deletion
- Custom transformers can implement erasure logic

✅ **Right to Portability**
- Field selection enables data export in specific formats
- No masking for data subject's own data

✅ **Right to Rectification**
- Audit log transformer tracks data access
- Masking protects data during rectification process

### Implementation

```csharp
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableGdprCompliance = true;
    options.EnableDataMasking = true;
    options.MaskWithoutConsent = true;

    // Log all sensitive data access
    options.RegisterTransformer<GdprAuditTransformer>();
});

public class GdprAuditTransformer : IDataTransformer
{
    private readonly IAuditLogger _auditLogger;

    public object Transform(object data, HttpContext context)
    {
        var userId = context.User.FindFirst("userId")?.Value;
        var properties = data.GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<SensitiveDataAttribute>() != null);

        foreach (var prop in properties)
        {
            _auditLogger.LogGdprDataAccess(
                userId,
                data.GetType().Name,
                prop.Name,
                prop.GetValue(data)?.ToString());
        }

        return data;
    }
}
```

## Troubleshooting

### Masking Not Working

**Check configuration:**
```csharp
builder.Services.AddResponseWrapperTransformation(options =>
{
    options.EnableDataMasking = true; // Ensure this is true
});
```

**Check attribute:**
```csharp
[SensitiveData] // Ensure attribute is present
public string Email { get; set; }
```

### Field Selection Not Working

**Check query parameter:**
```http
GET /api/users/1?fields=id,name
```

**Check configuration:**
```csharp
options.FieldSelectionQueryParameter = "fields"; // Default
```

### Role-Based Masking Not Working

**Check user claims:**
```csharp
var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
// Ensure roles are present in claims
```

**Check role names:**
```csharp
[SensitiveData(ExcludeRoles = new[] { "Admin" })] // Must match exactly
```

## Best Practices

1. **Always mask PII** - Email, phone, SSN, credit cards
2. **Use appropriate strategies** - Email for emails, CreditCard for cards
3. **Enable GDPR compliance** - For EU customers
4. **Log sensitive data access** - Audit trail for compliance
5. **Cache transformations** - Improve performance
6. **Test masking in production** - Verify masking is working
7. **Document masking rules** - For compliance audits
8. **Use role-based masking** - Different views for different users
9. **Limit field selection depth** - Prevent abuse
10. **Monitor performance impact** - Ensure acceptable overhead

---

[← Back to Caching](caching.md) | [Next: Presets →](presets.md)
