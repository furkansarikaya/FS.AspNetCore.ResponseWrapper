using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Filters;

/// <summary>
/// Operation filter to wrap API responses with ApiResponse wrapper in OpenAPI documentation
/// </summary>
public class ResponseWrapperOperationFilter : IOperationFilter
{
    private readonly OpenApiResponseWrapperOptions _options;

    public ResponseWrapperOperationFilter(OpenApiResponseWrapperOptions options)
    {
        _options = options;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add response headers to all responses
        AddResponseHeaders(operation);

        if (!_options.AutoWrapResponses)
            return;

        var responsesToWrap = new Dictionary<string, OpenApiResponse>();

        foreach (var response in operation.Responses)
        {
            var statusCode = int.TryParse(response.Key, out var code) ? code : 0;

            // Skip excluded status codes
            if (statusCode > 0 && _options.ExcludedStatusCodes.Contains(statusCode))
                continue;

            // Skip if already wrapped
            if (IsAlreadyWrapped(response.Value))
                continue;

            responsesToWrap[response.Key] = response.Value;
        }

        // Wrap responses
        foreach (var kvp in responsesToWrap)
        {
            WrapResponse(kvp.Value, context);
        }

        // Add error response examples if enabled
        if (_options.IncludeErrorExamples)
        {
            AddErrorResponseExamples(operation);
        }
    }

    private void AddResponseHeaders(OpenApiOperation operation)
    {
        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();

            // Add X-Request-Id header
            if (!response.Headers.ContainsKey("X-Request-Id"))
            {
                response.Headers.Add("X-Request-Id", new OpenApiHeader
                {
                    Description = "Unique request identifier for tracking and correlation",
                    Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
                    Example = new OpenApiString("req-" + Guid.NewGuid().ToString("N"))
                });
            }

            // Add X-Correlation-Id header
            if (!response.Headers.ContainsKey("X-Correlation-Id"))
            {
                response.Headers.Add("X-Correlation-Id", new OpenApiHeader
                {
                    Description = "Correlation ID for distributed tracing across services",
                    Schema = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiString("corr-" + Guid.NewGuid().ToString("N"))
                });
            }

            // Add ETag header for GET/successful responses
            var responseCode = response.Headers.Keys.FirstOrDefault();
            if (responseCode?.StartsWith("2") == true && !response.Headers.ContainsKey("ETag"))
            {
                response.Headers.Add("ETag", new OpenApiHeader
                {
                    Description = "Entity tag for cache validation (if caching is enabled)",
                    Schema = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiString("\"" + Guid.NewGuid().ToString("N") + "\"")
                });
            }

            // Add Cache-Control header
            if (!response.Headers.ContainsKey("Cache-Control"))
            {
                response.Headers.Add("Cache-Control", new OpenApiHeader
                {
                    Description = "Caching directives for the response",
                    Schema = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiString("no-cache, no-store, must-revalidate")
                });
            }
        }
    }

    private bool IsAlreadyWrapped(OpenApiResponse response)
    {
        if (response.Content == null || !response.Content.Any())
            return false;

        var jsonContent = response.Content.FirstOrDefault(c => c.Key.Contains("json"));
        if (jsonContent.Value?.Schema == null)
            return false;

        // Check if schema references ApiResponse
        var schemaRef = jsonContent.Value.Schema.Reference?.Id;
        return schemaRef != null && schemaRef.Contains("ApiResponse");
    }

    private void WrapResponse(OpenApiResponse response, OperationFilterContext context)
    {
        if (response.Content == null || !response.Content.Any())
            return;

        var contentTypes = response.Content.Keys.ToList();

        foreach (var contentType in contentTypes)
        {
            var mediaType = response.Content[contentType];
            var originalSchema = mediaType.Schema;

            if (originalSchema == null)
                continue;

            // Create wrapped schema
            var wrappedSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["success"] = new OpenApiSchema
                    {
                        Type = "boolean",
                        Description = "Indicates whether the request was successful"
                    },
                    ["message"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Response message",
                        Nullable = true
                    },
                    ["data"] = originalSchema,
                    ["errors"] = new OpenApiSchema
                    {
                        Type = "array",
                        Description = "List of error messages if any",
                        Items = new OpenApiSchema
                        {
                            Type = "string"
                        },
                        Nullable = true
                    },
                    ["statusCode"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = "Application-specific status code for complex workflow handling",
                        Nullable = true
                    }
                },
                Required = new HashSet<string> { "success" }
            };

            // Add metadata if enabled
            if (_options.IncludeMetadataSchema)
            {
                wrappedSchema.Properties["metadata"] = new OpenApiSchema
                {
                    Type = "object",
                    Description = "Additional metadata about the response",
                    AdditionalPropertiesAllowed = true,
                    Nullable = true
                };
            }

            mediaType.Schema = wrappedSchema;
        }
    }

    private void AddErrorResponseExamples(OpenApiOperation operation)
    {
        // Add 400 Bad Request example if not exists
        if (!operation.Responses.ContainsKey("400"))
        {
            operation.Responses["400"] = CreateErrorResponse(
                "Bad Request - Validation failed or invalid input",
                "Validation failed",
                new[] { "Field 'email' is required", "Field 'password' must be at least 8 characters" });
        }

        // Add 401 Unauthorized example if not exists
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses["401"] = CreateErrorResponse(
                "Unauthorized - Authentication required",
                "Authentication required",
                new[] { "Invalid or missing authentication token" });
        }

        // Add 404 Not Found example if not exists
        if (!operation.Responses.ContainsKey("404"))
        {
            operation.Responses["404"] = CreateErrorResponse(
                "Not Found - Requested resource does not exist",
                "Resource not found",
                new[] { "The requested resource could not be found" });
        }

        // Add 500 Internal Server Error example if not exists
        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"] = CreateErrorResponse(
                "Internal Server Error - An unexpected error occurred",
                "An unexpected error occurred",
                new[] { "An internal server error occurred while processing your request" });
        }
    }

    private OpenApiResponse CreateErrorResponse(string description, string message, string[] errors)
    {
        var response = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["success"] = new OpenApiSchema
                            {
                                Type = "boolean",
                                Description = "Always false for error responses"
                            },
                            ["message"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Human-readable error message"
                            },
                            ["data"] = new OpenApiSchema
                            {
                                Nullable = true,
                                Description = "Always null for error responses"
                            },
                            ["errors"] = new OpenApiSchema
                            {
                                Type = "array",
                                Description = "List of error messages",
                                Items = new OpenApiSchema
                                {
                                    Type = "string"
                                }
                            },
                            ["metadata"] = new OpenApiSchema
                            {
                                Type = "object",
                                Description = "Additional metadata about the request/response",
                                Nullable = true
                            }
                        }
                    },
                    Example = CreateErrorExample(message, errors)
                }
            }
        };

        return response;
    }

    private OpenApiObject CreateErrorExample(string message, string[] errors)
    {
        var errorsArray = new OpenApiArray();
        foreach (var error in errors)
        {
            errorsArray.Add(new OpenApiString(error));
        }

        return new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(false),
            ["data"] = new OpenApiNull(),
            ["message"] = new OpenApiString(message),
            ["errors"] = errorsArray,
            ["metadata"] = new OpenApiObject
            {
                ["requestId"] = new OpenApiString("req-" + Guid.NewGuid().ToString("N")),
                ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O")),
                ["executionTimeMs"] = new OpenApiInteger(12),
                ["path"] = new OpenApiString("/api/example"),
                ["method"] = new OpenApiString("GET")
            }
        };
    }
}
