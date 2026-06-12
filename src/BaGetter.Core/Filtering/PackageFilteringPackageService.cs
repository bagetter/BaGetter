using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageFilteringPackageService : IPackageService
{
    private readonly IPackageService _inner;
    private readonly IPackageDatabase _packages;
    private readonly IPackagePolicyEvaluator _policy;

    public PackageFilteringPackageService(
        IPackageService inner,
        IPackageDatabase packages,
        IPackagePolicyEvaluator policy)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public async Task<IReadOnlyList<NuGetVersion>> FindPackageVersionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var versions = await _inner.FindPackageVersionsAsync(id, cancellationToken);
        if (!_policy.IsFilteringEnabled)
        {
            return versions;
        }

        var localPackages = await GetLocalPackagesByVersionAsync(id, cancellationToken);

        return versions
            .Where(version => IsAllowed(id, version, localPackages))
            .ToList();
    }

    public async Task<IReadOnlyList<Package>> FindPackagesAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var packages = await _inner.FindPackagesAsync(id, cancellationToken);
        if (!_policy.IsFilteringEnabled)
        {
            return packages;
        }

        var localPackages = await GetLocalPackagesByVersionAsync(id, cancellationToken);

        return packages
            .Where(package => IsAllowed(package.Id ?? id, package.Version, localPackages))
            .ToList();
    }

    public async Task<Package> FindPackageOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        if (!_policy.IsFilteringEnabled)
        {
            return await _inner.FindPackageOrNullAsync(id, version, cancellationToken);
        }

        if (!await IsAllowedBeforeMirrorAsync(id, version, cancellationToken))
        {
            return null;
        }

        return await _inner.FindPackageOrNullAsync(id, version, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        if (!_policy.IsFilteringEnabled)
        {
            return await _inner.ExistsAsync(id, version, cancellationToken);
        }

        if (!await IsAllowedBeforeMirrorAsync(id, version, cancellationToken))
        {
            return false;
        }

        return await _inner.ExistsAsync(id, version, cancellationToken);
    }

    public Task AddDownloadAsync(
        string packageId,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        return _inner.AddDownloadAsync(packageId, version, cancellationToken);
    }

    private async Task<Dictionary<SemanticVersion, Package>> GetLocalPackagesByVersionAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var packages = await _packages.FindAsync(id, includeUnlisted: true, cancellationToken);

        return packages
            .GroupBy(package => package.Version, VersionComparer.VersionReleaseMetadata)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                VersionComparer.VersionReleaseMetadata);
    }

    private async Task<bool> IsAllowedBeforeMirrorAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        var localPackage = await _packages.FindOrNullAsync(
            id,
            version,
            includeUnlisted: true,
            cancellationToken);

        if (localPackage != null)
        {
            return IsAllowed(localPackage);
        }

        return IsAllowed(id, version, PackageFilterScope.Upstream);
    }

    private bool IsAllowed(
        string id,
        NuGetVersion version,
        Dictionary<SemanticVersion, Package> localPackages)
    {
        return localPackages.TryGetValue(version, out var package)
            ? IsAllowed(package)
            : IsAllowed(id, version, PackageFilterScope.Upstream);
    }

    private bool IsAllowed(Package package)
    {
        var scope = string.IsNullOrEmpty(package.CachedFrom)
            ? PackageFilterScope.Local
            : PackageFilterScope.CachedUpstream;

        return IsAllowed(package.Id, package.Version, scope);
    }

    private bool IsAllowed(string id, NuGetVersion version, PackageFilterScope scope)
    {
        return !_policy.Evaluate(new PackageFilterContext(id, version, scope)).IsBlocked;
    }
}
