namespace FS.AspNetCore.ResponseWrapper.Transformation.Attributes;

/// <summary>
/// Marks a property for data masking in responses
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MaskAttribute : Attribute
{
    /// <summary>
    /// Type of masking to apply
    /// </summary>
    public MaskType MaskType { get; set; } = MaskType.Full;

    /// <summary>
    /// Number of visible characters at start and end (for Partial masking)
    /// </summary>
    public int VisibleChars { get; set; } = 2;

    /// <summary>
    /// Custom masking character
    /// </summary>
    public char? MaskChar { get; set; }

    public MaskAttribute()
    {
    }

    public MaskAttribute(MaskType maskType)
    {
        MaskType = maskType;
    }
}

/// <summary>
/// Types of data masking
/// </summary>
public enum MaskType
{
    /// <summary>
    /// Fully mask the value (e.g., "******")
    /// </summary>
    Full,

    /// <summary>
    /// Partially mask showing first and last characters (e.g., "ab***@****.com")
    /// </summary>
    Partial,

    /// <summary>
    /// Mask as email format (e.g., "u***@d***.com")
    /// </summary>
    Email,

    /// <summary>
    /// Mask as phone number (e.g., "***-***-1234")
    /// </summary>
    Phone,

    /// <summary>
    /// Mask as credit card (e.g., "****-****-****-1234")
    /// </summary>
    CreditCard
}
