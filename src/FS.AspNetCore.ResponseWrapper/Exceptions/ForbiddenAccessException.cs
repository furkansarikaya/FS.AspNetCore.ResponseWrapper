namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when a user attempts to access a resource for which they lack sufficient permissions.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 403 Forbidden response with customizable error messaging.
/// </summary>
/// <remarks>
/// Forbidden access exceptions indicate that the user's identity has been successfully authenticated
/// but they lack the necessary permissions to access the requested resource. These exceptions are
/// considered expected application behavior and are not logged as errors by default, as they
/// represent normal security enforcement rather than technical failures.
/// 
/// This exception type should be used when authorization checks determine that an authenticated
/// user does not have the required roles, permissions, or access rights to perform the requested operation.
/// </remarks>
public class ForbiddenAccessException(string message) : ApplicationExceptionBase(message);