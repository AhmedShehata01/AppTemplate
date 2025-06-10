using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLog;

namespace Kindergarten.BLL.Middleware
{
    public class ExceptionMiddleware
    {
        #region Prop
        private readonly RequestDelegate _next;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion


        #region CTOR
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        #endregion

        #region Actions
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
            var errorId = Guid.NewGuid().ToString("N"); // توليد Error ID

            _logger.Error(ex, $"[ErrorId: {errorId}] An unhandled exception occurred."); // تسجيله في الـ log

            context.Response.ContentType = "application/json";

            var statusCode = ex switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode,
                message = statusCode switch
                {
                    400 => "البيانات المرسلة غير صحيحة.",
                    401 => "غير مصرح لك بالوصول.",
                    404 => "العنصر المطلوب غير موجود.",
                    _ => "حدث خطأ غير متوقع. برجاء المحاولة لاحقاً."
                },
                errorId
            });

            await context.Response.WriteAsync(result);
        }


        #endregion

    }
}
