using System.Diagnostics.Metrics;

namespace BaGetter.Web;

internal static class RequestTelemetryMetrics
{
    private static readonly Meter Meter = new("BaGetter.Web", "1.0.0");

    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "bagetter.http.requests",
        unit: "{request}",
        description: "Total HTTP requests.");

    public static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>(
        "bagetter.http.request.duration",
        unit: "ms",
        description: "HTTP request duration.");
}
