using System.Diagnostics;

namespace SideSeat.Middleware;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isHealthCheck = context.Request.Path.StartsWithSegments("/health");
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 100)
        {
            correlationId = context.TraceIdentifier;
        }

        context.TraceIdentifier = correlationId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var stopwatch = Stopwatch.StartNew();

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["User"] = context.User.Identity?.Name ?? "anonymous"
        }))
        {
            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
                if (!isHealthCheck)
                {
                    logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                        context.Request.Method,
                        context.Request.Path.Value,
                        context.Response.StatusCode,
                        stopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }
    }
}
