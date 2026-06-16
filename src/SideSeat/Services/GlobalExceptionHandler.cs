using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SideSeat.Services;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path.Value);

        if (!httpContext.Request.Path.StartsWithSegments("/api") &&
            !httpContext.Request.Path.StartsWithSegments("/mcp"))
        {
            httpContext.Response.Redirect($"/Home/Error?requestId={Uri.EscapeDataString(httpContext.TraceIdentifier)}");
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Dogodila se neočekivana greška.",
                Detail = "Pokušaj ponovno. Ako se problem ponavlja, navedi correlation ID.",
                Extensions = { ["correlationId"] = httpContext.TraceIdentifier }
            },
            Exception = exception
        });
    }
}
