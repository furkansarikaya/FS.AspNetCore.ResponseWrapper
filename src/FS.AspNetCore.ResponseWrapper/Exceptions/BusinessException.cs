namespace FS.AspNetCore.ResponseWrapper.Exceptions;

/// <summary>
/// Represents errors that occur when business logic rules are violated during application execution.
/// This exception is automatically handled by the GlobalExceptionHandlingMiddleware and results
/// in a structured HTTP 400 Bad Request response with customizable error messaging.
/// </summary>
/// <remarks>
/// Business exceptions are considered expected application behavior and are not logged as errors
/// by default. They represent violations of domain-specific business rules rather than technical failures.
/// The GlobalExceptionHandlingMiddleware handles these exceptions gracefully and presents them
/// to API consumers as validation-style errors with appropriate HTTP status codes.
/// 
/// This exception type is ideal for scenarios where application logic determines that an operation
/// cannot proceed due to business constraints, such as insufficient inventory, account limits,
/// or workflow state violations.
/// </remarks>
public class BusinessException(string message) : ApplicationExceptionBase(message);