using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Storage;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core.Indexing;

public class PackageDeletionService : IPackageDeletionService
{
    private readonly IPackageDatabase _packages;
    private readonly IPackageStorageService _storage;
    private readonly IFeedSettingsResolver _feedSettings;
    private readonly IFeedService _feedService;
    private readonly ILogger<PackageDeletionService> _logger;

    public PackageDeletionService(
        IPackageDatabase packages,
        IPackageStorageService storage,
        IFeedSettingsResolver feedSettings,
        IFeedService feedService,
        ILogger<PackageDeletionService> logger)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _feedSettings = feedSettings ?? throw new ArgumentNullException(nameof(feedSettings));
        _feedService = feedService ?? throw new ArgumentNullException(nameof(feedService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> TryDeletePackageAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var feed = await _feedService.GetFeedByIdAsync(feedId, cancellationToken);
        var behavior = _feedSettings.GetPackageDeletionBehavior(feed);
        switch (behavior)
        {
            case PackageDeletionBehavior.Unlist:
                return await TryUnlistPackageAsync(feedId, id, version, cancellationToken);

            case PackageDeletionBehavior.HardDelete:
                return await TryHardDeletePackageAsync(feedId, feedSlug, id, version, cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown deletion behavior '{behavior}'");
        }
    }

    public async Task<bool> TryUnlistPackageAsync(Guid feedId, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlisting package {PackageId} {PackageVersion}...", id, version);

        if (!await _packages.UnlistPackageAsync(feedId, id, version, cancellationToken))
        {
            _logger.LogWarning("Could not find package {PackageId} {PackageVersion}", id, version);

            return false;
        }

        _logger.LogInformation("Unlisted package {PackageId} {PackageVersion}", id, version);

        return true;
    }

    public async Task<bool> TryRelistPackageAsync(Guid feedId, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Relisting package {PackageId} {PackageVersion}...", id, version);

        if (!await _packages.RelistPackageAsync(feedId, id, version, cancellationToken))
        {
            _logger.LogWarning("Could not find package {PackageId} {PackageVersion}", id, version);

            return false;
        }

        _logger.LogInformation("Relisted package {PackageId} {PackageVersion}", id, version);

        return true;
    }

    public async Task<bool> TryHardDeletePackageAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Hard deleting package {PackageId} {PackageVersion} from the database...",
            id,
            version);

        var found = await _packages.HardDeletePackageAsync(feedId, id, version, cancellationToken);
        if (!found)
        {
            _logger.LogWarning(
                "Could not find package {PackageId} {PackageVersion} in the database",
                id,
                version);
        }

        // Delete the package from storage. This is necessary even if the package isn't
        // in the database to ensure that the storage is consistent with the database.
        _logger.LogInformation("Hard deleting package {PackageId} {PackageVersion} from storage...",
            id,
            version);

        await _storage.DeleteAsync(feedSlug, id, version, cancellationToken);

        _logger.LogInformation(
            "Hard deleted package {PackageId} {PackageVersion} from storage",
            id,
            version);

        return found;
    }

    private static IList<NuGetVersion> GetValidVersions<TS, T>(IEnumerable<NuGetVersion> versions, Func<NuGetVersion, TS> getParent, Func<NuGetVersion, T> getSelector, int versionsToKeep)
            where TS : IComparable<TS>, IEquatable<TS>
            where T : IComparable<T>, IEquatable<T>
    {
        var validVersions = versions
            // for each parent group
            .GroupBy(v => getParent(v))
            // get all versions by selector
            .SelectMany(g => g.Select(k => (parent: g.Key, selector: getSelector(k)))
                .Distinct()
                .OrderByDescending(k => k.selector)
                .Take(versionsToKeep))
            .ToList();
        return versions.Where(k => validVersions.Any(v => getParent(k).Equals(v.parent) && getSelector(k).Equals(v.selector))).ToList();
    }

    public async Task<int> DeleteOldVersionsAsync(Guid feedId, string feedSlug, Package package, uint? maxMajor, uint? maxMinor, uint? maxPatch, uint? maxPrerelease, CancellationToken cancellationToken)
    {
        // list all versions of the package
        var packages = await _packages.FindAsync(feedId, package.Id, includeUnlisted: true, cancellationToken);
        if (packages is null || packages.Count <= maxMajor) return 0;

        var goodVersions = new HashSet<NuGetVersion>();

        if (maxMajor.HasValue)
        {
            goodVersions = GetValidVersions(packages.Select(t => t.Version), v => 0, v => v.Major, (int)maxMajor).ToHashSet();
        }
        else
        {
            goodVersions = packages.Select(p => p.Version).ToHashSet();
        }

        if (maxMinor.HasValue)
        {
            goodVersions.IntersectWith(GetValidVersions(goodVersions, v => (v.Major), v => v.Minor, (int)maxMinor));
        }

        if (maxPatch.HasValue)
        {
            goodVersions.IntersectWith(GetValidVersions(goodVersions, v => (v.Major, v.Minor), v => v.Patch, (int)maxPatch));
        }

        if (maxPrerelease.HasValue)
        {
            var allPreReleaseValidVersions = GetValidPrereleaseVersions(packages.Where(p => goodVersions.Contains(p.Version)).ToList(), maxPrerelease.Value);

            goodVersions.RemoveWhere(v => v.IsPrerelease);
            goodVersions.UnionWith(allPreReleaseValidVersions);
        }

        // sort by version and take everything except the last maxPackages
        var versionsToDelete = packages.Where(p => !goodVersions.Contains(p.Version)).ToList();

        var deleted = 0;
        foreach (var version in versionsToDelete)
        {
            if (await TryHardDeletePackageAsync(feedId, feedSlug, package.Id, version.Version, cancellationToken)) deleted++;
        }
        return deleted;
    }

    /// <summary>
    /// Filters out prereleases that should be removed
    /// </summary>
    /// <param name="packages">All packages versions</param>
    /// <param name="maxPrereleaseVersions">Max numbers of prereleases</param>
    /// <returns>All prereleases that should be kept</returns>
    private HashSet<NuGetVersion> GetValidPrereleaseVersions
        (IReadOnlyList<Package> packages, uint maxPrereleaseVersions)
    {
        var preReleasesParentGroups = packages
            .Where(p => p.Version.IsPrerelease)
            .GroupBy(v => new { v.Version.Major, v.Version.Minor, v.Version.Patch });

        var preReleaseValidVersions = new HashSet<NuGetVersion>();

        foreach (var versions in preReleasesParentGroups)
        {
            var preReleasesSemVersions = versions
                .Select(p => p.Version)
                .Where(preRelease => preRelease.IsSemVer2)
                .ToHashSet();

            if (preReleasesSemVersions.Count > 0)
            {
                // this will give us 'alpha' or 'beta' etc
                var prereleaseTypes = preReleasesSemVersions
                    .Select(v => v.ReleaseLabels?.FirstOrDefault())
                    .Where(lb => lb is not null)
                    .Distinct();

                foreach (var preReleaseType in prereleaseTypes)
                {
                    var preReleaseVersions = preReleasesSemVersions
                        .Where(p =>
                            p.ReleaseLabels!.FirstOrDefault() == preReleaseType
                            && GetPreReleaseBuild(p) is not null).ToList();

                    preReleaseValidVersions.UnionWith(
                        GetValidVersions(preReleaseVersions,
                            v => (v.Major, v.Minor, v.Patch), v => GetPreReleaseBuild(v).Value, (int)maxPrereleaseVersions));
                }
            }

            preReleaseValidVersions.UnionWith(
                versions.Where(pr => !pr.Version.IsSemVer2).OrderByDescending(pr => pr.Published).Take((int)maxPrereleaseVersions).Select(x => x.Version).ToHashSet());
        }

        return preReleaseValidVersions;
    }

    /// <summary>
    /// Tries to get the version number of a pre-release build.<br/>
    /// If we have 1.1.1-alpha.1 , this will return 1 or <c>null</c> if not valid.
    /// </summary>
    /// <returns>The version as <c>int</c> or <c>null</c> if not found.</returns>
    private int? GetPreReleaseBuild(NuGetVersion nuGetVersion)
    {
        if (nuGetVersion.IsPrerelease && nuGetVersion.ReleaseLabels != null)
        {
            // Assuming the last part of the release label is the build number
            var lastLabel = nuGetVersion.ReleaseLabels.LastOrDefault();
            if (int.TryParse(lastLabel, out var buildNumber))
            {
                return buildNumber;
            }
            else
            {
                _logger.LogWarning("Could not parse build number from prerelease label {PrereleaseLabel} - prerelease number is expected to be like 2.3.4-alpha.1 where 1 is prerelease", nuGetVersion);
            }
        }
        return null;
    }
}
