using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AppTemplate.BLL.Services.UserSessionServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppTemplate.BLL.Middleware
{
    public class SingleLoginMiddleware
    {
        private readonly RequestDelegate _next;

        public SingleLoginMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: UserId claim missing.");
                return;
            }

            // حل المشكلة هنا 👇
            var userSessionService = context.RequestServices.GetRequiredService<IUserSessionService>();

            var session = await userSessionService.GetUserSessionAsync(userId);
            if (session == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Session not found.");
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Missing bearer token.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length);
            if (token != session.Token)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Token mismatch.");
                return;
            }

            await _next(context);
        }
    }
}
