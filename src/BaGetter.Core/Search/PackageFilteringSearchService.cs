using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackageFilteringSearchService : IPackageSearchService
{
    private const int MinimumBatchSize = 20;

    private readonly ISearchService _inner;
    private readonly IPackageDatabase _packages;
    private readonly IPackagePolicyEvaluator _policy;
    private readonly ISearchResponseBuilder _responseBuilder;

    public PackageFilteringSearchService(
        ISearchService inner,
        IPackageDatabase packages,
        IPackagePolicyEvaluator policy,
        ISearchResponseBuilder responseBuilder)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(responseBuilder);

        _inner = inner;
        _packages = packages;
        _policy = policy;
        _responseBuilder = responseBuilder;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!_policy.IsFilteringEnabled || request.Skip < 0 || request.Take <= 0)
        {
            return await _inner.SearchAsync(request, cancellationToken);
        }

        var targetCount = GetTargetCount(request.Skip, request.Take);
        var filteredResults = new List<SearchResult>();
        var seenPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceSkip = 0;
        var batchSize = Math.Max(request.Take, MinimumBatchSize);
        SearchContext context = null;

        while (filteredResults.Count < targetCount)
        {
            var response = await _inner.SearchAsync(
                Clone(request, sourceSkip, batchSize),
                cancellationToken);
            context ??= response.Context;
            var results = response.Data ?? Array.Empty<SearchResult>();

            if (results.Count == 0)
            {
                break;
            }

            var discoveredPackage = false;
            foreach (var result in results)
            {
                if (!seenPackageIds.Add(result.PackageId))
                {
                    continue;
                }

                discoveredPackage = true;
                var filteredResult = await FilterSearchResultAsync(result, cancellationToken);
                if (filteredResult != null)
                {
                    filteredResults.Add(filteredResult);
                }

                if (filteredResults.Count == targetCount)
                {
                    break;
                }
            }

            sourceSkip += results.Count;
            if (results.Count < batchSize || !discoveredPackage)
            {
                break;
            }
        }

        var data = filteredResults
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return new SearchResponse
        {
            Context = context ?? _responseBuilder.BuildSearch(new List<PackageRegistration>()).Context,
            TotalHits = filteredResults.Count,
            Data = data
        };
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken cancellationToken)
    {
        if (!_policy.IsFilteringEnabled || request.Skip < 0 || request.Take <= 0)
        {
            return await _inner.AutocompleteAsync(request, cancellationToken);
        }

        var targetCount = GetTargetCount(request.Skip, request.Take);
        var allowedPackageIds = new List<string>();
        var seenPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceSkip = 0;
        var batchSize = Math.Max(request.Take, MinimumBatchSize);

        while (allowedPackageIds.Count < targetCount)
        {
            var response = await _inner.AutocompleteAsync(
                Clone(request, sourceSkip, batchSize),
                cancellationToken);
            var packageIds = response.Data ?? Array.Empty<string>();

            if (packageIds.Count == 0)
            {
                break;
            }

            var discoveredPackage = false;
            foreach (var packageId in packageIds)
            {
                if (!seenPackageIds.Add(packageId))
                {
                    continue;
                }

                discoveredPackage = true;
                var packages = await _packages.FindAsync(
                    packageId,
                    includeUnlisted: false,
                    cancellationToken);

                if ((packages.Count == 0 && IsAllowed(packageId, version: null, PackageFilterScope.Upstream)) ||
                    packages.Any(IsAllowed))
                {
                    allowedPackageIds.Add(packageId);
                }

                if (allowedPackageIds.Count == targetCount)
                {
                    break;
                }
            }

            sourceSkip += packageIds.Count;
            if (packageIds.Count < batchSize || !discoveredPackage)
            {
                break;
            }
        }

        return _responseBuilder.BuildAutocomplete(
            allowedPackageIds.Skip(request.Skip).Take(request.Take).ToList());
    }

    public async Task<AutocompleteResponse> ListPackageVersionsAsync(
        VersionsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _inner.ListPackageVersionsAsync(request, cancellationToken);
        if (!_policy.IsFilteringEnabled)
        {
            return response;
        }

        var packages = await _packages.FindAsync(
            request.PackageId,
            includeUnlisted: false,
            cancellationToken);
        var packagesByVersion = packages
            .GroupBy(package => package.Version, VersionComparer.VersionReleaseMetadata)
            .ToDictionary(group => group.Key, group => group.First(), VersionComparer.VersionReleaseMetadata);

        var versions = (response.Data ?? Array.Empty<string>())
            .Where(version =>
            {
                if (!NuGetVersion.TryParse(version, out var parsedVersion))
                {
                    return false;
                }

                return packagesByVersion.TryGetValue(parsedVersion, out var package)
                    ? IsAllowed(package)
                    : IsAllowed(request.PackageId, parsedVersion, PackageFilterScope.Upstream);
            })
            .ToList();

        return _responseBuilder.BuildAutocomplete(versions);
    }

    public Task<DependentsResponse> FindDependentsAsync(
        string packageId,
        CancellationToken cancellationToken)
        => _inner.FindDependentsAsync(packageId, cancellationToken);

    private async Task<SearchResult> FilterSearchResultAsync(
        SearchResult result,
        CancellationToken cancellationToken)
    {
        var resultVersions = GetSearchResultVersions(result);
        if (resultVersions.Count == 0)
        {
            return null;
        }

        var packages = await _packages.FindAsync(
            result.PackageId,
            includeUnlisted: false,
            cancellationToken);
        var packagesByVersion = packages
            .GroupBy(package => package.Version, VersionComparer.VersionReleaseMetadata)
            .ToDictionary(group => group.Key, group => group.First(), VersionComparer.VersionReleaseMetadata);

        var allowedVersions = resultVersions
            .Where(version => NuGetVersion.TryParse(version.Version, out _))
            .Where(version =>
            {
                var parsedVersion = NuGetVersion.Parse(version.Version);
                return packagesByVersion.TryGetValue(parsedVersion, out var package)
                    ? IsAllowed(package)
                    : IsAllowed(result.PackageId, parsedVersion, PackageFilterScope.Upstream);
            })
            .OrderByDescending(version => NuGetVersion.Parse(version.Version))
            .ToList();

        return allowedVersions.Count == 0
            ? null
            : Clone(result, allowedVersions);
    }

    private bool IsAllowed(Package package)
    {
        var scope = string.IsNullOrEmpty(package.CachedFrom)
            ? PackageFilterScope.Local
            : PackageFilterScope.CachedUpstream;

        return !_policy.Evaluate(
            new PackageFilterContext(package.Id, package.Version, scope)).IsBlocked;
    }

    private bool IsAllowed(string packageId, NuGetVersion version, PackageFilterScope scope)
    {
        return !_policy.Evaluate(
            new PackageFilterContext(packageId, version, scope)).IsBlocked;
    }

    private static IReadOnlyList<SearchResultVersion> GetSearchResultVersions(SearchResult result)
    {
        if (result.Versions != null && result.Versions.Count > 0)
        {
            return result.Versions;
        }

        return string.IsNullOrEmpty(result.Version)
            ? Array.Empty<SearchResultVersion>()
            : new[]
            {
                new SearchResultVersion
                {
                    RegistrationLeafUrl = result.RegistrationIndexUrl,
                    Version = result.Version,
                    Downloads = result.TotalDownloads,
                }
            };
    }

    private static SearchResult Clone(SearchResult result, List<SearchResultVersion> versions)
    {
        var totalDownloads = versions.Any(version => version.Downloads > 0)
            ? versions.Sum(version => version.Downloads)
            : result.TotalDownloads;

        return new SearchResult
        {
            PackageId = result.PackageId,
            Version = versions[0].Version,
            Description = result.Description,
            Authors = result.Authors,
            IconUrl = result.IconUrl,
            LicenseUrl = result.LicenseUrl,
            PackageTypes = result.PackageTypes,
            ProjectUrl = result.ProjectUrl,
            RegistrationIndexUrl = result.RegistrationIndexUrl,
            Summary = result.Summary,
            Tags = result.Tags,
            Title = result.Title,
            TotalDownloads = totalDownloads,
            Versions = versions,
        };
    }

    private static int GetTargetCount(int skip, int take)
        => (int)Math.Min(int.MaxValue, (long)skip + take);

    private static SearchRequest Clone(SearchRequest request, int skip, int take)
        => new()
        {
            Skip = skip,
            Take = take,
            IncludePrerelease = request.IncludePrerelease,
            IncludeSemVer2 = request.IncludeSemVer2,
            PackageType = request.PackageType,
            Framework = request.Framework,
            Query = request.Query
        };

    private static AutocompleteRequest Clone(AutocompleteRequest request, int skip, int take)
        => new()
        {
            Skip = skip,
            Take = take,
            IncludePrerelease = request.IncludePrerelease,
            IncludeSemVer2 = request.IncludeSemVer2,
            PackageType = request.PackageType,
            Query = request.Query
        };
}
