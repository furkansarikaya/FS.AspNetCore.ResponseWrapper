namespace FS.AspNetCore.ResponseWrapper.Examples;

public sealed class BusinessRuleValidationException : Exception
{
    public string Msg { get; }
    public string ErrorCode { get; }
    public BusinessRuleValidationException(string msg, string errorCode)
        : base(msg)
    {
        Msg = msg;
        ErrorCode = errorCode;
        
        Data["ExposeMessage"] = true;
        Data["ErrorCode"] = errorCode;
    }
}