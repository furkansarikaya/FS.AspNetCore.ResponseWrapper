namespace FS.AspNetCore.ResponseWrapper.Transformation.Models;

/// <summary>
/// Options for response transformation integration with Response Wrapper
/// </summary>
public class TransformationOptions
{
    /// <summary>
    /// Enable response data transformation
    /// Default: true
    /// </summary>
    public bool EnableTransformation { get; set; } = true;

    /// <summary>
    /// Enable automatic data masking for sensitive fields
    /// Default: true
    /// </summary>
    public bool EnableDataMasking { get; set; } = true;

    /// <summary>
    /// Enable field selection/projection from query parameters
    /// Default: true
    /// </summary>
    public bool EnableFieldSelection { get; set; } = true;

    /// <summary>
    /// Query parameter name for field selection (e.g., ?fields=name,email)
    /// Default: "fields"
    /// </summary>
    public string FieldSelectionParameterName { get; set; } = "fields";

    /// <summary>
    /// Masking character for sensitive data
    /// Default: '*'
    /// </summary>
    public char MaskingCharacter { get; set; } = '*';

    /// <summary>
    /// Number of characters to show at start and end of masked string
    /// Default: 2 (e.g., "ab***@****.com")
    /// </summary>
    public int MaskingVisibleChars { get; set; } = 2;

    /// <summary>
    /// Enable automatic email masking detection
    /// Default: true
    /// </summary>
    public bool AutoMaskEmails { get; set; } = true;

    /// <summary>
    /// Enable automatic phone number masking detection
    /// Default: true
    /// </summary>
    public bool AutoMaskPhoneNumbers { get; set; } = true;

    /// <summary>
    /// Enable automatic credit card masking detection
    /// Default: true
    /// </summary>
    public bool AutoMaskCreditCards { get; set; } = true;

    /// <summary>
    /// List of property names to automatically mask (case-insensitive)
    /// Default: password, ssn, creditcard, cvv
    /// </summary>
    public HashSet<string> AutoMaskPropertyNames { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "ssn",
        "creditcard",
        "cvv",
        "secret",
        "token",
        "apikey"
    };
}
