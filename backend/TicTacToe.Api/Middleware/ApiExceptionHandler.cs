using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TicTacToe.Api.Middleware;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", "The resource changed during this request. Refresh and retry."),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled API exception for {Path}", context.Request.Path);
        else
            logger.LogInformation(exception, "API request rejected with {StatusCode}", status);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        }, cancellationToken);
        return true;
    }
}
