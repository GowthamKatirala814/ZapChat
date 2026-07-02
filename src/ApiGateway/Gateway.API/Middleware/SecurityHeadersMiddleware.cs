namespace Gateway.API.Middleware;

/// <summary>
/// Adds industry-standard security headers to every response from the gateway.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent browsers from MIME-sniffing a response away from the declared content-type
            headers["X-Content-Type-Options"] = "nosniff";

            // Deny embedding in iframes — prevents clickjacking
            headers["X-Frame-Options"] = "DENY";

            // Enable cross-site scripting filter in legacy browsers
            headers["X-XSS-Protection"] = "1; mode=block";

            // Control referrer information sent with requests
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict browser features
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // HTTP Strict Transport Security (HTTPS only)
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Content Security Policy — allow resources from same origin
            // In production the API gateway is the only backend entry point.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "connect-src 'self' wss: ws:; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data: blob:; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
