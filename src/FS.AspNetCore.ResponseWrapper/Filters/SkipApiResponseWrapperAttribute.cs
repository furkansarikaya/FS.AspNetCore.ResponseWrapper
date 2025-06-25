namespace FS.AspNetCore.ResponseWrapper.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipApiResponseWrapperAttribute : Attribute
{
    public string Reason { get; set; } = "";
    
    public SkipApiResponseWrapperAttribute(string reason = "")
    {
        Reason = reason;
    }
}