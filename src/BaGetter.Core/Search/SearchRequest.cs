using System;

namespace BaGetter.Core.Search;

/// <summary>
/// The NuGet V3 search request.
/// </summary>
/// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource#request-parameters"/></remarks>
public class SearchRequest
{
    public Guid FeedId { get; set; }
    /// <summary>
    /// The number of results to skip, for pagination.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// The number of results to return, for pagination.
    /// </summary>
    public int Take { get; set; }

    /// <summary>
    /// Whether to include pre-release packages.
    /// </summary>
    public bool IncludePrerelease { get; set; }

    /// <summary>
    /// Whether to include SemVer 2.0.0 compatible packages.
    /// </summary>
    public bool IncludeSemVer2 { get; set; }

    /// <summary>
    /// Filter results to a package type. If null, no filter is applied.
    /// </summary>
    public string PackageType { get; set; }

    /// <summary>
    /// Filters results to a target framework. If null, no filter is applied.
    /// </summary>
    public string Framework { get; set; }

    /// <summary>
    /// Filters results to a tag. If null, no filter is applied.
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Whether to compute the facets (package types, frameworks and tags present in the feed)
    /// and return them on the <see cref="Protocol.Models.SearchResponse.Facets"/>.
    /// </summary>
    public bool IncludeFacets { get; set; }

    /// <summary>
    /// Whether to include packages that have no listed versions. When set, such packages are
    /// returned (shown via their latest version) with <see cref="Protocol.Models.SearchResult.Listed"/>
    /// set to <c>false</c> so the UI can mark them. The NuGet protocol search leaves this off.
    /// </summary>
    public bool IncludeUnlisted { get; set; }

    /// <summary>
    /// The search query.
    /// </summary>
    public string Query { get; set; }
}
