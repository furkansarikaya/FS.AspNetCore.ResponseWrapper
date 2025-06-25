namespace FS.AspNetCore.ResponseWrapper.Exceptions;

public class ForbiddenAccessException(string message) : ApplicationExceptionBase(message);