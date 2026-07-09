using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;

namespace BaGetter.Core.Search;

/// <summary>
/// A minimal search service implementation, used for advanced scenarios.
/// </summary>
public class NullSearchService : ISearchService
{
    private static readonly IReadOnlyList<string> _emptyStringList = new List<string>();

    private static readonly Task<AutocompleteResponse> _emptyAutocompleteResponseTask =
        Task.FromResult(new AutocompleteResponse
        {
            TotalHits = 0,
            Data = _emptyStringList,
            Context = AutocompleteContext.Default
        });

    private static readonly Task<DependentsResponse> _emptyDependentsResponseTask =
        Task.FromResult(new DependentsResponse
        {
            TotalHits = 0,
            Data = new List<PackageDependent>()
        });

    private static readonly Task<SearchResponse> _emptySearchResponseTask =
        Task.FromResult(new SearchResponse
        {
            TotalHits = 0,
            Data = new List<SearchResult>()
        });

    public Task<AutocompleteResponse> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken cancellationToken)
    {
        return _emptyAutocompleteResponseTask;
    }

    public Task<AutocompleteResponse> ListPackageVersionsAsync(
        VersionsRequest request,
        CancellationToken cancellationToken)
    {
        return _emptyAutocompleteResponseTask;
    }

    public Task<DependentsResponse> FindDependentsAsync(Guid feedId, string packageId, CancellationToken cancellationToken)
    {
        return _emptyDependentsResponseTask;
    }

    public Task<SearchResponse> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        return _emptySearchResponseTask;
    }
}
