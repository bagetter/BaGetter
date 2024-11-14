using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageDeletionService : IPackageDeletionService
{
    private readonly IPackageDatabase _packages;
    private readonly IPackageStorageService _storage;
    private readonly BaGetterOptions _options;
    private readonly ILogger<PackageDeletionService> _logger;

    public PackageDeletionService(
        IPackageDatabase packages,
        IPackageStorageService storage,
        IOptionsSnapshot<BaGetterOptions> options,
        ILogger<PackageDeletionService> logger)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> TryDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        switch (_options.PackageDeletionBehavior)
        {
            case PackageDeletionBehavior.Unlist:
                return await TryUnlistPackageAsync(id, version, cancellationToken);

            case PackageDeletionBehavior.HardDelete:
                return await TryHardDeletePackageAsync(id, version, cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown deletion behavior '{_options.PackageDeletionBehavior}'");
        }
    }

    private async Task<bool> TryUnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlisting package {PackageId} {PackageVersion}...", id, version);

        if (!await _packages.UnlistPackageAsync(id, version, cancellationToken))
        {
            _logger.LogWarning("Could not find package {PackageId} {PackageVersion}", id, version);

            return false;
        }

        _logger.LogInformation("Unlisted package {PackageId} {PackageVersion}", id, version);

        return true;
    }

    private async Task<bool> TryHardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Hard deleting package {PackageId} {PackageVersion} from the database...",
            id,
            version);

        var found = await _packages.HardDeletePackageAsync(id, version, cancellationToken);
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

        await _storage.DeleteAsync(id, version, cancellationToken);

        _logger.LogInformation(
            "Hard deleted package {PackageId} {PackageVersion} from storage",
            id,
            version);

        return found;
    }

    private static IList<NuGetVersion> GetValidVersions<S, T>(IEnumerable<NuGetVersion> versions, Func<NuGetVersion, S> getParent, Func<NuGetVersion,T> getSelector, int versionsToKeep)
            where S : IComparable<S>, IEquatable<S>
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

    public async Task<int> DeleteOldVersionsAsync(Package package, uint? maxMajor, uint? maxMinor, uint? maxPatch, uint? maxPrerelease, CancellationToken cancellationToken)
    {
        // list all versions of the package
        var packages = await _packages.FindAsync(package.Id, includeUnlisted: true, cancellationToken);
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
            // this assume we have something like 1.1.1-alpha.1 - alpha is the release type
            var preReleases = packages.Select(p => p.Version).Where(p => p.IsPrerelease).ToList();
            // this will give us 'alpha' or 'beta' etc
            var prereleaseTypes = preReleases
                .Select(v => v.ReleaseLabels?.FirstOrDefault())
                .Where(lb => lb is not null)
                .Distinct();

            var allPreReleaseValidVersions = new HashSet<NuGetVersion>();
            foreach (var preReleaseType in prereleaseTypes)
            {
                var preReleaseVersions = preReleases.Where(p => p.ReleaseLabels!.FirstOrDefault() == preReleaseType
                        && GetPreReleaseBuild(p) is not null).ToList();

                allPreReleaseValidVersions.UnionWith
                    (GetValidVersions(preReleaseVersions,
                        v => (v.Major, v.Minor, v.Patch), v => GetPreReleaseBuild(v).Value, (int)maxPrerelease));

            }
            goodVersions.IntersectWith(allPreReleaseValidVersions);
        }

        // sort by version and take everything except the last maxPackages
        var versionsToDelete = packages.Where(p => !goodVersions.Contains(p.Version)).ToList();

        var deleted = 0;
        foreach (var version in versionsToDelete)
        {
            if (await TryHardDeletePackageAsync(package.Id, version.Version, cancellationToken)) deleted++;
        }
        return deleted;
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
