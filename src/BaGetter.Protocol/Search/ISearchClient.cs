using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;

namespace BaGetter.Protocol;

/// <summary>
/// The client used to search for packages.
/// </summary>
/// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource"/></remarks>
public interface ISearchClient
{
    /// <summary>
    /// Perform a search query.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource#search-for-packages"/></remarks>
    /// <param name="query">The search query.</param>
    /// <param name="skip">How many results to skip.</param>
    /// <param name="take">How many results to return.</param>
    /// <param name="includePrerelease">Whether pre-release packages should be returned.</param>
    /// <param name="includeSemVer2">Whether packages that require SemVer 2.0.0 compatibility should be returned.</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The search response.</returns>
    Task<SearchResponse> SearchAsync(
        string query = null,
        int skip = 0,
        int take = 20,
        bool includePrerelease = true,
        bool includeSemVer2 = true,
        CancellationToken cancellationToken = default);
}
