using System.Diagnostics;

namespace Library.Api.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-ID"] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            // Never log request bodies, email addresses, or query strings.
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F2} ms. RequestId: {RequestId}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                timer.Elapsed.TotalMilliseconds, context.TraceIdentifier);
        }
    }
}
