using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageService : IPackageService
{
    private readonly IPackageDatabase _db;
    private readonly IUpstreamClient _upstream;
    private readonly IPackageIndexingService _indexer;
    private readonly ILogger<PackageService> _logger;

    public PackageService(
        IPackageDatabase db,
        IUpstreamClient upstream,
        IPackageIndexingService indexer,
        ILogger<PackageService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _upstream = upstream ?? throw new ArgumentNullException(nameof(upstream));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<NuGetVersion>> FindPackageVersionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var upstreamVersions = await _upstream.ListPackageVersionsAsync(id, cancellationToken);

        // Merge the local package versions into the upstream package versions.
        var localPackages = await _db.FindAsync(id, includeUnlisted: true, cancellationToken);
        var localVersions = localPackages.Select(p => p.Version);

        if (!upstreamVersions.Any()) return localVersions.ToList();
        if (!localPackages.Any()) return upstreamVersions;

        return upstreamVersions.Concat(localVersions).Distinct().ToList();
    }

    public async Task<IReadOnlyList<Package>> FindPackagesAsync(string id, CancellationToken cancellationToken)
    {
        var upstreamPackages = await _upstream.ListPackagesAsync(id, cancellationToken);
        var localPackages = await _db.FindAsync(id, includeUnlisted: true, cancellationToken);

        if (!upstreamPackages.Any()) return localPackages;
        if (!localPackages.Any()) return upstreamPackages;

        // Merge the local packages into the upstream packages.
        var result = upstreamPackages.ToDictionary(p => p.Version);
        var local = localPackages.ToDictionary(p => p.Version);

        foreach (var localPackage in local)
        {
            if (result.TryGetValue(localPackage.Key, out var upstreamPackage))
            {
                var mergedPackage = ClonePackage(localPackage.Value);
                MergeUpstreamMetadata(mergedPackage, upstreamPackage);
                result[localPackage.Key] = mergedPackage;
                continue;
            }

            result[localPackage.Key] = localPackage.Value;
        }

        return result.Values.ToList();
    }

    public async Task<Package> FindPackageOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        if (!await MirrorAsync(id, version, cancellationToken))
        {
            return null;
        }

        return await _db.FindOrNullAsync(id, version, includeUnlisted: true, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        return await MirrorAsync(id, version, cancellationToken);
    }

    public async Task AddDownloadAsync(string packageId, NuGetVersion version, CancellationToken cancellationToken)
    {
        await _db.AddDownloadAsync(packageId, version, cancellationToken);
    }

    /// <summary>
    /// Index the package from an upstream if it does not exist locally.
    /// </summary>
    /// <param name="id">The package ID to index from an upstream.</param>
    /// <param name="version">The package version to index from an upstream.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the package exists locally or was indexed from an upstream source.</returns>
    private async Task<bool> MirrorAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        if (await _db.ExistsAsync(id, version, cancellationToken))
        {
            return true;
        }

        var cacheFeedUrl = _upstream.GetServiceIndexUrl();

        _logger.LogInformation(
            "Package {PackageId} {PackageVersion} does not exist locally. Checking upstream feed ({cacheFeedUrl})...",
            id,
            version,
            cacheFeedUrl);

        try
        {
            using var packageStream = await _upstream.DownloadPackageOrNullAsync(id, version, cancellationToken);
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

            var result = await _indexer.IndexAsync(packageStream, cacheFeedUrl, cancellationToken);

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

    private static void MergeUpstreamMetadata(Package local, Package upstream)
    {
        local.Downloads += upstream.Downloads;
        local.HasReadme = local.HasReadme || upstream.HasReadme;

        // Cached upstream packages are indexed at cache time. Use upstream
        // visibility metadata so package pages do not show the cache timestamp.
        if (ShouldUseUpstreamVisibilityMetadata(local, upstream))
        {
            local.Published = upstream.Published;
            local.Listed = upstream.Listed;
        }

        local.IconUrl ??= upstream.IconUrl;
        local.LicenseUrl ??= upstream.LicenseUrl;
        local.ProjectUrl ??= upstream.ProjectUrl;
        local.RepositoryUrl ??= upstream.RepositoryUrl;

        if (string.IsNullOrEmpty(local.RepositoryType))
        {
            local.RepositoryType = upstream.RepositoryType;
        }

        if (string.IsNullOrEmpty(local.Language))
        {
            local.Language = upstream.Language;
        }

        if (string.IsNullOrEmpty(local.MinClientVersion))
        {
            local.MinClientVersion = upstream.MinClientVersion;
        }

        if (string.IsNullOrEmpty(local.Summary))
        {
            local.Summary = upstream.Summary;
        }

        if (string.IsNullOrEmpty(local.Title))
        {
            local.Title = upstream.Title;
        }

        if (string.IsNullOrEmpty(local.Description))
        {
            local.Description = upstream.Description;
        }

        if ((local.Tags?.Length ?? 0) == 0)
        {
            local.Tags = upstream.Tags;
        }

        if ((local.Authors?.Length ?? 0) == 0)
        {
            local.Authors = upstream.Authors;
        }
    }

    private static bool ShouldUseUpstreamVisibilityMetadata(Package local, Package upstream)
    {
        if (!string.IsNullOrEmpty(local.CachedFrom))
        {
            return true;
        }

        // Packages cached before cache provenance was persisted have no
        // CachedFrom value, but their local publish date is the cache time.
        return upstream.Published != default && local.Published > upstream.Published;
    }

    private static Package ClonePackage(Package package)
    {
        return new Package
        {
            Key = package.Key,
            Id = package.Id,
            Authors = package.Authors?.ToArray(),
            Description = package.Description,
            Downloads = package.Downloads,
            HasReadme = package.HasReadme,
            HasEmbeddedIcon = package.HasEmbeddedIcon,
            IsPrerelease = package.IsPrerelease,
            CachedFrom = package.CachedFrom,
            ReleaseNotes = package.ReleaseNotes,
            Language = package.Language,
            Listed = package.Listed,
            MinClientVersion = package.MinClientVersion,
            Published = package.Published,
            RequireLicenseAcceptance = package.RequireLicenseAcceptance,
            SemVerLevel = package.SemVerLevel,
            Summary = package.Summary,
            Title = package.Title,
            IconUrl = package.IconUrl,
            LicenseUrl = package.LicenseUrl,
            ProjectUrl = package.ProjectUrl,
            RepositoryUrl = package.RepositoryUrl,
            RepositoryType = package.RepositoryType,
            Tags = package.Tags?.ToArray(),
            RowVersion = package.RowVersion?.ToArray(),
            Dependencies = package.Dependencies?.ToList(),
            PackageTypes = package.PackageTypes?.ToList(),
            TargetFrameworks = package.TargetFrameworks?.ToList(),
            NormalizedVersionString = package.NormalizedVersionString,
            OriginalVersionString = package.OriginalVersionString,
        };
    }
}
