using System.Diagnostics;

namespace ESDEMO.Api.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly EventId RequestCompletedEvent = new(1000, "RequestCompleted");

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? unhandledException = null;

        try
        {
            await next(httpContext);
        }
        catch (Exception exception)
        {
            unhandledException = exception;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = unhandledException is null
                ? httpContext.Response.StatusCode
                : StatusCodes.Status500InternalServerError;
            var logLevel = GetLogLevel(httpContext.Request.Path, statusCode, unhandledException);
            var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

            logger.Log(
                logLevel,
                RequestCompletedEvent,
                unhandledException,
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms (TraceId: {TraceId})",
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? "/",
                statusCode,
                stopwatch.ElapsedMilliseconds,
                traceId);
        }
    }

    private static LogLevel GetLogLevel(
        PathString requestPath,
        int statusCode,
        Exception? unhandledException)
    {
        if (requestPath.StartsWithSegments("/health") && statusCode < StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Debug;
        }

        if (unhandledException is not null || statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        return statusCode >= StatusCodes.Status400BadRequest
            ? LogLevel.Warning
            : LogLevel.Information;
    }
}
