using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ZapChat.Shared.Errors;

/// <summary>
/// Turns exceptions into a single consistent JSON error shape.
///
/// Replaces the six copies of a catch-all handler that returned
/// {status:500,title:"An unexpected error occurred."} for every failure —
/// including validation failures and permission denials.
///
/// A <see cref="ZapChatException"/> becomes its declared status code with its
/// message. Anything else becomes a 500 whose body contains only a trace id;
/// the real exception goes to the log, never to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex,
                "Exception after the response started on {Method} {Path}; cannot write an error body.",
                context.Request.Method, context.Request.Path);
            return;
        }

        var traceId = context.TraceIdentifier;
        ApiError body;
        int status;

        switch (ex)
        {
            case ZapChatException known:
                status = known.StatusCode;
                body = new ApiError(known.ErrorCode, known.Message, traceId)
                {
                    Errors = known is ValidationException v ? v.Errors : null,
                    Category = known is RejectedException r ? r.Category : null
                };

                // Expected outcomes are not errors. Log them at a level that does
                // not pollute the error stream.
                _logger.Log(
                    status >= 500 ? LogLevel.Error : LogLevel.Information,
                    "{Status} {Code} on {Method} {Path}: {Message}",
                    status, known.ErrorCode, context.Request.Method,
                    context.Request.Path, known.Message);
                break;

            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                // The client went away. Nothing to report.
                context.Response.StatusCode = 499;
                return;

            case MongoDB.Driver.MongoCommandException mce when mce.Code == 11000:
                status = 409;
                body = new ApiError("conflict", "That record already exists.", traceId);
                _logger.LogWarning(mce, "Duplicate key on {Path}", context.Request.Path);
                break;

            case TimeoutException:
            case MongoDB.Driver.MongoConnectionException:
            case MongoDB.Bson.BsonException when ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase):
                status = 503;
                body = new ApiError("database_unavailable",
                    "The database is not reachable. Please try again.", traceId);
                _logger.LogError(ex, "Mongo unavailable on {Path}", context.Request.Path);
                break;

            default:
                status = 500;
                // Deliberately no detail: the message could contain a connection
                // string, a file path, or a query.
                body = new ApiError("internal_error",
                    "An unexpected error occurred. Quote the trace id if you report this.", traceId);
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                break;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, Json));
    }
}

/// <summary>The single error shape every ZapChat service returns.</summary>
public sealed class ApiError
{
    public string Code { get; }
    public string Message { get; }
    public string TraceId { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
    public string? Category { get; init; }

    public ApiError(string code, string message, string traceId)
    {
        Code = code;
        Message = message;
        TraceId = traceId;
    }
}
