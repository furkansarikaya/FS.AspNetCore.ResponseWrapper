# Getting Started Guide

Complete step-by-step guide to get ResponseWrapper working in your ASP.NET Core API.

## Prerequisites

- .NET 10.0 SDK or later
- ASP.NET Core Web API project
- Basic knowledge of C# and ASP.NET Core

## Step 1: Create or Open Your Project

### Creating a New Project

```bash
# Create a new Web API project
dotnet new webapi -n MyApi
cd MyApi

# Open in your IDE
code . # or rider . or start MyApi.sln
```

### Using an Existing Project

Just open your existing ASP.NET Core Web API project.

## Step 2: Install the Package

### Via .NET CLI (Recommended)

```bash
dotnet add package FS.AspNetCore.ResponseWrapper
```

### Via Package Manager Console (Visual Studio)

```powershell
Install-Package FS.AspNetCore.ResponseWrapper
```

### Via NuGet Package Manager (Visual Studio/Rider)

1. Right-click on your project
2. Select "Manage NuGet Packages"
3. Search for "FS.AspNetCore.ResponseWrapper"
4. Click "Install"

## Step 3: Configure ResponseWrapper

Open your `Program.cs` file and add ResponseWrapper:

```csharp
using FS.AspNetCore.ResponseWrapper;
using FS.AspNetCore.ResponseWrapper.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddResponseWrapper(); // 👈 Add this line

var app = builder.Build();

// Add middleware (optional but recommended for error handling)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // 👈 Add this line

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

That's it! **No changes to your controllers needed.**

## Step 4: Test It Out

### Create a Simple Controller

```csharp
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public List<User> GetAll()
    {
        return new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
        };
    }

    [HttpGet("{id}")]
    public User? GetById(int id)
    {
        // Simulate database lookup
        if (id == 1)
            return new User { Id = 1, Name = "John Doe", Email = "john@example.com" };

        return null; // Will return 200 with null data (not ideal - see error handling below)
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### Run Your API

```bash
dotnet run
```

### Test the Endpoints

**Get all users:** `GET https://localhost:5001/api/users`

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com"
    },
    {
      "id": 2,
      "name": "Jane Smith",
      "email": "jane@example.com"
    }
  ],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:30:45.123Z",
    "executionTimeMs": 12,
    "version": "1.0",
    "path": "/api/users",
    "method": "GET"
  }
}
```

Notice how your simple `List<User>` response is now wrapped with:
- ✅ Success indicator
- ✅ Metadata (request ID, timestamp, execution time)
- ✅ Standardized structure

## Step 5: Add Error Handling

Returning `null` for not found is not ideal. Let's use proper exceptions:

```csharp
using FS.AspNetCore.ResponseWrapper.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "John Doe", Email = "john@example.com" },
        new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
    };

    [HttpGet]
    public List<User> GetAll()
    {
        return _users;
    }

    [HttpGet("{id}")]
    public User GetById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);

        if (user == null)
            throw new NotFoundException("User", id); // 👈 Throws proper exception

        return user;
    }

    [HttpPost]
    public User Create(CreateUserRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required"); // 👈 Validation exception

        // Check for duplicates
        if (_users.Any(u => u.Email == request.Email))
            throw new ConflictException($"User with email {request.Email} already exists");

        var user = new User
        {
            Id = _users.Max(u => u.Id) + 1,
            Name = request.Name,
            Email = request.Email
        };

        _users.Add(user);
        return user;
    }
}

public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### Test Error Responses

**Get non-existent user:** `GET https://localhost:5001/api/users/999`

```json
{
  "success": false,
  "data": null,
  "message": "The requested item could not be found",
  "statusCode": "NOT_FOUND",
  "errors": ["User (999) was not found."],
  "metadata": {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-01-15T10:35:22.456Z",
    "executionTimeMs": 5,
    "path": "/api/users/999",
    "method": "GET"
  }
}
```

HTTP Status: **404 Not Found**

**Create user with validation error:**

```json
POST /api/users
{
  "name": "",
  "email": "test@example.com"
}
```

Response:
```json
{
  "success": false,
  "data": null,
  "message": "Please check your input and try again",
  "statusCode": "VALIDATION_ERROR",
  "errors": ["Name is required"],
  "metadata": {
    "requestId": "...",
    "timestamp": "...",
    "executionTimeMs": 3
  }
}
```

HTTP Status: **400 Bad Request**

## Step 6: Customize Configuration (Optional)

Add custom configuration to control ResponseWrapper behavior:

```csharp
builder.Services.AddResponseWrapper(options =>
{
    // Enable/disable features
    options.EnableExecutionTimeTracking = true;
    options.EnablePaginationMetadata = true;
    options.EnableCorrelationId = true;

    // Exclude specific paths from wrapping
    options.ExcludedPaths = new[] { "/health", "/metrics", "/swagger" };

    // Customize behavior
    options.WrapSuccessResponses = true;
    options.WrapErrorResponses = true;
},
errorMessages =>
{
    // Customize error messages
    errorMessages.ValidationErrorMessage = "Invalid input provided";
    errorMessages.NotFoundErrorMessage = "Resource not found";
    errorMessages.UnauthorizedAccessMessage = "Please log in first";
});
```

## What's Next?

Now that you have the basics working:

1. 📖 Learn about [Core Features](core-features.md) - Interfaces, status codes, custom metadata
2. 📄 Explore [Pagination Support](pagination.md) - Automatic pagination detection
3. 💡 Check [Examples](examples.md) - Real-world scenarios
4. ⚙️ Read [Configuration Guide](configuration.md) - All available options
5. 🚀 Try [Enterprise Features](enterprise/README.md) - OpenAPI, caching, telemetry

## Common Questions

### Do I need to change my existing controllers?
**No!** ResponseWrapper works with your existing code automatically.

### Will this slow down my API?
**Minimal impact.** ResponseWrapper adds only a few milliseconds (typically <5ms) to response time.

### Can I disable wrapping for specific endpoints?
**Yes!** Use `[SkipApiResponseWrapper]` attribute or configure `ExcludedPaths`.

### Does this work with minimal APIs?
**Currently no.** ResponseWrapper is designed for controller-based APIs. Minimal API support is planned for future versions.

### Can I customize the response format?
**Partially.** You can control metadata inclusion and error messages, but the core structure (success, data, errors, metadata) is fixed for consistency.

## Troubleshooting

### Responses are not being wrapped

**Check:**
1. ✅ Controller has `[ApiController]` attribute
2. ✅ `AddResponseWrapper()` is called in `Program.cs`
3. ✅ Endpoint is not in `ExcludedPaths`

### Error responses not showing

**Check:**
1. ✅ `GlobalExceptionHandlingMiddleware` is registered
2. ✅ Middleware is added EARLY in the pipeline (before routing)
3. ✅ Exceptions are being thrown (not caught elsewhere)

### Need more help?
Check the [Troubleshooting Guide](troubleshooting.md) for more common issues.

---

[← Back to README](../README.md) | [Next: Core Features →](core-features.md)
