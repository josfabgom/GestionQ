using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using GestionQ.Licensing;

namespace GestionQ.Web.Middleware
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // Allow access to static files and the license activation page
            if (path != null && (
                path.StartsWith("/lib/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/license") ||
                path.StartsWith("/api/auth/login"))) 
            {
                await _next(context);
                return;
            }

            string licenseKey = configuration["LicenseKey"] ?? string.Empty;
            bool isValid = LicenseValidator.IsLicenseValid(licenseKey, out string errorMessage);

            if (!isValid)
            {
                if (path != null && path.StartsWith("/api/"))
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync($"{{\"error\": \"LICENSE_REQUIRED\", \"message\": \"{errorMessage}\"}}");
                    return;
                }
                
                context.Response.Redirect("/License");
                return;
            }

            await _next(context);
        }
    }
}
