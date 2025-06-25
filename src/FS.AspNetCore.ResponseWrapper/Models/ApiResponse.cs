namespace FS.AspNetCore.ResponseWrapper.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public ResponseMetadata? Metadata { get; set; }
    
    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
    
    public static ApiResponse<T> ErrorResult(string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = [error]
        };
    }
    
    public static ApiResponse<T> ErrorResult(IEnumerable<string> errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors.ToList()
        };
    }
}