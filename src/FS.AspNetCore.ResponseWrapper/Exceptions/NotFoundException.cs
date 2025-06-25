namespace FS.AspNetCore.ResponseWrapper.Exceptions;

public class NotFoundException(string name, object key) : ApplicationExceptionBase($"{name} ({key}) bulunamadı.");