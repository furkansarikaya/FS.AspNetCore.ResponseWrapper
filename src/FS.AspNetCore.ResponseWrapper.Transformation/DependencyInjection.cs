using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.Transformation.Enrichers;
using FS.AspNetCore.ResponseWrapper.Transformation.Models;
using FS.AspNetCore.ResponseWrapper.Transformation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AspNetCore.ResponseWrapper.Transformation;

/// <summary>
/// Extension methods for configuring transformation integration with Response Wrapper
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds transformation integration to Response Wrapper
    /// </summary>
    /// <param name="options">Response Wrapper options</param>
    /// <param name="configureOptions">Optional configuration for transformation integration</param>
    /// <returns>The same ResponseWrapperOptions instance for method chaining</returns>
    public static ResponseWrapperOptions AddTransformation(
        this ResponseWrapperOptions options,
        Action<TransformationOptions>? configureOptions = null)
    {
        var transformationOptions = new TransformationOptions();
        configureOptions?.Invoke(transformationOptions);

        // Services will be registered separately in DI container
        // This just configures the enricher

        return options;
    }

    /// <summary>
    /// Adds transformation services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Optional configuration for transformation</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperTransformation(
        this IServiceCollection services,
        Action<TransformationOptions>? configureOptions = null)
    {
        var transformationOptions = new TransformationOptions();
        configureOptions?.Invoke(transformationOptions);

        // Register transformation options as singleton
        services.AddSingleton(transformationOptions);

        // Register transformation services
        services.AddSingleton<DataMaskingService>();
        services.AddSingleton<FieldSelectionService>();

        // Configure Response Wrapper to use transformation
        services.Configure<ResponseWrapperOptions>(opts =>
        {
            var serviceProvider = services.BuildServiceProvider();
            opts.ResponseEnrichers.Add(new TransformationEnricher(
                transformationOptions,
                serviceProvider.GetRequiredService<DataMaskingService>(),
                serviceProvider.GetRequiredService<FieldSelectionService>()
            ));
        });

        return services;
    }

    /// <summary>
    /// Adds transformation with data masking only
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="maskingCharacter">Character to use for masking</param>
    /// <param name="visibleChars">Number of visible characters at start/end</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperDataMasking(
        this IServiceCollection services,
        char maskingCharacter = '*',
        int visibleChars = 2)
    {
        return services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = false;
            opts.MaskingCharacter = maskingCharacter;
            opts.MaskingVisibleChars = visibleChars;
        });
    }

    /// <summary>
    /// Adds transformation with field selection only
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="parameterName">Query parameter name for field selection</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperFieldSelection(
        this IServiceCollection services,
        string parameterName = "fields")
    {
        return services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = false;
            opts.EnableFieldSelection = true;
            opts.FieldSelectionParameterName = parameterName;
        });
    }

    /// <summary>
    /// Adds GDPR-compliant transformation (strict data masking)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>The same IServiceCollection instance for method chaining</returns>
    public static IServiceCollection AddResponseWrapperGDPRCompliance(
        this IServiceCollection services)
    {
        return services.AddResponseWrapperTransformation(opts =>
        {
            opts.EnableDataMasking = true;
            opts.EnableFieldSelection = true;
            opts.AutoMaskEmails = true;
            opts.AutoMaskPhoneNumbers = true;
            opts.AutoMaskCreditCards = true;
            opts.MaskingVisibleChars = 0; // Full masking for GDPR
            opts.AutoMaskPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password",
                "ssn",
                "creditcard",
                "cvv",
                "secret",
                "token",
                "apikey",
                "email",
                "phone",
                "address",
                "ipaddress",
                "location",
                "birthdate",
                "dob"
            };
        });
    }
}
