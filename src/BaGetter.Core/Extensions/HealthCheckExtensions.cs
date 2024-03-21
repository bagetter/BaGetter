using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BaGetter.Core.Extensions;

public static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Formats the <see cref="HealthReport"/> as JSON and writes it to the specified <see cref="Stream"/>.
    /// </summary>
    /// <param name="report">The report to format.</param>
    /// <param name="stream">A writable stream to write the report to. Will not be closed.</param>
    /// <param name="detailedReport">Whether to include detailed information about each health check.</param>
    /// <param name="statusPropertyName">The name of the property that will contain the overall status.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="Task"/> completing when the report is completely written to the stream.</returns>
    public static async Task FormatAsJson(this HealthReport report, Stream stream, bool detailedReport, string statusPropertyName = "Status",
        CancellationToken cancellationToken = default)
    {
        // Always include the overall status.
        IEnumerable<(string Key, HealthStatus Value)> entries = [(statusPropertyName, report.Status)];

        // Include details if requested.
        if (detailedReport)
        {
            entries = entries.Concat(report.Entries.Select(entry => (entry.Key, entry.Value.Status)));
        }

        await JsonSerializer.SerializeAsync(
            stream,
            entries.ToDictionary(entry => entry.Key, entry => entry.Value),
            SerializerOptions,
            cancellationToken);
    }

    /// <summary>
    /// Determine whether a health check is configured for BaGetter.
    /// </summary>
    /// <param name="check">The <see cref="HealthCheckRegistration"/>.</param>
    /// <param name="options">The current BaGetter configuration. Will be checked for configured services.</param>
    /// <returns>A boolean representing whether the given check is configured in this BaGetter instance.</returns>
    public static bool IsConfigured(this HealthCheckRegistration check, BaGetterOptions options)
    {
        return check.Tags.Count == 0 || // General checks
               check.Tags.Contains(options.Database.Type) || // Database check
               check.Tags.Contains(options.Storage.Type) || // Storage check
               check.Tags.Contains(options.Search.Type); // Search check
    }
}
