using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FS.AspNetCore.ResponseWrapper.OpenApi.Swashbuckle.Filters;

/// <summary>
/// Schema filter to add ApiResponse wrapper schema to OpenAPI documentation
/// </summary>
public class ResponseWrapperSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Add example values for ApiResponse models
        if (context.Type.Name == "ApiResponse" || context.Type.Name.StartsWith("ApiResponse`"))
        {
            schema.Example = null; // Let Swashbuckle generate from properties
        }
    }
}
