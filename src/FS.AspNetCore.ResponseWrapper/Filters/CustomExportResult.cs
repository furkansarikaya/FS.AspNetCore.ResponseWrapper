using Microsoft.AspNetCore.Mvc;

namespace FS.AspNetCore.ResponseWrapper.Filters;

public class CustomExportResult(byte[] data, string fileName, string contentType) : ActionResult, ISpecialResult
{
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = contentType;
        response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
        await response.Body.WriteAsync(data);
    }
}