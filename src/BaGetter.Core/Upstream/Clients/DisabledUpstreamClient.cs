using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// The client used when there are no upstream package sources.
/// </summary>
public class DisabledUpstreamClient : IUpstreamClient
{
    private readonly IReadOnlyList<NuGetVersion> _emptyVersionList = new List<NuGetVersion>();
    private readonly IReadOnlyList<Package> _emptyPackageList = new List<Package>();
    private readonly IReadOnlyList<SearchResult> _emptySearchResultList = new List<SearchResult>();
    private readonly IReadOnlyList<string> _emptyStringList = new List<string>();

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int skip,
        int take,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_emptySearchResultList);
    }

    public Task<IReadOnlyList<string>> AutocompleteAsync(
        string query,
        int skip,
        int take,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_emptyStringList);
    }

    public Task<IReadOnlyList<NuGetVersion>> ListPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_emptyVersionList);
    }

    public Task<IReadOnlyList<Package>> ListPackagesAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_emptyPackageList);
    }

    public Task EnrichPackageMetadataAsync(Package package, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadPackageOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(null);
    }

    public Task<Stream> DownloadPackageReadmeOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(null);
    }

    public string GetServiceIndexUrl()
    {
        return null;
    }
}
