using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BaGetter.Core;

namespace BaGetter.Web.Admin;

public sealed class FileAdminAuditLog : IAdminAuditLog
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path;
    private readonly ILogger<FileAdminAuditLog> _logger;

    public FileAdminAuditLog(
        IWebHostEnvironment environment,
        IOptions<BaGetterOptions> options,
        ILogger<FileAdminAuditLog> logger)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var configuredPath = options.Value.Admin?.AuditLogPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Admin.AuditLogPath must be configured.");
        }

        _path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public async Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await Gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var line = JsonSerializer.Serialize(entry, JsonOptions);
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            Gate.Release();
        }

        _logger.LogInformation(
            "ADMIN_AUDIT actor={Actor} action={Action} package={PackageId} version={PackageVersion} result={Result} ip={IpAddress}",
            entry.Actor,
            entry.Action,
            entry.PackageId,
            entry.PackageVersion,
            entry.Result,
            entry.IpAddress);
    }

    public async Task<IReadOnlyList<AdminAuditEntry>> ReadLatestAsync(int count, CancellationToken cancellationToken)
    {
        if (count <= 0 || !File.Exists(_path))
        {
            return [];
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            var latest = new Queue<AdminAuditEntry>(count);
            await using var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: true);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                var entry = ParseEntry(line);
                if (entry == null)
                {
                    continue;
                }

                if (latest.Count == count)
                {
                    latest.Dequeue();
                }

                latest.Enqueue(entry);
            }

            return latest.Reverse().ToList();
        }
        finally
        {
            Gate.Release();
        }
    }

    private AdminAuditEntry ParseEntry(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<AdminAuditEntry>(line, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Ignoring malformed admin audit log entry");
            return null;
        }
    }
}
