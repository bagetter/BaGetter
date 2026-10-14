using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using Microsoft.EntityFrameworkCore;

namespace BaGetter.Core;

public class DatabaseSearchService : ISearchService
{
    private const string TagQueryPrefix = "tag:";
    private const string AuthorQueryPrefix = "author:";

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
        var (textQuery, tags, authors) = ParseSearchQuery(request.Query);

        IQueryable<Package> search = _context.Packages;
        search = ApplySearchTextQuery(search, textQuery);
        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks);

        List<string> packageIdResults;
        if (tags.Count > 0 || authors.Count > 0)
        {
            packageIdResults = await GetFilteredPackageIdsForSearchAsync(
                search,
                tags,
                authors,
                request.Skip,
                request.Take,
                cancellationToken);
        }
        else
        {
            packageIdResults = await search
                .Select(p => p.Id)
                .Distinct()
                .OrderBy(id => id)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);
        }

        // This query MUST fetch all versions for each package that matches the search,
        // otherwise the results for a package's latest version may be incorrect.
        search = _context.Packages.Where(p => packageIdResults.Contains(p.Id));

        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks);

        var results = await search.ToListAsync(cancellationToken);
        var groupedResults = results
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PackageRegistration(group.Key, group.ToList()))
            .ToList();

        return _searchBuilder.BuildSearch(groupedResults);
    }

    public async Task<AutocompleteResponse> AutocompleteAsync(AutocompleteRequest request, CancellationToken cancellationToken)
    {
        var (textQuery, tags, authors) = ParseSearchQuery(request.Query);
        IQueryable<Package> search = _context.Packages;

        search = ApplySearchTextQuery(search, textQuery);
        search = ApplySearchFilters(
            search,
            request.IncludePrerelease,
            request.IncludeSemVer2,
            request.PackageType,
            frameworks: null);

        List<string> packageIds;
        if (tags.Count > 0 || authors.Count > 0)
        {
            packageIds = await GetFilteredPackageIdsForAutocompleteAsync(
                search,
                tags,
                authors,
                request.Skip,
                request.Take,
                cancellationToken);
        }
        else
        {
            packageIds = await search
                .OrderByDescending(p => p.Downloads)
                .Select(p => p.Id)
                .Distinct()
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);
        }

        return _searchBuilder.BuildAutocomplete(packageIds);
    }

    public async Task<AutocompleteResponse> ListPackageVersionsAsync(VersionsRequest request, CancellationToken cancellationToken)
    {
        var packageId = request.PackageId.ToLower();
        var search = _context
            .Packages
            .Where(p => p.Id.ToLower().Equals(packageId));

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

    public async Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken)
    {
        var dependents = await _context
            .Packages
            .Where(p => p.Listed)
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
    private static IQueryable<Package> ApplySearchTextQuery(IQueryable<Package> query, string textQuery)
    {
        if (string.IsNullOrEmpty(textQuery))
        {
            return query;
        }

        var normalizedTextQuery = textQuery.ToLowerInvariant();

        return query.Where(p => p.Id.ToLower().Contains(normalizedTextQuery));
    }

    private static (string TextQuery, IReadOnlyList<string> Tags, IReadOnlyList<string> Authors) ParseSearchQuery(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return (null, Array.Empty<string>(), Array.Empty<string>());
        }

        var textTerms = new List<string>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var authors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tokens = TokenizeQuery(search);
        foreach (var token in tokens)
        {
            if (token.StartsWith(TagQueryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var tag = token[TagQueryPrefix.Length..].Trim();
                if (!string.IsNullOrEmpty(tag))
                {
                    tags.Add(tag);
                }
                continue;
            }

            if (token.StartsWith(AuthorQueryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var author = token[AuthorQueryPrefix.Length..].Trim();
                if (!string.IsNullOrEmpty(author))
                {
                    authors.Add(author);
                }
                continue;
            }

            textTerms.Add(token);
        }

        var textQuery = textTerms.Count > 0 ? string.Join(' ', textTerms) : null;
        return (textQuery, tags.ToList(), authors.ToList());
    }

    private static List<string> TokenizeQuery(string input)
    {
        var tokens = new List<string>();
        var i = 0;
        var length = input.Length;

        while (i < length)
        {
            while (i < length && char.IsWhiteSpace(input[i]))
            {
                i++;
            }

            if (i >= length)
            {
                break;
            }

            var isTag = StartsWithAt(input, i, TagQueryPrefix);
            var isAuthor = !isTag && StartsWithAt(input, i, AuthorQueryPrefix);
            var prefix = isTag ? TagQueryPrefix : (isAuthor ? AuthorQueryPrefix : null);

            if (prefix != null)
            {
                i += prefix.Length;

                while (i < length && char.IsWhiteSpace(input[i]))
                {
                    i++;
                }

                var value = ReadTokenValue(input, ref i);
                tokens.Add(prefix + value);
                continue;
            }

            tokens.Add(ReadTokenValue(input, ref i));
        }

        return tokens;
    }

    private static bool StartsWithAt(string input, int index, string value)
    {
        return input.AsSpan(index).StartsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadTokenValue(string input, ref int index)
    {
        if (index >= input.Length)
        {
            return string.Empty;
        }

        if (input[index] == '"')
        {
            index++;
            var start = index;

            while (index < input.Length && input[index] != '"')
            {
                index++;
            }

            var value = input.Substring(start, index - start);

            if (index < input.Length && input[index] == '"')
            {
                index++;
            }

            return value;
        }

        var unquotedStart = index;
        while (index < input.Length && !char.IsWhiteSpace(input[index]))
        {
            index++;
        }

        return input.Substring(unquotedStart, index - unquotedStart);
    }

    private static bool HasAllTags(string[] packageTags, IReadOnlyList<string> requiredTags)
    {
        if (requiredTags == null || requiredTags.Count == 0)
        {
            return true;
        }

        if (packageTags == null || packageTags.Length == 0)
        {
            return false;
        }

        var packageTagSet = new HashSet<string>(packageTags, StringComparer.OrdinalIgnoreCase);
        return requiredTags.All(packageTagSet.Contains);
    }

    private static bool HasAllAuthors(string[] packageAuthors, IReadOnlyList<string> requiredAuthors)
    {
        if (requiredAuthors == null || requiredAuthors.Count == 0)
        {
            return true;
        }

        if (packageAuthors == null || packageAuthors.Length == 0)
        {
            return false;
        }

        return requiredAuthors.All(required =>
            packageAuthors.Any(author =>
                !string.IsNullOrEmpty(author) &&
                author.Contains(required, StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task<List<string>> GetFilteredPackageIdsForSearchAsync(
        IQueryable<Package> query,
        IReadOnlyList<string> requiredTags,
        IReadOnlyList<string> requiredAuthors,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (take <= 0)
        {
            return new List<string>();
        }

        var results = new List<string>(capacity: take);
        var seenPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchedCount = 0;

        await foreach (var candidate in query
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PackageFilterCandidate
            {
                Id = p.Id,
                Tags = p.Tags,
                Authors = p.Authors,
            })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (!seenPackageIds.Add(candidate.Id))
            {
                continue;
            }

            if (!HasAllTags(candidate.Tags, requiredTags) || !HasAllAuthors(candidate.Authors, requiredAuthors))
            {
                continue;
            }

            if (matchedCount++ < skip)
            {
                continue;
            }

            results.Add(candidate.Id);
            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }

    private static async Task<List<string>> GetFilteredPackageIdsForAutocompleteAsync(
        IQueryable<Package> query,
        IReadOnlyList<string> requiredTags,
        IReadOnlyList<string> requiredAuthors,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (take <= 0)
        {
            return new List<string>();
        }

        var results = new List<string>(capacity: take);
        var seenPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matchedCount = 0;

        await foreach (var candidate in query
            .AsNoTracking()
            .OrderByDescending(p => p.Downloads)
            .ThenBy(p => p.Id)
            .Select(p => new PackageFilterCandidate
            {
                Id = p.Id,
                Tags = p.Tags,
                Authors = p.Authors,
            })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (!seenPackageIds.Add(candidate.Id))
            {
                continue;
            }

            if (!HasAllTags(candidate.Tags, requiredTags) || !HasAllAuthors(candidate.Authors, requiredAuthors))
            {
                continue;
            }

            if (matchedCount++ < skip)
            {
                continue;
            }

            results.Add(candidate.Id);
            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }

    private static IQueryable<Package> ApplySearchFilters(
        IQueryable<Package> query,
        bool includePrerelease,
        bool includeSemVer2,
        string packageType,
        IReadOnlyList<string> frameworks)
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

        return query.Where(p => p.Listed);
    }

    private IReadOnlyList<string> GetCompatibleFrameworksOrNull(string framework)
    {
        if (framework == null) return null;

        return _frameworks.FindAllCompatibleFrameworks(framework);
    }

    private sealed class PackageFilterCandidate
    {
        public string Id { get; init; }
        public string[] Tags { get; init; }
        public string[] Authors { get; init; }
    }
}
