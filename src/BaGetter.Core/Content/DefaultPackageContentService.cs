using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Storage;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core.Content;

/// <summary>
/// Implements the NuGet Package Content resource in NuGet's V3 protocol.
/// </summary>
public class DefaultPackageContentService : IPackageContentService
{
    private readonly IPackageService _packages;
    private readonly IPackageStorageService _storage;

    public DefaultPackageContentService(
        IPackageService packages,
        IPackageStorageService storage)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<PackageVersionsResponse> GetPackageVersionsOrNullAsync(
        Guid feedId,
        string feedSlug,
        string id,
        CancellationToken cancellationToken = default)
    {
        var versions = await _packages.FindPackageVersionsAsync(feedId, id, cancellationToken);
        if (!versions.Any())
        {
            return null;
        }

        return new PackageVersionsResponse
        {
            Versions = versions
                .Select(v => v.ToNormalizedString())
                .Select(v => v.ToLowerInvariant())
                .ToList()
        };
    }

    public async Task<Stream> GetPackageContentStreamOrNullAsync(
        Guid feedId,
        string feedSlug,
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken = default)
    {
        if (!await _packages.ExistsAsync(feedId, feedSlug, id, version, cancellationToken))
        {
            return null;
        }

        await _packages.AddDownloadAsync(feedId, id, version, cancellationToken);
        return await _storage.GetPackageStreamAsync(feedSlug, id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageManifestStreamOrNullAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        if (!await _packages.ExistsAsync(feedId, feedSlug, id, version, cancellationToken))
        {
            return null;
        }

        return await _storage.GetNuspecStreamAsync(feedSlug, id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageReadmeStreamOrNullAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindPackageOrNullAsync(feedId, feedSlug, id, version, cancellationToken);
        if (package == null || !package.HasReadme)
        {
            return null;
        }

        return await _storage.GetReadmeStreamAsync(feedSlug, id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageIconStreamOrNullAsync(Guid feedId, string feedSlug, string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindPackageOrNullAsync(feedId, feedSlug, id, version, cancellationToken);
        if (package == null || !package.HasEmbeddedIcon)
        {
            return null;
        }

        return await _storage.GetIconStreamAsync(feedSlug, id, version, cancellationToken);
    }
}
