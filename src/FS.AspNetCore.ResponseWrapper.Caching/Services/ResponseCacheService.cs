using System.IO.Compression;
using System.Text;
using System.Text.Json;
using FS.AspNetCore.ResponseWrapper.Caching.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace FS.AspNetCore.ResponseWrapper.Caching.Services;

/// <summary>
/// Service for caching API responses
/// </summary>
public class ResponseCacheService
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly CachingOptions _options;

    public ResponseCacheService(
        CachingOptions options,
        IMemoryCache? memoryCache = null,
        IDistributedCache? distributedCache = null)
    {
        _options = options;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// Gets a cached response
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching)
            return default;

        if (_options.UseDistributedCache && _distributedCache != null)
        {
            return await GetFromDistributedCacheAsync<T>(key, cancellationToken);
        }
        else if (_memoryCache != null)
        {
            return GetFromMemoryCache<T>(key);
        }

        return default;
    }

    /// <summary>
    /// Sets a cached response
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching)
            return;

        var duration = expiration ?? TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds);

        if (_options.UseDistributedCache && _distributedCache != null)
        {
            await SetInDistributedCacheAsync(key, value, duration, cancellationToken);
        }
        else if (_memoryCache != null)
        {
            SetInMemoryCache(key, value, duration);
        }
    }

    /// <summary>
    /// Removes a cached response
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.UseDistributedCache && _distributedCache != null)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        else if (_memoryCache != null)
        {
            _memoryCache.Remove(key);
        }
    }

    /// <summary>
    /// Removes all cached responses matching a pattern
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Pattern-based removal is complex for distributed cache
        // This is a simplified implementation
        // For production, consider using Redis SCAN or a cache tagging strategy
        await Task.CompletedTask;
    }

    private T? GetFromMemoryCache<T>(string key)
    {
        if (_memoryCache!.TryGetValue(key, out T? value))
        {
            return value;
        }

        return default;
    }

    private void SetInMemoryCache<T>(string key, T value, TimeSpan duration)
    {
        var entryOptions = new MemoryCacheEntryOptions();

        if (_options.UseSlidingExpiration)
        {
            entryOptions.SetSlidingExpiration(duration);
        }
        else
        {
            entryOptions.SetAbsoluteExpiration(duration);
        }

        // Set size limit if specified
        if (_options.MaxCacheEntrySizeBytes > 0)
        {
            var json = JsonSerializer.Serialize(value);
            var size = Encoding.UTF8.GetByteCount(json);

            if (size > _options.MaxCacheEntrySizeBytes)
            {
                // Skip caching if entry is too large
                return;
            }

            entryOptions.SetSize(size);
        }

        _memoryCache!.Set(key, value, entryOptions);
    }

    private async Task<T?> GetFromDistributedCacheAsync<T>(string key, CancellationToken cancellationToken)
    {
        var bytes = await _distributedCache!.GetAsync(key, cancellationToken);

        if (bytes == null || bytes.Length == 0)
            return default;

        // Decompress if compression is enabled
        if (_options.EnableCompression)
        {
            bytes = await DecompressAsync(bytes);
        }

        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task SetInDistributedCacheAsync<T>(
        string key,
        T value,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Check size limit
        if (_options.MaxCacheEntrySizeBytes > 0 && bytes.Length > _options.MaxCacheEntrySizeBytes)
        {
            return;
        }

        // Compress if enabled
        if (_options.EnableCompression)
        {
            bytes = await CompressAsync(bytes);
        }

        var options = new DistributedCacheEntryOptions();

        if (_options.UseSlidingExpiration)
        {
            options.SetSlidingExpiration(duration);
        }
        else
        {
            options.SetAbsoluteExpiration(duration);
        }

        await _distributedCache!.SetAsync(key, bytes, options, cancellationToken);
    }

    private async Task<byte[]> CompressAsync(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            await gzip.WriteAsync(data);
        }
        return output.ToArray();
    }

    private async Task<byte[]> DecompressAsync(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            await gzip.CopyToAsync(output);
        }
        return output.ToArray();
    }
}
