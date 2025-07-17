using FS.AspNetCore.ResponseWrapper;
using FS.AspNetCore.ResponseWrapper.Middlewares;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddResponseWrapper(options =>
{
    options.DateTimeProvider = () => DateTime.Now;
    options.EnableQueryStatistics = builder.Environment.EnvironmentName != "Production";

    // Exclude specific paths from wrapping
    options.ExcludedPaths = ["/health", "/metrics", "/swagger"];

    // Exclude specific result types from wrapping
    options.ExcludedTypes = [typeof(FileResult), typeof(RedirectResult)];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();