using System.Collections.Concurrent;
using System.Reflection;
using FS.AspNetCore.ResponseWrapper.Models;

namespace FS.AspNetCore.ResponseWrapper.Helpers;

/// <summary>
/// Provides flexible pagination detection that works with any object having pagination properties,
/// regardless of the specific interface implementation. This helper uses duck typing principles
/// to detect pagination information from any compatible object structure.
/// </summary>
/// <remarks>
/// This class solves the common problem where users might have their own pagination interfaces
/// or classes with different namespaces but identical structure. Instead of requiring strict
/// interface implementation, it uses reflection to detect the presence of required properties.
/// 
/// The duck typing approach follows the principle: "If it walks like a duck and quacks like a duck,
/// then it must be a duck." In our case, if an object has the pagination properties we expect,
/// we treat it as a paged result regardless of its specific type declaration.
/// 
/// This provides maximum flexibility while maintaining type safety through careful validation
/// of property types and accessibility.
/// </remarks>
public static class PaginationDetectionHelper
{
    /// <summary>
    /// Cache for storing reflection results to improve performance on repeated calls.
    /// This prevents the overhead of reflection analysis for the same types across requests.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PaginationTypeInfo> TypeCache = new();

    /// <summary>
    /// Required property names for pagination detection. These represent the minimal set
    /// of properties needed to extract comprehensive pagination metadata.
    /// </summary>
    private static readonly string[] RequiredPaginationProperties = 
    {
        "Page", "PageSize", "TotalPages", "TotalItems", "HasNextPage", "HasPreviousPage"
    };

    /// <summary>
    /// Detects if an object contains pagination information using duck typing principles.
    /// This method analyzes the object's type to determine if it has the expected pagination properties.
    /// </summary>
    /// <param name="data">The object to analyze for pagination properties</param>
    /// <returns>True if the object has pagination properties; otherwise, false</returns>
    /// <remarks>
    /// This method performs a comprehensive analysis of the object's type structure:
    /// 1. Checks for the presence of all required pagination properties
    /// 2. Validates that property types match expected types (int, bool)
    /// 3. Ensures properties are readable (have public getters)
    /// 4. Caches results for performance optimization
    /// 
    /// The detection is designed to be permissive - it accepts any object structure that
    /// contains the required properties with correct types, regardless of inheritance
    /// hierarchy, interface implementation, or namespace origin.
    /// </remarks>
    /// <example>
    /// <code>
    /// // This would return true for any of these object types:
    /// // - FS.AspNetCore.ResponseWrapper.Models.Paging.PagedResult&lt;T&gt;
    /// // - MyProject.Models.PagedResponse&lt;T&gt;  
    /// // - ThirdParty.Library.PaginatedResult&lt;T&gt;
    /// // As long as they have the required properties
    /// 
    /// var isPagedResult = PaginationDetectionHelper.HasPaginationProperties(myData);
    /// </code>
    /// </example>
    public static bool HasPaginationProperties(object data)
    {
        if (data == null) return false;

        var type = data.GetType();
        var typeInfo = GetOrCreateTypeInfo(type);
        
        return typeInfo.IsPaginationCompatible;
    }

    /// <summary>
    /// Extracts pagination metadata from any object that has compatible pagination properties.
    /// This method uses reflection to safely extract values regardless of the specific type implementation.
    /// </summary>
    /// <param name="data">The object containing pagination information</param>
    /// <returns>PaginationMetadata extracted from the object, or null if extraction fails</returns>
    /// <remarks>
    /// This method provides safe value extraction with comprehensive error handling:
    /// 1. Validates that the object has pagination properties before attempting extraction
    /// 2. Uses cached reflection information for optimal performance
    /// 3. Handles type conversion and null value scenarios gracefully
    /// 4. Returns null rather than throwing exceptions for invalid data
    /// 
    /// The extraction process is designed to be defensive - it assumes that property values
    /// might not always be in the expected format and handles edge cases appropriately.
    /// This ensures that the pagination detection doesn't break the application even
    /// when encountering unexpected object structures.
    /// </remarks>
    /// <example>
    /// <code>
    /// var paginationInfo = PaginationDetectionHelper.ExtractPaginationMetadata(pagedData);
    /// if (paginationInfo != null)
    /// {
    ///     // Successfully extracted pagination information
    ///     Console.WriteLine($"Page {paginationInfo.Page} of {paginationInfo.TotalPages}");
    /// }
    /// </code>
    /// </example>
    public static PaginationMetadata? ExtractPaginationMetadata(object data)
    {
        if (data == null) return null;

        var type = data.GetType();
        var typeInfo = GetOrCreateTypeInfo(type);
        
        if (!typeInfo.IsPaginationCompatible) return null;

        try
        {
            // Extract values using cached property information for optimal performance
            var page = (int)(typeInfo.PageProperty?.GetValue(data) ?? 0);
            var pageSize = (int)(typeInfo.PageSizeProperty?.GetValue(data) ?? 0);
            var totalPages = (int)(typeInfo.TotalPagesProperty?.GetValue(data) ?? 0);
            var totalItems = (int)(typeInfo.TotalItemsProperty?.GetValue(data) ?? 0);
            var hasNextPage = (bool)(typeInfo.HasNextPageProperty?.GetValue(data) ?? false);
            var hasPreviousPage = (bool)(typeInfo.HasPreviousPageProperty?.GetValue(data) ?? false);

            return new PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage
            };
        }
        catch (Exception)
        {
            // If value extraction fails for any reason, return null rather than crashing
            // This maintains system stability even with unexpected object structures
            return null;
        }
    }

    /// <summary>
    /// Extracts the items collection from a paginated object, regardless of its specific type structure.
    /// This method looks for an "Items" property and extracts its value for clean data presentation.
    /// </summary>
    /// <param name="data">The paginated object containing an items collection</param>
    /// <returns>Tuple containing the extracted items and their type, or original data if extraction fails</returns>
    /// <remarks>
    /// This method implements the separation of concerns principle by extracting business data
    /// from pagination metadata. The extraction process:
    /// 1. Looks for a property named "Items" on the provided object
    /// 2. Extracts the items collection for clean API responses
    /// 3. Determines the item type for proper generic type construction
    /// 4. Falls back gracefully to the original object if extraction isn't possible
    /// 
    /// The method is designed to work with any pagination structure that follows the common
    /// pattern of having an "Items" property containing the actual business data. This pattern
    /// is used by most pagination libraries and custom implementations.
    /// </remarks>
    /// <example>
    /// <code>
    /// var (cleanItems, itemType) = PaginationDetectionHelper.ExtractItems(pagedResult);
    /// // cleanItems now contains just the business data without pagination metadata
    /// // itemType contains the type information for proper response construction
    /// </code>
    /// </example>
    public static (object items, Type itemType) ExtractItems(object data)
    {
        if (data == null) return (data, data?.GetType() ?? typeof(object));

        var type = data.GetType();
        var itemsProperty = type.GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
        
        if (itemsProperty == null) 
            return (data, type);

        var items = itemsProperty.GetValue(data);
        if (items == null) 
            return (data, type);

        // Determine the item type from the Items property's generic type
        var itemsType = itemsProperty.PropertyType;
        var itemType = GetCollectionItemType(itemsType);

        return (items, itemType);
    }

    /// <summary>
    /// Gets or creates cached type information for pagination detection.
    /// This method implements a caching strategy to avoid repeated reflection overhead.
    /// </summary>
    /// <param name="type">The type to analyze</param>
    /// <returns>Cached type information with pagination compatibility details</returns>
    private static PaginationTypeInfo GetOrCreateTypeInfo(Type type)
    {
        return TypeCache.GetOrAdd(type, AnalyzeTypeForPagination);
    }

    /// <summary>
    /// Analyzes a type to determine its pagination compatibility and caches property information.
    /// This method performs the actual reflection work to understand the type structure.
    /// </summary>
    /// <param name="type">The type to analyze for pagination properties</param>
    /// <returns>Type information including compatibility status and property references</returns>
    private static PaginationTypeInfo AnalyzeTypeForPagination(Type type)
    {
        var typeInfo = new PaginationTypeInfo { Type = type };

        try
        {
            // Use reflection to find all required pagination properties
            typeInfo.PageProperty = GetCompatibleProperty(type, "Page", typeof(int));
            typeInfo.PageSizeProperty = GetCompatibleProperty(type, "PageSize", typeof(int));
            typeInfo.TotalPagesProperty = GetCompatibleProperty(type, "TotalPages", typeof(int));
            typeInfo.TotalItemsProperty = GetCompatibleProperty(type, "TotalItems", typeof(int));
            typeInfo.HasNextPageProperty = GetCompatibleProperty(type, "HasNextPage", typeof(bool));
            typeInfo.HasPreviousPageProperty = GetCompatibleProperty(type, "HasPreviousPage", typeof(bool));

            // Type is pagination-compatible if all required properties are present and accessible
            typeInfo.IsPaginationCompatible = 
                typeInfo.PageProperty != null &&
                typeInfo.PageSizeProperty != null &&
                typeInfo.TotalPagesProperty != null &&
                typeInfo.TotalItemsProperty != null &&
                typeInfo.HasNextPageProperty != null &&
                typeInfo.HasPreviousPageProperty != null;
        }
        catch
        {
            // If reflection fails for any reason, mark as incompatible
            typeInfo.IsPaginationCompatible = false;
        }

        return typeInfo;
    }

    /// <summary>
    /// Finds a property with the specified name and type, ensuring it's readable.
    /// This method provides type-safe property discovery with accessibility validation.
    /// </summary>
    /// <param name="type">The type to search for the property</param>
    /// <param name="propertyName">The name of the property to find</param>
    /// <param name="expectedType">The expected type of the property</param>
    /// <returns>PropertyInfo if found and compatible, null otherwise</returns>
    private static PropertyInfo? GetCompatibleProperty(Type type, string propertyName, Type expectedType)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        
        if (property == null) return null;
        if (!property.CanRead) return null;
        if (property.PropertyType != expectedType) return null;

        return property;
    }

    /// <summary>
    /// Determines the item type from a collection type, handling various collection interfaces.
    /// This method provides robust type discovery for generic collections.
    /// </summary>
    /// <param name="collectionType">The collection type to analyze</param>
    /// <returns>The item type if determinable, object type as fallback</returns>
    private static Type GetCollectionItemType(Type collectionType)
    {
        // Handle generic types like List<T>, IEnumerable<T>, etc.
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
                return genericArgs[0];
        }

        // Handle array types
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType() ?? typeof(object);
        }

        // Fallback for non-generic collections
        return typeof(object);
    }

    /// <summary>
    /// Internal class for caching type analysis results to optimize repeated operations.
    /// This class encapsulates all the reflection information needed for pagination processing.
    /// </summary>
    private class PaginationTypeInfo
    {
        public Type Type { get; set; } = typeof(object);
        public bool IsPaginationCompatible { get; set; }
        public PropertyInfo? PageProperty { get; set; }
        public PropertyInfo? PageSizeProperty { get; set; }
        public PropertyInfo? TotalPagesProperty { get; set; }
        public PropertyInfo? TotalItemsProperty { get; set; }
        public PropertyInfo? HasNextPageProperty { get; set; }
        public PropertyInfo? HasPreviousPageProperty { get; set; }
    }
}