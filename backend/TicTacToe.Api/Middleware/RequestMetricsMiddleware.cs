using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TicTacToe.Api.Middleware;

public sealed class RequestMetricsMiddleware(RequestDelegate next, ILogger<RequestMetricsMiddleware> logger)
{
    private static readonly Meter Meter = new("TicTacToe.Api", "1.0.0");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("http.server.request.duration", "ms");

    public async Task InvokeAsync(HttpContext context)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            timer.Stop();
            RequestDuration.Record(timer.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("method", context.Request.Method),
                new KeyValuePair<string, object?>("status", context.Response.StatusCode));
            logger.LogInformation("HTTP {Method} {Path} returned {StatusCode} in {ElapsedMs:F2} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, timer.Elapsed.TotalMilliseconds);
        }
    }
}
