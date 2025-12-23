namespace FS.AspNetCore.ResponseWrapper.Extensions.Models;

/// <summary>
/// Predefined configuration presets for common scenarios
/// </summary>
public enum PresetType
{
    /// <summary>
    /// Minimal configuration - Core features only
    /// </summary>
    Minimal,

    /// <summary>
    /// Basic configuration - Core + OpenAPI documentation
    /// </summary>
    Basic,

    /// <summary>
    /// Standard configuration - Core + OpenAPI + Caching
    /// </summary>
    Standard,

    /// <summary>
    /// Advanced configuration - All features except GDPR
    /// </summary>
    Advanced,

    /// <summary>
    /// Full enterprise stack - All features enabled
    /// </summary>
    Enterprise,

    /// <summary>
    /// GDPR compliant configuration - Privacy-first settings
    /// </summary>
    GDPRCompliant,

    /// <summary>
    /// Performance optimized - Caching and transformations
    /// </summary>
    Performance,

    /// <summary>
    /// Development mode - Verbose logging and debugging features
    /// </summary>
    Development,

    /// <summary>
    /// Production mode - Optimized for production workloads
    /// </summary>
    Production
}
