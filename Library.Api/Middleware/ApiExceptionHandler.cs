using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Middleware;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DbUpdateConcurrencyException => (409, "The resource changed. Refresh it and try again."),
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 19 } } =>
                (409, "The change conflicts with existing library data."),
            SqliteException { SqliteErrorCode: 5 or 6 } =>
                (503, "The library is busy. Please retry the request."),
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 } } =>
                (503, "The library is busy. Please retry the request."),
            _ => (500, "An unexpected error occurred.")
        };

        logger.LogError(exception, "Request {RequestId} failed with HTTP {StatusCode}", context.TraceIdentifier, status);
        context.Response.StatusCode = status;
        if (status == 503)
            context.Response.Headers.RetryAfter = "1";

        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
        return true;
    }
}
