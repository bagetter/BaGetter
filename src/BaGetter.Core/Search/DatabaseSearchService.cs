using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Indexing;
using BaGetter.Core.Metadata;
using BaGetter.Protocol.Models;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Core.Search;

public class DatabaseSearchService : ISearchService
{
    private readonly IContext _context;
    private readonly IFrameworkCompatibilityService _frameworks;
    private readonly ISearchResponseBuilder _searchBuilder;

    public DatabaseSearchService(IContext context, IFrameworkCompatibilityService frameworks, ISearchResponseBuilder searchBuilder)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(frameworks);
        ArgumentNullException.ThrowIfNull(searchBuilder);

        _context = context;
        _frameworks = frameworks;
        _searchBuilder = searchBuilder;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var frameworks = GetCompatibleFrameworksOrNull(request.Framework);

        var search = _context.Packages.Where(p => p.FeedId == request.FeedId);
        search = ApplySearchQuery(search, request.Query);
        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks,
            request.IncludeUnlisted);

        if (!string.IsNullOrEmpty(request.Tag))
        {
            var taggedPackageIds = await GetPackageIdsWithTagAsync(request.FeedId, request.Tag, request.IncludeUnlisted, cancellationToken);
            search = search.Where(p => taggedPackageIds.Contains(p.Id));
        }

        var packageIds = search
            .Select(p => p.Id)
            .Distinct()
            .OrderBy(id => id)
            .Skip(request.Skip)
            .Take(request.Take);

        // This query MUST fetch all versions for each package that matches the search,
        // otherwise the results for a package's latest version may be incorrect.
        // If possible, we'll find all these packages in a single query by matching
        // the package IDs in a subquery. Otherwise, run two queries:
        //   1. Find the package IDs that match the search
        //   2. Find all package versions for these package IDs
        if (_context.SupportsLimitInSubqueries)
        {
            search = _context.Packages.Where(p => p.FeedId == request.FeedId && packageIds.Contains(p.Id));
        }
        else
        {
            var packageIdResults = await packageIds.ToListAsync(cancellationToken);

            search = _context.Packages.Where(p => p.FeedId == request.FeedId && packageIdResults.Contains(p.Id));
        }

        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks,
            request.IncludeUnlisted);

        var results = await search.ToListAsync(cancellationToken);
        var groupedResults = results
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PackageRegistration(group.Key, group.ToList()))
            .ToList();

        var response = _searchBuilder.BuildSearch(groupedResults, request.IncludeUnlisted);

        if (request.IncludeFacets)
        {
            response.Facets = await ComputeFacetsAsync(request, cancellationToken);
        }

        return response;
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(AutocompleteRequest request, CancellationToken cancellationToken)
    {
        var search = _context.Packages.Where(p => p.FeedId == request.FeedId);

        search = ApplySearchQuery(search, request.Query);
        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks: null);

        var packageIds = await search
            .OrderByDescending(p => p.Downloads)
            .Select(p => p.Id)
            .Distinct()
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return _searchBuilder.BuildAutocomplete(packageIds);
    }

    public async Task<AutocompleteResponse> ListPackageVersionsAsync(VersionsRequest request, CancellationToken cancellationToken)
    {
        var packageId = request.PackageId.ToLower();
        var search = _context
            .Packages
            .Where(p => p.FeedId == request.FeedId && p.Id.ToLower().Equals(packageId));

        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            packageType: null,
            frameworks: null);

        var packageVersions = await search
            .Select(p => p.NormalizedVersionString)
            .ToListAsync(cancellationToken);

        return _searchBuilder.BuildAutocomplete(packageVersions);
    }

    public async Task<DependentsResponse> FindDependentsAsync(Guid feedId, string packageId, CancellationToken cancellationToken)
    {
        var dependents = await _context
            .Packages
            .Where(p => p.FeedId == feedId && p.Listed)
            .OrderByDescending(p => p.Downloads)
            .Where(p => p.Dependencies.Any(d => d.Id == packageId))
            .Take(20)
            .Select(r => new PackageDependent
            {
                Id = r.Id,
                Description = r.Description,
                TotalDownloads = r.Downloads
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return _searchBuilder.BuildDependents(dependents);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "Not for EF queries")]
    private static IQueryable<Package> ApplySearchQuery(IQueryable<Package> query, string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return query;
        }

        search = search.ToLowerInvariant();

        return query.Where(p => p.Id.ToLower().Contains(search));
    }

    private static IQueryable<Package> ApplySearchFilters(
        IQueryable<Package> query,
        bool includePrerelease,
        bool includeSemVer2,
        string packageType,
        IReadOnlyList<string> frameworks,
        bool includeUnlisted = false)
    {
        if (!includePrerelease)
        {
            query = query.Where(p => !p.IsPrerelease);
        }

        if (!includeSemVer2)
        {
            query = query.Where(p => p.SemVerLevel != SemVerLevel.SemVer2);
        }

        if (!string.IsNullOrEmpty(packageType))
        {
            query = query.Where(p => p.PackageTypes.Any(t => t.Name == packageType));
        }

        if (frameworks != null)
        {
            query = query.Where(p => p.TargetFrameworks.Any(f => frameworks.Contains(f.Moniker)));
        }

        if (!includeUnlisted)
        {
            query = query.Where(p => p.Listed);
        }

        return query;
    }

    private IReadOnlyList<string> GetCompatibleFrameworksOrNull(string framework)
    {
        if (framework == null) return null;

        return _frameworks.FindAllCompatibleFrameworks(framework);
    }

    /// <summary>
    /// Finds the IDs of the packages in a feed that carry a given tag.
    /// Tags are stored as a JSON string (value converter), so they can't be filtered in SQL;
    /// the (id, tags) pairs are materialized and matched in memory instead.
    /// </summary>
    private async Task<List<string>> GetPackageIdsWithTagAsync(Guid feedId, string tag, bool includeUnlisted, CancellationToken cancellationToken)
    {
        var rows = await _context.Packages
            .Where(p => p.FeedId == feedId && (includeUnlisted || p.Listed))
            .Select(p => new { p.Id, p.Tags })
            .ToListAsync(cancellationToken);

        return rows
            .Where(r => r.Tags != null && r.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.Id)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Computes the distinct package types, frameworks and tags present in a feed so the UI can
    /// populate its filter dropdowns with only the values that actually occur. Honors the prerelease
    /// and SemVer2 toggles, but is independent of the query and the package type/framework/tag filters.
    /// </summary>
    private async Task<SearchFacets> ComputeFacetsAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Packages.Where(p => p.FeedId == request.FeedId && p.Listed);

        if (!request.IncludePrerelease)
        {
            query = query.Where(p => !p.IsPrerelease);
        }

        if (!request.IncludeSemVer2)
        {
            query = query.Where(p => p.SemVerLevel != SemVerLevel.SemVer2);
        }

        var packageTypes = await query
            .SelectMany(p => p.PackageTypes.Select(t => t.Name))
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToListAsync(cancellationToken);

        var frameworks = await query
            .SelectMany(p => p.TargetFrameworks.Select(f => f.Moniker))
            .Where(moniker => !string.IsNullOrEmpty(moniker))
            .Distinct()
            .ToListAsync(cancellationToken);

        // Tags are stored as a JSON string (value converter), so aggregate them in memory.
        var tagArrays = await query
            .Select(p => p.Tags)
            .ToListAsync(cancellationToken);

        var tags = tagArrays
            .Where(t => t != null)
            .SelectMany(t => t)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        // "any" is the dropdowns' "no filter" sentinel; it's also the moniker stored for
        // framework-agnostic packages. Drop it so it doesn't show up as a bogus filter value.
        return new SearchFacets
        {
            PackageTypes = SortFacet(packageTypes),
            Frameworks = SortFacet(frameworks),
            Tags = SortFacet(tags),
        };
    }

    private static IReadOnlyList<string> SortFacet(IEnumerable<string> values)
    {
        return values
            .Where(v => !string.Equals(v, "any", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
