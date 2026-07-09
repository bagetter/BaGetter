using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Indexing;
using BaGetter.Core.Upstream;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageService : IPackageService
{
    private readonly IPackageDatabase _db;
    private readonly IUpstreamClientFactory _upstreamFactory;
    private readonly IFeedService _feedService;
    private readonly IPackageIndexingService _indexer;
    private readonly ILogger<PackageService> _logger;

    public PackageService(
        IPackageDatabase db,
        IUpstreamClientFactory upstreamFactory,
        IFeedService feedService,
        IPackageIndexingService indexer,
        ILogger<PackageService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _upstreamFactory = upstreamFactory ?? throw new ArgumentNullException(nameof(upstreamFactory));
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<NuGetVersion>> FindPackageVersionsAsync(
        Guid feedId,
        string id,
        CancellationToken cancellationToken)
    {
        var feed = await _feedService.GetFeedByIdAsync(feedId, cancellationToken);
        var upstream = _upstreamFactory.CreateForFeed(feed);
        var upstreamVersions = await upstream.ListPackageVersionsAsync(id, cancellationToken);

        // Merge the local package versions into the upstream package versions.
        var localPackages = await _db.FindAsync(feedId, id, includeUnlisted: true, cancellationToken);
        var localVersions = localPackages.Select(p => p.Version);

        if (!upstreamVersions.Any()) return localVersions.ToList();
        if (!localPackages.Any()) return upstreamVersions;

        return upstreamVersions.Concat(localVersions).Distinct().ToList();
    }

    public async Task<IReadOnlyList<Package>> FindPackagesAsync(Guid feedId, string id, CancellationToken cancellationToken)
    {
        var feed = await _feedService.GetFeedByIdAsync(feedId, cancellationToken);
        var upstream = _upstreamFactory.CreateForFeed(feed);
        var upstreamPackages = await upstream.ListPackagesAsync(id, cancellationToken);
        var localPackages = await _db.FindAsync(feedId, id, includeUnlisted: true, cancellationToken);

        if (!upstreamPackages.Any()) return localPackages;
        if (!localPackages.Any()) return upstreamPackages;

        // Merge the local packages into the upstream packages.
        var result = upstreamPackages.ToDictionary(p => p.Version);
        var local = localPackages.ToDictionary(p => p.Version);

        foreach (var localPackage in local)
        {
            result[localPackage.Key] = localPackage.Value;
        }

        return result.Values.ToList();
    }

    public async Task<Package> FindPackageOrNullAsync(
        Guid feedId,
        string feedSlug,
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        if (!await MirrorAsync(feedId, feedSlug, id, version, cancellationToken))
        {
            return null;
        }

        return await _db.FindOrNullAsync(feedId, id, version, includeUnlisted: true, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        return await MirrorAsync(feedId, feedSlug, id, version, cancellationToken);
    }

    public async Task AddDownloadAsync(Guid feedId, string packageId, NuGetVersion version, CancellationToken cancellationToken)
    {
        await _db.AddDownloadAsync(feedId, packageId, version, cancellationToken);
    }

    /// <summary>
    /// Index the package from an upstream if it does not exist locally.
    /// </summary>
    /// <param name="feedId">The feed's id.</param>
    /// <param name="feedSlug">The feed's slug, used to prefix storage paths.</param>
    /// <param name="id">The package ID to index from an upstream.</param>
    /// <param name="version">The package version to index from an upstream.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the package exists locally or was indexed from an upstream source.</returns>
    private async Task<bool> MirrorAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        if (await _db.ExistsAsync(feedId, id, version, cancellationToken))
        {
            return true;
        }

        var feed = await _feedService.GetFeedByIdAsync(feedId, cancellationToken);
        var upstream = _upstreamFactory.CreateForFeed(feed);
        var cacheFeedUrl = upstream.GetServiceIndexUrl();

        _logger.LogInformation(
            "Package {PackageId} {PackageVersion} does not exist locally. Checking upstream feed ({cacheFeedUrl})...",
            id,
            version,
            cacheFeedUrl);

        try
        {
            using var packageStream = await upstream.DownloadPackageOrNullAsync(id, version, cancellationToken);
            if (packageStream == null)
            {
                _logger.LogWarning(
                    "Upstream feed does not have package {PackageId} {PackageVersion}",
                    id,
                    version);
                return false;
            }

            _logger.LogInformation(
                "Downloaded package {PackageId} {PackageVersion}, indexing...",
                id,
                version);

            var result = await _indexer.IndexAsync(feedId, feedSlug, packageStream, cacheFeedUrl, cancellationToken);

            _logger.LogInformation(
                "Finished indexing package {PackageId} {PackageVersion} from upstream feed with result {Result}",
                id,
                version,
                result);

            return result == PackageIndexingResult.Success;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to index package {PackageId} {PackageVersion} from upstream",
                id,
                version);

            return false;
        }
    }
}
