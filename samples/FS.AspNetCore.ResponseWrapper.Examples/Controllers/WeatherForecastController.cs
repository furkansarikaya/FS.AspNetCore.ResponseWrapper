using FS.AspNetCore.ResponseWrapper.Exceptions;
using Microsoft.AspNetCore.Mvc;
using TimeoutException = System.TimeoutException;

namespace FS.AspNetCore.ResponseWrapper.Examples.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger = logger;

    [HttpGet(Name = "GetWeatherForecast")]
    public IActionResult Get(int i)
    {
        switch (i)
        {
            case 1:
                throw new Exception("Test exception");
            case 2:
                throw new ApplicationException("Test ApplicationException");
            case 3:
                throw new UnauthorizedAccessException("Test UnauthorizedAccessException");
            case 4:
                throw new TimeoutException("Test TimeoutException");
            case 5:
                throw new NotSupportedException("Test NotSupportedException");
            case 6:
                throw new ArgumentException("Test ArgumentException");
            case 7:
                throw new InvalidOperationException("Test InvalidOperationException");
            case 8:
                throw new NullReferenceException("Test NullReferenceException");
            case 9:
                throw new KeyNotFoundException("Test KeyNotFoundException");
            case 10:
                throw new IndexOutOfRangeException("Test IndexOutOfRangeException");
            case 11:
                throw new FormatException("Test FormatException");
            case 12:
                throw new OverflowException("Test OverflowException");
            case 13:
                throw new DivideByZeroException("Test DivideByZeroException");
            case 14:
                throw new StackOverflowException("Test StackOverflowException");
            case 15:
                throw new OutOfMemoryException("Test OutOfMemoryException");
            case 16:
                throw new InvalidOperationException("Test InvalidOperationException");
            case 17:
                throw new BusinessException("Test BusinessException", "Business");
            case 18:
                throw new NotFoundException("Test NotFoundException", "NotFoundException");
            case 19:
                throw new NotFoundException(nameof(Get), 19);
            case 20:
                throw new BusinessRuleValidationException("Test BusinessRuleValidationException", "BusinessRule");
            default:
            {
                var items = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    })
                    .ToList();
                WeatherForecastResponse response = new()
                {
                    Items = items,
                    StatusCode = "Success",
                    Message = "Success message",
                    Metadata = new Dictionary<string, object>
                    {
                        { "verificationType", "2fa" },
                        { "canResend", true },
                        { "resendCooldown", 30 },
                        { "attemptsRemaining", 3 },
                        { "expiresIn", 300 }
                    }
                };
                return Ok(response);
            }
        }
    }
}