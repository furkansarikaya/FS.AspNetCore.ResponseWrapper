using FS.AspNetCore.ResponseWrapper.Extensibility;
using FS.AspNetCore.ResponseWrapper.Models;
using FS.AspNetCore.ResponseWrapper.Transformation.Models;
using FS.AspNetCore.ResponseWrapper.Transformation.Services;
using Microsoft.AspNetCore.Http;

namespace FS.AspNetCore.ResponseWrapper.Transformation.Enrichers;

/// <summary>
/// Enricher that applies data transformations to responses
/// </summary>
public class TransformationEnricher : IResponseEnricher
{
    private readonly TransformationOptions _options;
    private readonly DataMaskingService _maskingService;
    private readonly FieldSelectionService _fieldSelectionService;

    /// <summary>
    /// Execution order - runs early before caching (40)
    /// </summary>
    public int Order => 40;

    public TransformationEnricher(
        TransformationOptions options,
        DataMaskingService maskingService,
        FieldSelectionService fieldSelectionService)
    {
        _options = options;
        _maskingService = maskingService;
        _fieldSelectionService = fieldSelectionService;
    }

    public Task EnrichAsync<T>(ApiResponse<T> response, HttpContext context)
    {
        if (!_options.EnableTransformation || response.Data == null)
            return Task.CompletedTask;

        var data = response.Data;

        // Apply field selection if requested
        if (_options.EnableFieldSelection)
        {
            var fieldsParam = context.Request.Query[_options.FieldSelectionParameterName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fieldsParam))
            {
                var filtered = _fieldSelectionService.SelectFields(data, fieldsParam);
                if (filtered != null)
                {
                    response.Data = (T)filtered;
                    data = response.Data;
                }
            }
        }

        // Apply data masking
        if (_options.EnableDataMasking)
        {
            var masked = _maskingService.MaskData(data);
            if (masked != null)
            {
                response.Data = (T)masked;
            }
        }

        return Task.CompletedTask;
    }
}
