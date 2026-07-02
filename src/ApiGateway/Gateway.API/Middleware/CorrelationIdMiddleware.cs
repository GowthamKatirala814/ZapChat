namespace Gateway.API.Middleware;

/// <summary>
/// Generates or propagates an X-Correlation-ID header for request tracing.
/// The ID is generated if not present in the incoming request, then added to
/// both the proxied upstream request and the client response.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from request, or generate a new one
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..16]; // 16-char short ID
        }

        // Store in Items for downstream use (e.g., logging middleware)
        context.Items[CorrelationIdHeader] = correlationId.ToString();

        // Forward to upstream services
        context.Request.Headers[CorrelationIdHeader] = correlationId.ToString();

        // Add to response so client can use for support/debugging
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
