namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a requested resource cannot be found in the application.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 404 Not Found response with customizable error messaging.
/// </summary>
/// <remarks>
/// Not found exceptions are considered expected application behavior and are not logged as errors
/// by default. They represent normal application flow when users request resources that do not exist
/// or have been removed. The exception message is automatically formatted to include both the
/// resource name and the key that was used in the search attempt, providing clear context
/// about what was not found.
/// 
/// This exception type is commonly used in repository patterns, service layers, and controller
/// actions when entity lookups fail to find the requested data.
/// </remarks>
public class NotFoundException(string name, object key) : ApplicationExceptionBase($"{name} ({key}) was not found.");