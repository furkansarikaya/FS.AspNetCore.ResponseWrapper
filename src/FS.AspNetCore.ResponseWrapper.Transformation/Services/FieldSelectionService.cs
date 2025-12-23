using System.Text.Json;
using System.Text.Json.Nodes;

namespace FS.AspNetCore.ResponseWrapper.Transformation.Services;

/// <summary>
/// Service for selecting specific fields from response data
/// </summary>
public class FieldSelectionService
{
    /// <summary>
    /// Selects only specified fields from the data
    /// </summary>
    /// <param name="data">Data to filter</param>
    /// <param name="fields">Comma-separated field names (supports nested: user.name, user.email)</param>
    /// <returns>Filtered data containing only selected fields</returns>
    public object? SelectFields(object? data, string fields)
    {
        if (data == null || string.IsNullOrWhiteSpace(fields))
            return data;

        var fieldList = fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (fieldList.Length == 0)
            return data;

        // Serialize to JSON for manipulation
        var json = JsonSerializer.Serialize(data);
        var jsonNode = JsonNode.Parse(json);

        if (jsonNode == null)
            return data;

        var result = FilterNode(jsonNode, fieldList);

        // Deserialize back to object
        return result?.Deserialize<object>();
    }

    private JsonNode? FilterNode(JsonNode node, string[] fields)
    {
        if (node is JsonObject jsonObject)
        {
            return FilterObject(jsonObject, fields);
        }
        else if (node is JsonArray jsonArray)
        {
            var filteredArray = new JsonArray();
            foreach (var item in jsonArray)
            {
                if (item != null)
                {
                    var filtered = FilterNode(item, fields);
                    if (filtered != null)
                    {
                        filteredArray.Add(filtered);
                    }
                }
            }
            return filteredArray;
        }

        return node;
    }

    private JsonObject FilterObject(JsonObject obj, string[] fields)
    {
        var result = new JsonObject();

        foreach (var field in fields)
        {
            // Handle nested fields (e.g., "user.name")
            var parts = field.Split('.', 2);
            var currentField = parts[0];

            if (!obj.ContainsKey(currentField))
                continue;

            var value = obj[currentField];

            if (parts.Length == 1)
            {
                // Simple field
                if (value != null)
                {
                    result[currentField] = value.DeepClone();
                }
            }
            else
            {
                // Nested field
                var nestedFields = new[] { parts[1] };

                if (value is JsonObject nestedObj)
                {
                    var filtered = FilterObject(nestedObj, nestedFields);
                    result[currentField] = filtered;
                }
                else if (value is JsonArray nestedArray)
                {
                    var filteredArray = new JsonArray();
                    foreach (var item in nestedArray)
                    {
                        if (item is JsonObject itemObj)
                        {
                            var filtered = FilterObject(itemObj, nestedFields);
                            filteredArray.Add(filtered);
                        }
                    }
                    result[currentField] = filteredArray;
                }
            }
        }

        return result;
    }
}
