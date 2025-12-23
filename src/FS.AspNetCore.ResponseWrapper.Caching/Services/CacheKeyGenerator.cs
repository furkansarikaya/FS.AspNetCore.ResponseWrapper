using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Caching.Services;

/// <summary>
/// Generates cache keys for responses based on request characteristics
/// </summary>
public class CacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key from HTTP context
    /// </summary>
    public string GenerateKey(HttpContext context, string prefix = "rw:")
    {
        var keyBuilder = new StringBuilder();

        keyBuilder.Append(prefix);
        keyBuilder.Append(context.Request.Method);
        keyBuilder.Append(":");
        keyBuilder.Append(context.Request.Path.Value);

        // Include query string in cache key
        if (context.Request.QueryString.HasValue)
        {
            keyBuilder.Append(context.Request.QueryString.Value);
        }

        // Include user identity for user-specific caching
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                keyBuilder.Append(":user:");
                keyBuilder.Append(userId);
            }
        }

        // Hash the key if it's too long
        var key = keyBuilder.ToString();
        if (key.Length > 250)
        {
            return prefix + ComputeHash(key);
        }

        return key;
    }

    /// <summary>
    /// Computes SHA256 hash of the input string
    /// </summary>
    private string ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Generates an ETag from response data
    /// </summary>
    public string GenerateETag(object? data)
    {
        if (data == null)
            return string.Empty;

        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var hash = ComputeHash(json);
        return $"\"{hash}\"";
    }
}
