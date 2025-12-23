using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FS.AspNetCore.ResponseWrapper.Transformation.Attributes;
using FS.AspNetCore.ResponseWrapper.Transformation.Models;

namespace FS.AspNetCore.ResponseWrapper.Transformation.Services;

/// <summary>
/// Service for masking sensitive data in responses
/// </summary>
public class DataMaskingService
{
    private readonly TransformationOptions _options;
    private static readonly Regex EmailRegex = new(@"^[^@]+@[^@]+\.[^@]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-\(\)]+$", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"^\d{13,19}$", RegexOptions.Compiled);

    public DataMaskingService(TransformationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Masks sensitive data in an object
    /// </summary>
    public object? MaskData(object? data)
    {
        if (data == null || !_options.EnableDataMasking)
            return data;

        var dataType = data.GetType();

        // Handle primitive types and strings
        if (dataType.IsPrimitive || dataType == typeof(string) || dataType == typeof(DateTime))
            return data;

        // Handle collections
        if (data is System.Collections.IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(MaskData(item));
            }
            return list;
        }

        // Clone the object to avoid modifying the original
        var json = JsonSerializer.Serialize(data);
        var clone = JsonSerializer.Deserialize(json, dataType);

        if (clone == null)
            return data;

        // Process properties
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            var value = property.GetValue(clone);
            if (value == null)
                continue;

            // Check for MaskAttribute
            var maskAttr = property.GetCustomAttribute<MaskAttribute>();
            if (maskAttr != null)
            {
                var maskedValue = ApplyMasking(value.ToString() ?? "", maskAttr.MaskType, maskAttr.MaskChar);
                property.SetValue(clone, maskedValue);
                continue;
            }

            // Check for ExcludeAttribute
            var excludeAttr = property.GetCustomAttribute<ExcludeAttribute>();
            if (excludeAttr != null)
            {
                property.SetValue(clone, null);
                continue;
            }

            // Auto-detect sensitive property names
            if (_options.AutoMaskPropertyNames.Contains(property.Name))
            {
                var maskedValue = ApplyMasking(value.ToString() ?? "", MaskType.Full, null);
                property.SetValue(clone, maskedValue);
                continue;
            }

            // Auto-detect patterns
            var stringValue = value.ToString() ?? "";

            if (_options.AutoMaskEmails && EmailRegex.IsMatch(stringValue))
            {
                var maskedValue = ApplyMasking(stringValue, MaskType.Email, null);
                property.SetValue(clone, maskedValue);
                continue;
            }

            if (_options.AutoMaskPhoneNumbers && PhoneRegex.IsMatch(stringValue))
            {
                var maskedValue = ApplyMasking(stringValue, MaskType.Phone, null);
                property.SetValue(clone, maskedValue);
                continue;
            }

            if (_options.AutoMaskCreditCards && CreditCardRegex.IsMatch(stringValue.Replace("-", "").Replace(" ", "")))
            {
                var maskedValue = ApplyMasking(stringValue, MaskType.CreditCard, null);
                property.SetValue(clone, maskedValue);
                continue;
            }

            // Recursively mask nested objects
            if (!property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
            {
                var maskedNested = MaskData(value);
                property.SetValue(clone, maskedNested);
            }
        }

        return clone;
    }

    private string ApplyMasking(string value, MaskType maskType, char? customMaskChar)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var maskChar = customMaskChar ?? _options.MaskingCharacter;

        return maskType switch
        {
            MaskType.Full => new string(maskChar, value.Length),
            MaskType.Partial => MaskPartial(value, maskChar),
            MaskType.Email => MaskEmail(value, maskChar),
            MaskType.Phone => MaskPhone(value, maskChar),
            MaskType.CreditCard => MaskCreditCard(value, maskChar),
            _ => value
        };
    }

    private string MaskPartial(string value, char maskChar)
    {
        if (value.Length <= _options.MaskingVisibleChars * 2)
            return new string(maskChar, value.Length);

        var visibleChars = _options.MaskingVisibleChars;
        var start = value.Substring(0, visibleChars);
        var end = value.Substring(value.Length - visibleChars);
        var middle = new string(maskChar, value.Length - (visibleChars * 2));

        return $"{start}{middle}{end}";
    }

    private string MaskEmail(string email, char maskChar)
    {
        var parts = email.Split('@');
        if (parts.Length != 2)
            return new string(maskChar, email.Length);

        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length > 2
            ? $"{localPart[0]}{new string(maskChar, localPart.Length - 2)}{localPart[^1]}"
            : new string(maskChar, localPart.Length);

        var domainParts = domainPart.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? $"{domainParts[0][0]}{new string(maskChar, domainParts[0].Length - 1)}.{domainParts[^1]}"
            : new string(maskChar, domainPart.Length);

        return $"{maskedLocal}@{maskedDomain}";
    }

    private string MaskPhone(string phone, char maskChar)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 4)
            return new string(maskChar, phone.Length);

        var lastFour = digits[^4..];
        var masked = new string(maskChar, digits.Length - 4) + lastFour;

        return phone.Contains("-")
            ? FormatWithSeparator(masked, '-')
            : masked;
    }

    private string MaskCreditCard(string card, char maskChar)
    {
        var digits = new string(card.Where(char.IsDigit).ToArray());

        if (digits.Length < 4)
            return new string(maskChar, card.Length);

        var lastFour = digits[^4..];
        var masked = new string(maskChar, digits.Length - 4) + lastFour;

        return card.Contains("-")
            ? FormatCreditCard(masked)
            : masked;
    }

    private string FormatWithSeparator(string value, char separator)
    {
        if (value.Length <= 10)
            return value;

        return $"{value[..3]}{separator}{value[3..6]}{separator}{value[6..]}";
    }

    private string FormatCreditCard(string value)
    {
        if (value.Length != 16)
            return value;

        return $"{value[..4]}-{value[4..8]}-{value[8..12]}-{value[12..]}";
    }
}
