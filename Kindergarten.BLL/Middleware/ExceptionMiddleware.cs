using System.Net;
using System.Text.Json;
using Kindergarten.BLL.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace Kindergarten.BLL.Middleware;
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var errorId = Guid.NewGuid().ToString();

        _logger.LogError(ex, "Unhandled Exception. ErrorId: {ErrorId}, Path: {Path}, Query: {Query}, User: {User}",
            errorId,
            context.Request.Path,
            context.Request.QueryString,
            context.User?.Identity?.Name ?? "Anonymous"
        );



        int statusCode = (int)HttpStatusCode.InternalServerError;
        string message = "حدث خطأ غير متوقع. برجاء المحاولة لاحقاً.";

        // تقدر هنا تمسك أنواع Exceptions معينة لو حابب ترجع StatusCode مختلف
        if (ex is UnauthorizedAccessException)
        {
            statusCode = (int)HttpStatusCode.Unauthorized;
            message = "غير مصرح لك بالوصول.";
        }
        else if (ex is KeyNotFoundException)
        {
            statusCode = (int)HttpStatusCode.NotFound;
            message = "العنصر المطلوب غير موجود.";
        }
        else if (ex is ArgumentException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            message = "البيانات المرسلة غير صحيحة.";
        }
        else
        {
            // لو انت في بيئة Development وعايز تظهر التفاصيل:
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                message = ex.Message;
            }
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var apiResponse = new ApiResponse<string>
        {
            Code = statusCode,
            Status = "Error",
            Result = message,
            ErrorId = errorId
        };

        var json = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
