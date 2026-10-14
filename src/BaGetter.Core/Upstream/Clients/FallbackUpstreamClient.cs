using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// Aggregates multiple upstream clients. Version/metadata listings are UNIONED across
/// every upstream (so a package present on several feeds contributes all its versions
/// and no single feed can shadow another — e.g. nuget.org's stale Hangfire.Throttling
/// 1.0.0-beta1 must not hide the licensed feed's 1.4.3). Downloads use the first
/// upstream that has the requested package.
/// </summary>
public sealed class FallbackUpstreamClient : IUpstreamClient, IDisposable
{
    private readonly IReadOnlyList<IUpstreamClient> _clients;
    private readonly ILogger<FallbackUpstreamClient> _logger;

    public FallbackUpstreamClient(
        IReadOnlyList<IUpstreamClient> clients,
        ILogger<FallbackUpstreamClient> logger)
    {
        if (clients == null || clients.Count == 0)
        {
            throw new ArgumentException("At least one upstream client is required.", nameof(clients));
        }

        _clients = clients;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<NuGetVersion>> ListPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        var versions = new HashSet<NuGetVersion>();
        foreach (var client in _clients)
        {
            try
            {
                foreach (var version in await client.ListPackageVersionsAsync(id, cancellationToken))
                {
                    versions.Add(version);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Mirror failed while listing versions for {PackageId}", id);
            }
        }

        return versions.ToList();
    }

    public async Task<IReadOnlyList<Package>> ListPackagesAsync(string id, CancellationToken cancellationToken)
    {
        // Union metadata across all upstreams, deduped by version. Earlier (higher
        // priority) upstreams win for a version present on more than one feed.
        var byVersion = new Dictionary<NuGetVersion, Package>();
        foreach (var client in _clients)
        {
            try
            {
                foreach (var package in await client.ListPackagesAsync(id, cancellationToken))
                {
                    if (!byVersion.ContainsKey(package.Version))
                    {
                        byVersion[package.Version] = package;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Mirror failed while listing metadata for {PackageId}", id);
            }
        }

        return byVersion.Values.ToList();
    }

    public async Task<Stream> DownloadPackageOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        foreach (var client in _clients)
        {
            try
            {
                var stream = await client.DownloadPackageOrNullAsync(id, version, cancellationToken);
                if (stream != null)
                {
                    return stream;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Mirror failed while downloading {PackageId} {PackageVersion}", id, version);
            }
        }

        return null;
    }

    public void Dispose()
    {
        foreach (var disposable in _clients.OfType<IDisposable>())
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
