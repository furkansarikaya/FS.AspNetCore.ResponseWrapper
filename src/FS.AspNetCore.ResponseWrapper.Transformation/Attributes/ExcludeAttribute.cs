namespace FS.AspNetCore.ResponseWrapper.Transformation.Attributes;

/// <summary>
/// Marks a property to be excluded from responses
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExcludeAttribute : Attribute
{
    /// <summary>
    /// Reason for exclusion (for documentation)
    /// </summary>
    public string? Reason { get; set; }

    public ExcludeAttribute()
    {
    }

    public ExcludeAttribute(string reason)
    {
        Reason = reason;
    }
}
