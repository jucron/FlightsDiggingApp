using FlightsDiggingApp.Services.Auth;

namespace FlightsDiggingApp.Controllers.Middlewares
{
    public class ApiKeyAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        public ApiKeyAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            var path = context.Request.Path;

            // Skip public endpoints (customize as needed)
            if (path.StartsWithSegments("/api/auth") || path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Read headers
            var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
            var timestampHeader = context.Request.Headers["X-Timestamp"].FirstOrDefault();
            var signature = context.Request.Headers["X-Token"].FirstOrDefault();

            // Validate presence
            if (string.IsNullOrEmpty(clientId) ||
                string.IsNullOrEmpty(timestampHeader) ||
                string.IsNullOrEmpty(signature) ||
                !long.TryParse(timestampHeader, out long timestamp))
            {
                // Missing or invalid authentication headers.
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Validate signature
            if (!authService.Authorize(clientId, timestamp, signature))
            {
                // Invalid signature
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // Proceed to the next middleware
            await _next(context);
        }
    }

}
