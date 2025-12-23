# OpenTelemetry & Distributed Tracing

Complete guide to distributed tracing and observability with ResponseWrapper and OpenTelemetry.

## Overview

ResponseWrapper's OpenTelemetry integration provides:
- ✅ **W3C Trace Context** propagation across services
- ✅ **Automatic span creation** for all API requests
- ✅ **Custom metrics** tracking (request count, duration, errors)
- ✅ **Rich span attributes** (HTTP method, path, status code, user info)
- ✅ **Exception tracking** in spans
- ✅ **Correlation ID integration**
- ✅ **Support for multiple exporters** (OTLP, Jaeger, Zipkin, Console)

## Quick Start

### Installation

```bash
dotnet add package FS.AspNetCore.ResponseWrapper.OpenTelemetry
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

### Basic Configuration

```csharp
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

// Add OpenTelemetry integration
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
    options.ServiceVersion = "1.0.0";
    options.EnableMetrics = true;
    options.EnableTracing = true;
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
```

**That's it!** All your API requests are now traced automatically.

## Features

### 1. W3C Trace Context Propagation

**Automatic trace context propagation across microservices:**

```
Client → Service A → Service B → Service C
         [trace-id: abc123]
                  ↓
         [span-id: 001] → [span-id: 002] → [span-id: 003]
```

**Request headers automatically handled:**
```http
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
tracestate: vendor1=value1,vendor2=value2
```

**Response headers automatically added:**
```http
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
X-Request-ID: req-8a4f2e1d9c7b6a3e
X-Correlation-ID: trace-abc-123-def-456
```

### 2. Automatic Span Creation

**Every API request creates a span with rich attributes:**

```csharp
[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    return await _userService.GetAsync(id);
}
```

**Span attributes automatically added:**
```
Span Name: GET /api/users/{id}
Attributes:
  - http.method: GET
  - http.url: https://api.example.com/api/users/123
  - http.status_code: 200
  - http.route: /api/users/{id}
  - http.request_content_length: 0
  - http.response_content_length: 245
  - execution_time_ms: 42
  - request_id: req-8a4f2e1d9c7b6a3e
  - correlation_id: trace-abc-123-def-456
  - user_id: 456 (if authenticated)
  - user_name: john@example.com (if authenticated)
```

### 3. Custom Metrics

**Automatic metrics collection:**

**Request Count:**
```
api.requests.count
  - http.method: GET
  - http.route: /api/users/{id}
  - http.status_code: 200
```

**Request Duration:**
```
api.requests.duration
  - http.method: GET
  - http.route: /api/users/{id}
  - Value: 42ms
```

**Error Count:**
```
api.requests.errors
  - http.method: POST
  - http.route: /api/orders
  - error_type: ValidationException
```

### 4. Exception Tracking

**Exceptions are automatically recorded in spans:**

```csharp
[HttpGet("{id}")]
public async Task<User> GetUser(int id)
{
    var user = await _userService.GetAsync(id);
    if (user == null)
        throw new NotFoundException("User", id); // Automatically tracked
    return user;
}
```

**Span includes exception details:**
```
Span Status: ERROR
Exception:
  - exception.type: FS.AspNetCore.ResponseWrapper.Exceptions.NotFoundException
  - exception.message: User (123) was not found.
  - exception.stacktrace: [full stack trace]
  - error_code: NOT_FOUND
```

## Configuration Options

### Complete Configuration

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    // Service identification
    options.ServiceName = "MyAPI";
    options.ServiceVersion = "1.0.0";
    options.ServiceNamespace = "Production";
    options.ServiceInstanceId = Environment.MachineName;

    // Feature toggles
    options.EnableMetrics = true;      // Enable custom metrics
    options.EnableTracing = true;      // Enable distributed tracing

    // Span attributes
    options.RecordException = true;    // Record exceptions in spans
    options.CaptureRequestBody = false; // Don't capture request body (security)
    options.CaptureResponseBody = false; // Don't capture response body (security)

    // Sampling
    options.SamplingRatio = 1.0;       // Sample 100% of requests (dev/staging)
                                        // Use 0.1 for 10% sampling (production)

    // Custom attributes
    options.CustomAttributes = new Dictionary<string, object>
    {
        { "deployment.environment", builder.Environment.EnvironmentName },
        { "region", "us-east-1" },
        { "datacenter", "dc1" }
    };
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
});
```

### Sampling Configuration

**Production sampling (10% of requests):**
```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI-Production";
    options.SamplingRatio = 0.1; // Sample 10%
});
```

**Development sampling (100% of requests):**
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseWrapperOpenTelemetry(options =>
    {
        options.ServiceName = "MyAPI-Development";
        options.SamplingRatio = 1.0; // Sample everything
    });
}
```

### Environment-Specific Configuration

```csharp
var telemetryOptions = builder.Environment.IsProduction()
    ? new ResponseWrapperOpenTelemetryOptions
    {
        ServiceName = "MyAPI",
        ServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        EnableMetrics = true,
        EnableTracing = true,
        SamplingRatio = 0.1, // 10% sampling in production
        RecordException = true,
        CaptureRequestBody = false, // Don't capture in production
        CaptureResponseBody = false
    }
    : new ResponseWrapperOpenTelemetryOptions
    {
        ServiceName = "MyAPI-Dev",
        ServiceVersion = "dev",
        EnableMetrics = true,
        EnableTracing = true,
        SamplingRatio = 1.0, // 100% sampling in development
        RecordException = true,
        CaptureRequestBody = true, // Capture for debugging
        CaptureResponseBody = true
    };

builder.Services.AddResponseWrapperOpenTelemetry(telemetryOptions);
```

## Exporters

### OTLP Exporter (Recommended)

**Works with Jaeger, Tempo, and OpenTelemetry Collector:**

```bash
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
});
```

### Jaeger Exporter

```bash
dotnet add package OpenTelemetry.Exporter.Jaeger
```

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
})
.AddJaegerExporter(jaegerOptions =>
{
    jaegerOptions.AgentHost = "localhost";
    jaegerOptions.AgentPort = 6831;
});
```

### Zipkin Exporter

```bash
dotnet add package OpenTelemetry.Exporter.Zipkin
```

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
})
.AddZipkinExporter(zipkinOptions =>
{
    zipkinOptions.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
});
```

### Console Exporter (Development)

```bash
dotnet add package OpenTelemetry.Exporter.Console
```

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseWrapperOpenTelemetry(options =>
    {
        options.ServiceName = "MyAPI-Dev";
    })
    .AddConsoleExporter();
}
```

### Multiple Exporters

```csharp
builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "MyAPI";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
})
.AddConsoleExporter(); // Also log to console for debugging
```

## Microservices Example

### Service A (Order Service)

```csharp
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "OrderService";
    options.ServiceVersion = "1.0.0";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://jaeger:4317");
});

var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();
app.Run();
```

**Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IInventoryClient _inventoryClient;
    private readonly IPaymentClient _paymentClient;

    [HttpPost]
    public async Task<OrderResult> CreateOrder(CreateOrderRequest request)
    {
        // Get correlation ID from request
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Check inventory (Service B)
        // Trace context automatically propagated!
        var available = await _inventoryClient.CheckAvailabilityAsync(
            request.ProductId,
            request.Quantity,
            correlationId);

        if (!available)
            throw new BusinessException("Out of stock", "OUT_OF_STOCK");

        // Process payment (Service C)
        // Trace context automatically propagated!
        var paymentResult = await _paymentClient.ProcessAsync(
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
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "InventoryService";
    options.ServiceVersion = "1.0.0";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://jaeger:4317");
});

var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();
app.Run();
```

**Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    [HttpGet("check/{productId}")]
    public async Task<bool> CheckAvailability(int productId, [FromQuery] int quantity)
    {
        // Trace context automatically received from Order Service
        var available = await _inventoryService.CheckAsync(productId, quantity);
        return available;
    }
}
```

### Service C (Payment Service)

```csharp
using FS.AspNetCore.ResponseWrapper.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseWrapper();

builder.Services.AddResponseWrapperOpenTelemetry(options =>
{
    options.ServiceName = "PaymentService";
    options.ServiceVersion = "1.0.0";
})
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://jaeger:4317");
});

var app = builder.Build();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.MapControllers();
app.Run();
```

**Trace visualization in Jaeger:**
```
Order Service [POST /api/orders]
├── Inventory Service [GET /api/inventory/check/123]
│   └── Database Query [SELECT * FROM inventory WHERE product_id = 123]
└── Payment Service [POST /api/payments/process]
    └── External Payment Gateway [POST https://payment-gateway.com/charge]

Total Duration: 234ms
  - Order Service: 234ms
  - Inventory Service: 45ms
  - Payment Service: 156ms
```

## Viewing Traces

### Jaeger UI

**Start Jaeger (Docker):**
```bash
docker run -d --name jaeger \
  -p 5775:5775/udp \
  -p 6831:6831/udp \
  -p 6832:6832/udp \
  -p 5778:5778 \
  -p 16686:16686 \
  -p 14268:14268 \
  -p 14250:14250 \
  -p 9411:9411 \
  jaegertracing/all-in-one:latest
```

**Access Jaeger UI:**
```
http://localhost:16686
```

**Search for traces:**
1. Select service: "MyAPI"
2. Set operation: "GET /api/users/{id}"
3. Click "Find Traces"

**Trace details show:**
- Request duration
- All spans in the trace
- Span attributes (HTTP method, status, etc.)
- Exceptions (if any)
- Timeline visualization

### Grafana Tempo

**Configure OTLP endpoint:**
```csharp
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://tempo:4317");
});
```

**Query traces in Grafana:**
```
{service.name="MyAPI"} | http.status_code="500"
```

## Custom Instrumentation

### Adding Custom Spans

```csharp
using System.Diagnostics;

[HttpPost]
public async Task<Order> CreateOrder(CreateOrderRequest request)
{
    // Create custom span
    using var activity = Activity.Current?.Source.StartActivity("ProcessOrder");
    activity?.SetTag("order.total", request.TotalAmount);
    activity?.SetTag("order.item_count", request.Items.Count);

    // Your business logic
    var order = await _orderService.CreateAsync(request);

    activity?.SetTag("order.id", order.Id);
    activity?.SetTag("order.status", order.Status);

    return order;
}
```

### Adding Custom Events

```csharp
[HttpPost("payment")]
public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
{
    var activity = Activity.Current;

    activity?.AddEvent(new ActivityEvent("PaymentStarted"));

    var result = await _paymentService.ProcessAsync(request);

    if (result.Success)
    {
        activity?.AddEvent(new ActivityEvent("PaymentSucceeded"));
    }
    else
    {
        activity?.AddEvent(new ActivityEvent("PaymentFailed",
            tags: new ActivityTagsCollection
            {
                { "failure_reason", result.FailureReason }
            }));
    }

    return result;
}
```

### Adding Metrics

```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter _meter = new("MyAPI.Orders");
    private static readonly Counter<long> _orderCounter = _meter.CreateCounter<long>("orders.created");
    private static readonly Histogram<double> _orderValueHistogram = _meter.CreateHistogram<double>("orders.value");

    public async Task<Order> CreateAsync(CreateOrderRequest request)
    {
        var order = await CreateOrderInDatabase(request);

        // Record metrics
        _orderCounter.Add(1, new KeyValuePair<string, object>("status", order.Status));
        _orderValueHistogram.Record(order.TotalAmount,
            new KeyValuePair<string, object>("currency", order.Currency));

        return order;
    }
}
```

## Performance Considerations

### Overhead

**OpenTelemetry overhead:**
- No sampling: ~2-5ms per request
- With 10% sampling: ~0.2-0.5ms per request
- With 1% sampling: ~0.02-0.05ms per request

**Recommendations:**
- **Development/Staging:** 100% sampling for complete visibility
- **Production:** 10% sampling for most applications
- **High-traffic APIs:** 1-5% sampling

### Sampling Strategies

**Probability-based sampling:**
```csharp
options.SamplingRatio = 0.1; // 10% of all requests
```

**Always-on for errors:**
```csharp
// Custom sampler that always samples errors
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder.SetSampler(new AlwaysOnErrorSampler());
    });

public class AlwaysOnErrorSampler : Sampler
{
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Always sample if there's an error
        if (samplingParameters.Tags.Any(t => t.Key == "error"))
            return new SamplingResult(SamplingDecision.RecordAndSample);

        // Otherwise, sample 10%
        return Random.Shared.NextDouble() < 0.1
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
```

## Troubleshooting

### Traces Not Appearing

**Check exporter endpoint:**
```csharp
.AddOtlpExporter(otlpOptions =>
{
    otlpOptions.Endpoint = new Uri("http://localhost:4317");
    // Ensure this endpoint is reachable
});
```

**Test connectivity:**
```bash
curl http://localhost:4317
```

**Enable console exporter for debugging:**
```csharp
.AddConsoleExporter(); // See traces in console
```

### Trace Context Not Propagating

**Ensure HTTP client is configured:**
```csharp
builder.Services.AddHttpClient("MyClient")
    .AddHttpMessageHandler<PropagatingHandler>();
```

**Or use OpenTelemetry HTTP instrumentation:**
```bash
dotnet add package OpenTelemetry.Instrumentation.Http
```

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder.AddHttpClientInstrumentation();
    });
```

### High Memory Usage

**Reduce sampling:**
```csharp
options.SamplingRatio = 0.01; // 1% sampling
```

**Disable body capture:**
```csharp
options.CaptureRequestBody = false;
options.CaptureResponseBody = false;
```

## Best Practices

1. **Use meaningful service names:** `OrderService`, not `Service1`
2. **Include version information:** Track which version produced traces
3. **Use appropriate sampling:** 100% in dev, 10% in prod, 1% in high-traffic
4. **Add custom attributes:** Business context helps debugging
5. **Don't capture sensitive data:** Disable body capture in production
6. **Monitor trace storage:** Ensure your tracing backend can handle the volume
7. **Use correlation IDs:** Link related operations across services

---

[← Back to OpenAPI](openapi.md) | [Next: Caching →](caching.md)
