using FS.AspNetCore.ResponseWrapper.OpenApi.NSwag.Models;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.NSwag.Processors;

/// <summary>
/// Operation processor to wrap API responses with ApiResponse wrapper in OpenAPI documentation
/// </summary>
public class ResponseWrapperOperationProcessor : IOperationProcessor
{
    private readonly OpenApiResponseWrapperOptions _options;

    public ResponseWrapperOperationProcessor(OpenApiResponseWrapperOptions options)
    {
        _options = options;
    }

    public bool Process(OperationProcessorContext context)
    {
        if (!_options.AutoWrapResponses)
            return true;

        var responsesToWrap = new List<KeyValuePair<string, OpenApiResponse>>();

        foreach (var response in context.OperationDescription.Operation.Responses)
        {
            var statusCode = int.TryParse(response.Key, out var code) ? code : 0;

            // Skip excluded status codes
            if (statusCode > 0 && _options.ExcludedStatusCodes.Contains(statusCode))
                continue;

            // Skip if already wrapped
            if (IsAlreadyWrapped(response.Value))
                continue;

            responsesToWrap.Add(new KeyValuePair<string, OpenApiResponse>(response.Key, response.Value));
        }

        // Wrap responses
        foreach (var kvp in responsesToWrap)
        {
            WrapResponse(kvp.Value, context);
        }

        // Add error response examples if enabled
        if (_options.IncludeErrorExamples)
        {
            AddErrorResponseExamples(context.OperationDescription.Operation);
        }

        return true;
    }

    private bool IsAlreadyWrapped(OpenApiResponse response)
    {
        if (response.Schema == null)
            return false;

        // Check if schema has ApiResponse structure
        return response.Schema.ActualSchema.Properties.ContainsKey("success") &&
               response.Schema.ActualSchema.Properties.ContainsKey("data") &&
               response.Schema.ActualSchema.Properties.ContainsKey("errors");
    }

    private void WrapResponse(OpenApiResponse response, OperationProcessorContext context)
    {
        var originalSchema = response.Schema;

        if (originalSchema == null)
            return;

        // Create wrapped schema
        var wrappedSchema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Description = "API Response wrapper"
        };

        // Add properties
        wrappedSchema.Properties["success"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Boolean,
            Description = "Indicates whether the request was successful",
            IsRequired = true
        };

        wrappedSchema.Properties["message"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Description = "Response message",
            IsNullableRaw = true
        };

        // Add data property with the original schema
        var dataProperty = new JsonSchemaProperty
        {
            Description = "Response data"
        };
        if (originalSchema.ActualSchema != null)
        {
            dataProperty.Reference = originalSchema.ActualSchema;
        }
        wrappedSchema.Properties["data"] = dataProperty;

        // Add errors array
        wrappedSchema.Properties["errors"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Description = "List of errors if any",
            IsNullableRaw = true,
            Item = new JsonSchema
            {
                Type = JsonObjectType.Object,
                Properties =
                {
                    ["code"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String,
                        Description = "Error code"
                    },
                    ["message"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String,
                        Description = "Error message"
                    },
                    ["field"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String,
                        Description = "Field name if validation error",
                        IsNullableRaw = true
                    }
                }
            }
        };

        // Add metadata if enabled
        if (_options.IncludeMetadataSchema)
        {
            wrappedSchema.Properties["metadata"] = new JsonSchemaProperty
            {
                Type = JsonObjectType.Object,
                Description = "Additional metadata about the response",
                IsNullableRaw = true,
                AllowAdditionalProperties = true
            };
        }

        response.Schema = wrappedSchema;
    }

    private void AddErrorResponseExamples(OpenApiOperation operation)
    {
        // Add 400 Bad Request example if not exists
        if (!operation.Responses.ContainsKey("400"))
        {
            operation.Responses["400"] = CreateErrorResponse(
                "Bad Request - Validation failed");
        }

        // Add 401 Unauthorized example if not exists
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses["401"] = CreateErrorResponse(
                "Unauthorized - Authentication required");
        }

        // Add 500 Internal Server Error example if not exists
        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses["500"] = CreateErrorResponse(
                "Internal Server Error - An unexpected error occurred");
        }
    }

    private OpenApiResponse CreateErrorResponse(string description)
    {
        return new OpenApiResponse
        {
            Description = description,
            Schema = new JsonSchema
            {
                Type = JsonObjectType.Object,
                Properties =
                {
                    ["success"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.Boolean,
                        IsRequired = true
                    },
                    ["message"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String
                    },
                    ["data"] = new JsonSchemaProperty
                    {
                        IsNullableRaw = true
                    },
                    ["errors"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.Array,
                        Item = new JsonSchema
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                ["code"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                                ["message"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                                ["field"] = new JsonSchemaProperty { Type = JsonObjectType.String, IsNullableRaw = true }
                            }
                        }
                    }
                }
            }
        };
    }
}
