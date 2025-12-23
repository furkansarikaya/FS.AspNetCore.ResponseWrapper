using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Extensibility;

/// <summary>
/// Defines a contract for transforming response data before it is wrapped in an API response.
/// Extension packages (Transformation, FieldSelection, etc.) implement this interface to modify,
/// filter, or reshape response data based on request parameters or business logic.
/// </summary>
/// <remarks>
/// <para>
/// The IResponseTransformer interface provides a pre-wrapping hook that allows modification
/// of response data before the ResponseWrapper filter processes it. This is useful for scenarios
/// like field filtering (sparse fieldsets), case conversion, data masking, or dynamic data shaping.
/// </para>
///
/// <para><strong>Execution Timing:</strong></para>
/// <para>
/// Transformers execute BEFORE the response is wrapped, allowing them to modify the actual data
/// that will be included in the ApiResponse.Data property. This is in contrast to enrichers,
/// which run after wrapping and can only add metadata or headers.
/// </para>
///
/// <para><strong>Transformation Pipeline:</strong></para>
/// <para>
/// Multiple transformers can be registered and will execute in registration order. Each transformer
/// receives the output of the previous transformer, creating a transformation pipeline. Transformers
/// should use <see cref="CanTransform"/> to determine if they should process a given response type.
/// </para>
///
/// <para><strong>Common Use Cases:</strong></para>
/// <list type="bullet">
/// <item><description>Sparse fieldsets: Filter response to include only requested fields (e.g., ?fields=id,name)</description></item>
/// <item><description>Case conversion: Transform property names (camelCase, snake_case, PascalCase)</description></item>
/// <item><description>Data masking: Redact or mask sensitive fields based on user permissions</description></item>
/// <item><description>Null handling: Remove null properties or convert them to default values</description></item>
/// <item><description>Localization: Transform enum values or messages to user's language</description></item>
/// <item><description>Data flattening: Reshape nested objects into flat structures</description></item>
/// </list>
///
/// <para><strong>Thread Safety:</strong></para>
/// <para>
/// Implementations must be thread-safe as transformers are registered as singletons and may be
/// invoked concurrently across multiple requests. Avoid using instance state.
/// </para>
///
/// <para><strong>Performance Considerations:</strong></para>
/// <para>
/// Transformers execute in the response pipeline and can impact response times, especially
/// for large payloads. Consider:
/// </para>
/// <list type="bullet">
/// <item><description>Using efficient serialization/deserialization approaches</description></item>
/// <item><description>Caching transformation schemas per endpoint</description></item>
/// <item><description>Short-circuiting when transformation is not needed</description></item>
/// <item><description>Avoiding reflection when possible</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class FieldFilterTransformer : IResponseTransformer
/// {
///     public bool CanTransform(Type responseType) => true;
///
///     public object Transform(object data, HttpContext context)
///     {
///         // Check for ?fields= query parameter
///         if (!context.Request.Query.TryGetValue("fields", out var fieldsParam))
///             return data;
///
///         var requestedFields = fieldsParam.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
///         if (requestedFields.Length == 0)
///             return data;
///
///         // Serialize to JSON element for easy manipulation
///         var jsonElement = JsonSerializer.SerializeToElement(data);
///         var filtered = new Dictionary&lt;string, JsonElement&gt;();
///
///         foreach (var field in requestedFields)
///         {
///             if (jsonElement.TryGetProperty(field, out var value))
///             {
///                 filtered[field] = value;
///             }
///         }
///
///         return filtered;
///     }
/// }
///
/// // Register in Startup/Program.cs
/// services.AddResponseWrapper(options =>
/// {
///     options.ResponseTransformers.Add(new FieldFilterTransformer());
/// });
/// </code>
/// </example>
public interface IResponseTransformer
{
    /// <summary>
    /// Determines whether this transformer can process the specified response type.
    /// </summary>
    /// <param name="responseType">
    /// The runtime type of the response data that will be transformed. This is the
    /// actual type returned from the controller action, not the wrapped ApiResponse type.
    /// </param>
    /// <returns>
    /// <c>true</c> if this transformer can process the given type; otherwise, <c>false</c>.
    /// When multiple transformers are registered, only those returning <c>true</c> will
    /// be invoked for a given response.
    /// </returns>
    /// <remarks>
    /// <para><strong>Type Checking Strategies:</strong></para>
    /// <list type="bullet">
    /// <item><description>
    /// Return <c>true</c> to transform all responses (least selective, most flexible)
    /// </description></item>
    /// <item><description>
    /// Check for specific types: <c>responseType == typeof(MyDto)</c>
    /// </description></item>
    /// <item><description>
    /// Check for interface implementation: <c>typeof(ITransformable).IsAssignableFrom(responseType)</c>
    /// </description></item>
    /// <item><description>
    /// Check for generic types: <c>responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(List&lt;&gt;)</c>
    /// </description></item>
    /// <item><description>
    /// Exclude specific types: <c>responseType != typeof(FileResult)</c>
    /// </description></item>
    /// </list>
    ///
    /// <para><strong>Performance Tip:</strong></para>
    /// <para>
    /// This method is called for every response, so it should be fast. Avoid expensive
    /// reflection operations or I/O. Cache type checks if needed.
    /// </para>
    /// </remarks>
    bool CanTransform(Type responseType);

    /// <summary>
    /// Transforms the response data according to the transformer's logic.
    /// </summary>
    /// <param name="data">
    /// The original response data returned from the controller action. This can be any
    /// object type (DTO, entity, collection, primitive, etc.). The transformer should
    /// handle null values gracefully.
    /// </param>
    /// <param name="context">
    /// The HTTP context for the current request. Use this to:
    /// <list type="bullet">
    /// <item><description>Read query parameters via <c>context.Request.Query</c></description></item>
    /// <item><description>Check request headers via <c>context.Request.Headers</c></description></item>
    /// <item><description>Access route data via <c>context.Request.RouteValues</c></description></item>
    /// <item><description>Get endpoint metadata via <c>context.GetEndpoint()</c></description></item>
    /// <item><description>Retrieve services via <c>context.RequestServices</c></description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// The transformed data. This can be:
    /// <list type="bullet">
    /// <item><description>The same instance if no transformation is needed</description></item>
    /// <item><description>A modified copy of the original data</description></item>
    /// <item><description>A completely different object or structure</description></item>
    /// <item><description>null if the transformation results in no data</description></item>
    /// </list>
    /// The returned object will be serialized and included in ApiResponse.Data.
    /// </returns>
    /// <remarks>
    /// <para><strong>Transformation Guidelines:</strong></para>
    /// <list type="bullet">
    /// <item><description>
    /// Return the original data unchanged if transformation is not needed for the current request.
    /// This is more efficient than creating a copy.
    /// </description></item>
    /// <item><description>
    /// Handle null input gracefully. Return null or throw a descriptive exception.
    /// </description></item>
    /// <item><description>
    /// Preserve the semantic meaning of the data. Don't transform in ways that would
    /// break client assumptions about the data structure.
    /// </description></item>
    /// <item><description>
    /// Use immutable transformations when possible to avoid side effects.
    /// </description></item>
    /// <item><description>
    /// Document the transformation behavior clearly for API consumers.
    /// </description></item>
    /// </list>
    ///
    /// <para><strong>Error Handling:</strong></para>
    /// <para>
    /// If transformation fails, the transformer can:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Throw an exception (will result in error response to client)</description></item>
    /// <item><description>Return the original data (graceful degradation)</description></item>
    /// <item><description>Return null or empty structure (fail-safe)</description></item>
    /// <item><description>Log the error and return a partial transformation</description></item>
    /// </list>
    ///
    /// <para><strong>Security Considerations:</strong></para>
    /// <para>
    /// Be cautious when using user input (query parameters, headers) to drive transformation
    /// logic. Validate and sanitize all inputs to prevent injection attacks or unintended
    /// data exposure. Consider implementing allowlists for permitted transformations.
    /// </para>
    /// </remarks>
    object Transform(object data, HttpContext context);
}
