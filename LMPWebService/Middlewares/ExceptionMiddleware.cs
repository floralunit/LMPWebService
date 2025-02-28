using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Ошибка базы данных.");
            await HandleExceptionAsync(context, dbEx, "Ошибка базы данных", 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка");
            await HandleExceptionAsync(context, ex, "Внутренняя ошибка сервера", 500);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string message, int statusCode)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            error = message,
            details = exception.Message
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
