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
                        Description = "List of errors if any",
                        Items = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["code"] = new OpenApiSchema { Type = "string" },
                                ["message"] = new OpenApiSchema { Type = "string" },
                                ["field"] = new OpenApiSchema { Type = "string", Nullable = true }
                            }
                        },
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
                "Bad Request",
                "Validation failed",
                new[]
                {
                    new { code = "VALIDATION_ERROR", message = "Field is required", field = "email" }
                });
        }

        // Add 401 Unauthorized example if not exists
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses["401"] = CreateErrorResponse(
                "Unauthorized",
                "Authentication required",
                new[]
                {
                    new { code = "UNAUTHORIZED", message = "Invalid or missing authentication token", field = (string?)null }
                });
        }

        // Add 500 Internal Server Error example if not exists
        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"] = CreateErrorResponse(
                "Internal Server Error",
                "An unexpected error occurred",
                new[]
                {
                    new { code = "INTERNAL_ERROR", message = "An unexpected error occurred while processing your request", field = (string?)null }
                });
        }
    }

    private OpenApiResponse CreateErrorResponse(string description, string message, object[] errors)
    {
        return new OpenApiResponse
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
                                Example = new OpenApiBoolean(false)
                            },
                            ["message"] = new OpenApiSchema
                            {
                                Type = "string",
                                Example = new OpenApiString(message)
                            },
                            ["data"] = new OpenApiSchema
                            {
                                Nullable = true,
                                Example = new OpenApiNull()
                            },
                            ["errors"] = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["code"] = new OpenApiSchema { Type = "string" },
                                        ["message"] = new OpenApiSchema { Type = "string" },
                                        ["field"] = new OpenApiSchema { Type = "string", Nullable = true }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
