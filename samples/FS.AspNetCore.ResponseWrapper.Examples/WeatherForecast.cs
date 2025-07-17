using FS.AspNetCore.ResponseWrapper.Models;

namespace FS.AspNetCore.ResponseWrapper.Examples;

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}

public class WeatherForecastResponse : IHasStatusCode
{
    public List<WeatherForecast> Items { get; set; } = [];
    public string? StatusCode { get; set; } = "";
    public string? Message { get; set; } = "";
}