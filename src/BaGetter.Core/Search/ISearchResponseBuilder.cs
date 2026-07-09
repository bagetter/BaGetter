using System.Collections.Generic;
using BaGetter.Core.Metadata;
using BaGetter.Protocol.Models;

namespace BaGetter.Core.Search;

public interface ISearchResponseBuilder
{
    /// <param name="results">The package registrations to project into search results.</param>
    /// <param name="includeUnlistedState">
    /// When true, each result shows its latest listed version (falling back to the latest version
    /// if all are unlisted) and carries a <see cref="SearchResult.Listed"/> flag. When false, the
    /// latest version is used and the flag is left null (the NuGet protocol behavior).
    /// </param>
    SearchResponse BuildSearch(IReadOnlyList<PackageRegistration> results, bool includeUnlistedState = false);
    AutocompleteResponse BuildAutocomplete(IReadOnlyList<string> data);
    DependentsResponse BuildDependents(IReadOnlyList<PackageDependent> results);
}
