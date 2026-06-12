using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// Combines the configured local search provider with optional upstream search.
/// Full proxy mode also uses this layer to rewrite package-resource URLs before
/// they are returned to clients.
/// </summary>
public class PackageSearchService : IPackageSearchService
{
    private readonly ISearchService _local;
    private readonly IUpstreamClient _upstream;
    private readonly IUrlGenerator _url;
    private readonly INuGetUrlRewriter _rewriter;
    private readonly IUpstreamHostProvider _upstreamHosts;
    private readonly IMemoryCache _cache;
    private readonly SearchOptions _options;
    private readonly FullProxyOptions _fullProxy;

    public PackageSearchService(
        ISearchService local,
        IUpstreamClient upstream,
        IUrlGenerator url,
        INuGetUrlRewriter rewriter,
        IUpstreamHostProvider upstreamHosts,
        IMemoryCache cache,
        IOptions<SearchOptions> options,
        IOptions<FullProxyOptions> fullProxyOptions)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(upstream);
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(rewriter);
        ArgumentNullException.ThrowIfNull(upstreamHosts);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fullProxyOptions);

        _local = local;
        _upstream = upstream;
        _url = url;
        _rewriter = rewriter;
        _upstreamHosts = upstreamHosts;
        _cache = cache;
        _options = options.Value;
        _fullProxy = fullProxyOptions.Value;
    }

    // Full proxy implies upstream search: clients cannot reach the upstream, so
    // BaGetter must serve upstream results regardless of the explicit setting.
    private bool IncludeUpstream => _options.IncludeUpstream || _fullProxy.Enabled;

    public async Task<SearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!IncludeUpstream)
        {
            return await _local.SearchAsync(request, cancellationToken);
        }

        await _upstreamHosts.EnsureResolvedAsync(cancellationToken);

        var expandedRequest = new SearchRequest
        {
            Skip = 0,
            Take = request.Skip + request.Take,
            IncludePrerelease = request.IncludePrerelease,
            IncludeSemVer2 = request.IncludeSemVer2,
            PackageType = request.PackageType,
            Framework = request.Framework,
            Query = request.Query
        };

        var localTask = _local.SearchAsync(expandedRequest, cancellationToken);
        var upstreamTask = UpstreamSearchAsync(
            request.Query,
            take: expandedRequest.Take,
            includePrerelease: request.IncludePrerelease,
            includeSemVer2: request.IncludeSemVer2,
            cancellationToken: cancellationToken);

        await Task.WhenAll(localTask, upstreamTask);

        var local = await localTask;
        var upstream = (await upstreamTask)
            .Select(result => FilterVersions(result, request))
            .Where(result => result != null)
            .Where(result => MatchesPackageType(result, request.PackageType))
            .ToList();

        var upstreamIds = new HashSet<string>(
            upstream.Select(result => result.PackageId),
            StringComparer.OrdinalIgnoreCase);

        var merged = MergeSearchResults(
                local.Data ?? Array.Empty<SearchResult>(),
                upstream)
            .Select(RewriteUrls)
            .ToList();

        var data = merged
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        await EnrichLocalOnlyDownloadCountsAsync(data, upstreamIds, cancellationToken);

        return new SearchResponse
        {
            TotalHits = merged.Count,
            Data = data,
            Context = SearchContext.Default(_url.GetPackageMetadataResourceUrl())
        };
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken cancellationToken)
    {
        if (!IncludeUpstream)
        {
            return await _local.AutocompleteAsync(request, cancellationToken);
        }

        var expandedRequest = new AutocompleteRequest
        {
            Skip = 0,
            Take = request.Skip + request.Take,
            IncludePrerelease = request.IncludePrerelease,
            IncludeSemVer2 = request.IncludeSemVer2,
            PackageType = request.PackageType,
            Query = request.Query
        };

        var localTask = _local.AutocompleteAsync(expandedRequest, cancellationToken);
        var upstreamTask = UpstreamAutocompleteAsync(
            request.Query,
            take: expandedRequest.Take,
            includePrerelease: request.IncludePrerelease,
            cancellationToken: cancellationToken);

        await Task.WhenAll(localTask, upstreamTask);

        var merged = ((await localTask).Data ?? Array.Empty<string>())
            .Concat(await upstreamTask)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var data = merged
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        return new AutocompleteResponse
        {
            TotalHits = merged.Count,
            Data = data,
            Context = AutocompleteContext.Default
        };
    }

    public async Task<AutocompleteResponse> ListPackageVersionsAsync(
        VersionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!IncludeUpstream)
        {
            return await _local.ListPackageVersionsAsync(request, cancellationToken);
        }

        var localTask = _local.ListPackageVersionsAsync(request, cancellationToken);
        var upstreamTask = _upstream.ListPackageVersionsAsync(request.PackageId, cancellationToken);

        await Task.WhenAll(localTask, upstreamTask);

        var data = ((await localTask).Data ?? Array.Empty<string>())
            .Select(NuGetVersion.Parse)
            .Concat(await upstreamTask)
            .Where(version => request.IncludePrerelease || !version.IsPrerelease)
            .Where(version => request.IncludeSemVer2 || !version.IsSemVer2)
            .Distinct()
            .OrderBy(version => version)
            .Select(version => version.ToNormalizedString().ToLowerInvariant())
            .ToList();

        return new AutocompleteResponse
        {
            TotalHits = data.Count,
            Data = data,
            Context = AutocompleteContext.Default
        };
    }

    public Task<DependentsResponse> FindDependentsAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        return _local.FindDependentsAsync(packageId, cancellationToken);
    }

    private async Task<IReadOnlyList<SearchResult>> UpstreamSearchAsync(
        string query,
        int take,
        bool includePrerelease,
        bool includeSemVer2,
        CancellationToken cancellationToken)
    {
        if (!UseUpstreamCache)
        {
            return await _upstream.SearchAsync(query, skip: 0, take, includePrerelease, cancellationToken);
        }

        var key = $"fullproxy:search:{includePrerelease}:{includeSemVer2}:{take}:{query}";
        if (_cache.TryGetValue(key, out IReadOnlyList<SearchResult> cached))
        {
            return cached;
        }

        var result = await _upstream.SearchAsync(query, skip: 0, take, includePrerelease, cancellationToken);
        _cache.Set(key, result, TimeSpan.FromMinutes(_fullProxy.CacheMinutes));
        return result;
    }

    private async Task<IReadOnlyList<string>> UpstreamAutocompleteAsync(
        string query,
        int take,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        if (!UseUpstreamCache)
        {
            return await _upstream.AutocompleteAsync(query, skip: 0, take, includePrerelease, cancellationToken);
        }

        var key = $"fullproxy:autocomplete:{includePrerelease}:{take}:{query}";
        if (_cache.TryGetValue(key, out IReadOnlyList<string> cached))
        {
            return cached;
        }

        var result = await _upstream.AutocompleteAsync(query, skip: 0, take, includePrerelease, cancellationToken);
        _cache.Set(key, result, TimeSpan.FromMinutes(_fullProxy.CacheMinutes));
        return result;
    }

    private bool UseUpstreamCache => _fullProxy.Enabled && _fullProxy.CacheMinutes > 0;

    private static SearchResult FilterVersions(SearchResult result, SearchRequest request)
    {
        if (result?.Versions == null)
        {
            return result;
        }

        var versions = result.Versions
            .Where(version => NuGetVersion.TryParse(version.Version, out _))
            .Where(version =>
            {
                var parsed = NuGetVersion.Parse(version.Version);
                return (request.IncludePrerelease || !parsed.IsPrerelease)
                    && (request.IncludeSemVer2 || !parsed.IsSemVer2);
            })
            .OrderByDescending(version => NuGetVersion.Parse(version.Version))
            .ToList();

        if (versions.Count == 0)
        {
            return null;
        }

        result.Versions = versions;
        result.Version = versions[0].Version;
        return result;
    }

    private SearchResult RewriteUrls(SearchResult result)
    {
        result.RegistrationIndexUrl = _url.GetRegistrationIndexUrl(result.PackageId);
        result.IconUrl = _rewriter.RewriteAssetUrl(result.IconUrl);
        result.LicenseUrl = _rewriter.RewriteAssetUrl(result.LicenseUrl);

        if (result.Versions != null)
        {
            foreach (var version in result.Versions)
            {
                if (NuGetVersion.TryParse(version.Version, out var nugetVersion))
                {
                    version.RegistrationLeafUrl = _url.GetRegistrationLeafUrl(
                        result.PackageId,
                        nugetVersion);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Filter upstream search results by the requested package type.
    /// Packages with no declared types default to "Dependency".
    /// </summary>
    private static bool MatchesPackageType(SearchResult result, string packageType)
    {
        if (string.IsNullOrEmpty(packageType)) return true;

        if (result.PackageTypes is null || result.PackageTypes.Count == 0)
        {
            return string.Equals("dependency", packageType, StringComparison.OrdinalIgnoreCase);
        }

        return result.PackageTypes.Any(t =>
            string.Equals(t.Name, packageType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Merge local and upstream results, deduplicating by package ID.
    /// Upstream results that overlap with local results add their download
    /// counts to the local counts.
    /// </summary>
    private static IEnumerable<SearchResult> MergeSearchResults(
        IReadOnlyList<SearchResult> local,
        IEnumerable<SearchResult> upstream)
    {
        var results = new Dictionary<string, SearchResult>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();

        foreach (var result in local.Concat(upstream))
        {
            if (!results.TryGetValue(result.PackageId, out var existing))
            {
                results[result.PackageId] = result;
                order.Add(result.PackageId);
                continue;
            }

            var versions = (existing.Versions ?? Array.Empty<SearchResultVersion>())
                .Concat(result.Versions ?? Array.Empty<SearchResultVersion>())
                .GroupBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderByDescending(version => NuGetVersion.Parse(version.Version))
                .ToList();

            existing.Versions = versions;
            if (versions.Count > 0)
            {
                existing.Version = versions[0].Version;
            }

            existing.TotalDownloads += result.TotalDownloads;
        }

        return order.Select(id => results[id]);
    }

    /// <summary>
    /// For displayed local-only results, fetch upstream download counts and
    /// cache them. This keeps locally mirrored packages from showing only their
    /// local cache count when the package did not overlap the upstream search page.
    /// </summary>
    private async Task EnrichLocalOnlyDownloadCountsAsync(
        List<SearchResult> results,
        HashSet<string> upstreamIds,
        CancellationToken cancellationToken)
    {
        var candidates = results
            .Where(result => !upstreamIds.Contains(result.PackageId))
            .ToList();

        foreach (var result in candidates)
        {
            var cacheKey = "upstream:dl:" + result.PackageId;
            var notFoundKey = "upstream:nf:" + result.PackageId;

            if (_cache.TryGetValue(notFoundKey, out _))
                continue;

            if (_cache.TryGetValue(cacheKey, out long cachedDownloads))
            {
                result.TotalDownloads += cachedDownloads;
                continue;
            }

            try
            {
                var upstreamResults = await _upstream.SearchAsync(
                    result.PackageId,
                    skip: 0,
                    take: 10,
                    includePrerelease: true,
                    cancellationToken);

                var match = upstreamResults.FirstOrDefault(
                    r => string.Equals(r.PackageId, result.PackageId, StringComparison.OrdinalIgnoreCase));

                if (match != null && match.TotalDownloads > 0)
                {
                    _cache.Set(cacheKey, match.TotalDownloads, TimeSpan.FromHours(1));
                    result.TotalDownloads += match.TotalDownloads;
                }
                else
                {
                    _cache.Set(notFoundKey, true, TimeSpan.FromMinutes(30));
                }
            }
            catch
            {
                // Best-effort; don't cache failures so the next search retries.
            }
        }
    }
}
