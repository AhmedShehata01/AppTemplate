using System.Net;
using System.Text.Json;
using Kindergarten.BLL.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kindergarten.BLL.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Entry point for the middleware in the pipeline.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // لو مفيش استثناء، كمل تنفيذ باقي البايبلاين عادي
                await _next(context);
            }
            catch (Exception ex)
            {
                // لو حصل Exception، تعامل معاه هنا
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles exceptions globally and returns a standardized JSON response.
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var errorId = Guid.NewGuid().ToString();

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message = "حدث خطأ غير متوقع. برجاء المحاولة لاحقاً.";

            // تحديد نوع الخطأ (Business vs System)
            LogLevel logLevel;

            if (ex is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = IsDevelopment() ? ex.Message : "غير مصرح لك بالوصول.";
                logLevel = LogLevel.Information; // Business Error → Info
            }
            else if (ex is KeyNotFoundException)
            {
                statusCode = (int)HttpStatusCode.NotFound;
                message = IsDevelopment() ? ex.Message : "العنصر المطلوب غير موجود.";
                logLevel = LogLevel.Information; // Business Error → Info
            }
            else if (ex is ArgumentException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = IsDevelopment() ? ex.Message : "البيانات المرسلة غير صحيحة.";
                logLevel = LogLevel.Information; // Business Error → Info
            }
            else
            {
                // System Error → Error
                if (IsDevelopment())
                {
                    message = ex.Message;
                }
                logLevel = LogLevel.Error;
            }

            // كتابة الـ Log بالمستوى المناسب
            _logger.Log(
                logLevel,
                ex,
                "Unhandled Exception. ErrorId: {ErrorId}, Path: {Path}, Query: {Query}, User: {User}",
                errorId,
                context.Request.Path,
                context.Request.QueryString,
                context.User?.Identity?.Name ?? "Anonymous"
            );

            // تجهيز الـ Response الموحد
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

        /// <summary>
        /// Checks if the current environment is Development.
        /// </summary>
        private bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }
    }
}
