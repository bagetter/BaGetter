using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BaGetter.Web;

internal sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;

    public RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        Exception exception = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            sw.Stop();

            var endpoint = context.GetEndpoint()?.DisplayName ?? "unknown";
            var method = context.Request.Method;
            var path = context.Request.Path.ToString();
            var statusCode = context.Response.StatusCode;
            var durationMs = sw.Elapsed.TotalMilliseconds;

            RequestTelemetryMetrics.Requests.Add(1,
                KeyValuePair.Create<string, object>("http.method", method),
                KeyValuePair.Create<string, object>("http.route", endpoint),
                KeyValuePair.Create<string, object>("http.status_code", statusCode),
                KeyValuePair.Create<string, object>("error", exception != null));

            RequestTelemetryMetrics.RequestDurationMs.Record(durationMs,
                KeyValuePair.Create<string, object>("http.method", method),
                KeyValuePair.Create<string, object>("http.route", endpoint),
                KeyValuePair.Create<string, object>("http.status_code", statusCode));

            _logger.LogInformation(
                "HTTP {Method} {Path} => {StatusCode} in {DurationMs}ms (endpoint: {Endpoint}, trace: {TraceId})",
                method,
                path,
                statusCode,
                Math.Round(durationMs, 2),
                endpoint,
                context.TraceIdentifier);
        }
    }
}
