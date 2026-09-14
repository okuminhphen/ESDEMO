using ESDEMO.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ESDEMO.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            RequestValidationException => StatusCodes.Status400BadRequest,
            AuthenticationFailedException => StatusCodes.Status401Unauthorized,
            ConflictException => StatusCodes.Status409Conflict,
            BadHttpRequestException badRequest => badRequest.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }

        ProblemDetails problemDetails = exception is RequestValidationException validation
            ? new ValidationProblemDetails(validation.Errors)
            : new ProblemDetails();
        problemDetails.Status = status;
        problemDetails.Title = exception switch
        {
            BadHttpRequestException => "Invalid HTTP request.",
            _ when status == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => exception.Message
        };
        // Auth errors never expose provider details or submitted credentials, even in Development.
        problemDetails.Detail = status == 500 && environment.IsDevelopment()
            && !httpContext.Request.Path.StartsWithSegments("/api/auth") ? exception.Message : null;
        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problemDetails.Status.Value;

        httpContext.Response.Headers.CacheControl = "no-store";
        await httpContext.Response.WriteAsJsonAsync<object>(problemDetails, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
