using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Filters;

/// <summary>
/// Schema filter to enhance ApiResponse wrapper schema in OpenAPI documentation
/// </summary>
public class ResponseWrapperSchemaFilter : ISchemaFilter
{
    private readonly OpenApiResponseWrapperOptions _options;

    public ResponseWrapperSchemaFilter(OpenApiResponseWrapperOptions options)
    {
        _options = options;
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Handle ApiResponse models
        if (context.Type.Name == "ApiResponse" || context.Type.Name.StartsWith("ApiResponse`"))
        {
            EnhanceApiResponseSchema(schema, context);
        }
        // Handle ResponseMetadata
        else if (context.Type == typeof(ResponseMetadata))
        {
            EnhanceResponseMetadataSchema(schema);
        }
        // Handle PaginationMetadata
        else if (context.Type.Name == "PaginationMetadata")
        {
            EnhancePaginationMetadataSchema(schema);
        }
    }

    private void EnhanceApiResponseSchema(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Add detailed descriptions
        if (schema.Properties.ContainsKey("success"))
        {
            schema.Properties["success"].Description = "Indicates whether the operation completed successfully";
        }

        if (schema.Properties.ContainsKey("data"))
        {
            schema.Properties["data"].Description = "The actual response data returned by the operation";
        }

        if (schema.Properties.ContainsKey("message"))
        {
            schema.Properties["message"].Description = "Optional human-readable message about the operation result";
        }

        if (schema.Properties.ContainsKey("errors"))
        {
            schema.Properties["errors"].Description = "Collection of error messages (only present when success is false)";
        }

        if (schema.Properties.ContainsKey("metadata"))
        {
            schema.Properties["metadata"].Description = "Additional metadata about the request/response processing";
        }

        if (schema.Properties.ContainsKey("statusCode"))
        {
            schema.Properties["statusCode"].Description = "Application-specific status code for complex workflow handling";
        }

        // Add example if configured
        if (_options.IncludeExamples)
        {
            AddApiResponseExample(schema, context);
        }
    }

    private void EnhanceResponseMetadataSchema(OpenApiSchema schema)
    {
        // Add detailed descriptions for metadata properties
        if (schema.Properties.ContainsKey("requestId"))
        {
            schema.Properties["requestId"].Description = "Unique identifier for this request (useful for log correlation)";
        }

        if (schema.Properties.ContainsKey("timestamp"))
        {
            schema.Properties["timestamp"].Description = "UTC timestamp when the response was generated";
        }

        if (schema.Properties.ContainsKey("executionTimeMs"))
        {
            schema.Properties["executionTimeMs"].Description = "Total execution time in milliseconds";
        }

        if (schema.Properties.ContainsKey("correlationId"))
        {
            schema.Properties["correlationId"].Description = "Correlation ID for distributed tracing";
        }

        if (schema.Properties.ContainsKey("path"))
        {
            schema.Properties["path"].Description = "Request path that was processed";
        }

        if (schema.Properties.ContainsKey("method"))
        {
            schema.Properties["method"].Description = "HTTP method used for the request";
        }
    }

    private void EnhancePaginationMetadataSchema(OpenApiSchema schema)
    {
        schema.Description = "Pagination information for list endpoints";

        if (schema.Properties.ContainsKey("page"))
        {
            schema.Properties["page"].Description = "Current page number (1-based)";
        }

        if (schema.Properties.ContainsKey("pageSize"))
        {
            schema.Properties["pageSize"].Description = "Number of items per page";
        }

        if (schema.Properties.ContainsKey("totalCount"))
        {
            schema.Properties["totalCount"].Description = "Total number of items across all pages";
        }

        if (schema.Properties.ContainsKey("totalPages"))
        {
            schema.Properties["totalPages"].Description = "Total number of pages available";
        }

        if (schema.Properties.ContainsKey("hasNextPage"))
        {
            schema.Properties["hasNextPage"].Description = "Indicates whether there are more pages after this one";
        }

        if (schema.Properties.ContainsKey("hasPreviousPage"))
        {
            schema.Properties["hasPreviousPage"].Description = "Indicates whether there are pages before this one";
        }
    }

    private void AddApiResponseExample(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Create example for successful response
        var exampleObject = new OpenApiObject
        {
            ["success"] = new OpenApiBoolean(true),
            ["data"] = new OpenApiObject
            {
                ["id"] = new OpenApiInteger(1),
                ["name"] = new OpenApiString("Example Data")
            },
            ["message"] = new OpenApiString("Operation completed successfully"),
            ["metadata"] = new OpenApiObject
            {
                ["requestId"] = new OpenApiString("req-123456789"),
                ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O")),
                ["executionTimeMs"] = new OpenApiInteger(45),
                ["path"] = new OpenApiString("/api/example"),
                ["method"] = new OpenApiString("GET")
            }
        };

        schema.Example = exampleObject;
    }
}
