using System.Diagnostics;

namespace Gateway.API.Middleware;

/// <summary>
/// Lightweight structured request/response logging middleware.
/// Logs: method, path, status code, latency, and correlation ID.
/// Uses the built-in ILogger (no extra logging frameworks needed).
/// </summary>
public class RequestLoggingMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health check endpoint to avoid noise
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Items.TryGetValue(CorrelationIdHeader, out var cid)
            ? cid?.ToString() ?? "-"
            : "-";

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var latencyMs = sw.ElapsedMilliseconds;

            // Choose log level based on status code
            if (statusCode >= 500)
            {
                _logger.LogError(
                    "[Gateway] {Method} {Path} → {StatusCode} ({LatencyMs}ms) [CorrId:{CorrelationId}]",
                    method, path, statusCode, latencyMs, correlationId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "[Gateway] {Method} {Path} → {StatusCode} ({LatencyMs}ms) [CorrId:{CorrelationId}]",
                    method, path, statusCode, latencyMs, correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "[Gateway] {Method} {Path} → {StatusCode} ({LatencyMs}ms) [CorrId:{CorrelationId}]",
                    method, path, statusCode, latencyMs, correlationId);
            }
        }
    }
}
