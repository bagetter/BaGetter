using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// The mirroring client for a NuGet server that uses the V3 protocol.
/// </summary>
public class V3UpstreamClient : IUpstreamClient
{
    private readonly NuGetClient _client;
    private readonly ILogger<V3UpstreamClient> _logger;
    private static readonly char[] AuthorSeparator = [',', ';', '\t', '\n', '\r'];
    private static readonly char[] TagSeparator = [' '];

    public V3UpstreamClient(NuGetClient client, ILogger<V3UpstreamClient> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _logger = logger;
    }
    public string GetServiceIndexUrl() => _client.ServiceIndexUrl;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int skip,
        int take,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _client.SearchAsync(
                query,
                skip,
                take,
                includePrerelease,
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to search upstream packages");
            return new List<SearchResult>();
        }
    }

    public async Task<IReadOnlyList<string>> AutocompleteAsync(
        string query,
        int skip,
        int take,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _client.AutocompleteAsync(
                query,
                skip,
                take,
                includePrerelease,
                cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to autocomplete upstream package IDs");
            return new List<string>();
        }
    }

    public async Task<Stream> DownloadPackageOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        try
        {
            using var downloadStream = await _client.DownloadPackageAsync(id, version, cancellationToken);
            return await downloadStream.AsTemporaryFileStreamAsync(cancellationToken);
        }
        catch (PackageNotFoundException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to download {PackageId} {PackageVersion} from upstream",
                id,
                version);
            return null;
        }
    }

    public async Task<IReadOnlyList<Package>> ListPackagesAsync(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var packageMetadata = await _client.GetPackageMetadataAsync(id, cancellationToken);
            return packageMetadata.Select(ToPackage).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to mirror {PackageId}'s upstream metadata", id);
            return new List<Package>();
        }
    }

    public async Task<IReadOnlyList<NuGetVersion>> ListPackageVersionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _client.ListPackageVersionsAsync(id, includeUnlisted: true, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to mirror {PackageId}'s upstream versions", id);
            return new List<NuGetVersion>();
        }
    }

    public async Task EnrichPackageMetadataAsync(Package package, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (package.HasReadme && package.RepositoryUrl != null && package.ProjectUrl != null)
        {
            return;
        }

        try
        {
            using var manifestStream = await _client.DownloadPackageManifestAsync(
                package.Id,
                package.Version,
                cancellationToken);

            var nuspec = new NuspecReader(manifestStream);
            var (repositoryUrl, repositoryType) = PackageArchiveReaderExtensions.GetRepositoryMetadata(nuspec);

            package.HasReadme = package.HasReadme || !string.IsNullOrEmpty(nuspec.GetReadme());
            package.RepositoryUrl ??= repositoryUrl;
            if (string.IsNullOrEmpty(package.RepositoryType))
            {
                package.RepositoryType = repositoryType;
            }

            package.ProjectUrl ??= ParseUri(nuspec.GetProjectUrl());
        }
        catch (PackageNotFoundException)
        {
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e,
                "Failed to read {PackageId} {PackageVersion}'s upstream manifest metadata",
                package.Id,
                package.Version);
        }
    }

    public async Task<Stream> DownloadPackageReadmeOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        try
        {
            using var packageStream = await DownloadPackageOrNullAsync(id, version, cancellationToken);
            if (packageStream == null)
            {
                return null;
            }

            using var packageReader = new PackageArchiveReader(packageStream);
            if (!packageReader.HasReadme())
            {
                return null;
            }

            await using var readmeStream = await packageReader.GetReadmeAsync(cancellationToken);
            return await readmeStream.AsTemporaryFileStreamAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e,
                "Failed to read {PackageId} {PackageVersion}'s upstream readme",
                id,
                version);
            return null;
        }
    }

    private Package ToPackage(PackageMetadata metadata)
    {
        var version = metadata.ParseVersion();

        return new Package
        {
            Id = metadata.PackageId,
            Version = version,
            Authors = ParseAuthors(metadata.Authors),
            Description = metadata.Description,
            Downloads = metadata.Downloads,
            HasReadme = !string.IsNullOrEmpty(metadata.ReadmeUrl),
            IsPrerelease = version.IsPrerelease,
            Language = metadata.Language,
            Listed = metadata.IsListed(),
            MinClientVersion = metadata.MinClientVersion,
            Published = metadata.Published.UtcDateTime,
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
            Summary = metadata.Summary,
            Title = metadata.Title,
            IconUrl = ParseUri(metadata.IconUrl),
            LicenseUrl = ParseUri(metadata.LicenseUrl),
            ProjectUrl = ParseUri(metadata.ProjectUrl),
            PackageTypes = new List<PackageType>(),
            RepositoryUrl = ParseUri(metadata.RepositoryUrl),
            RepositoryType = metadata.RepositoryType,
            SemVerLevel = version.IsSemVer2 ? SemVerLevel.SemVer2 : SemVerLevel.Unknown,
            Tags = ParseTags(metadata.Tags),

            Dependencies = ToDependencies(metadata)
        };
    }

    private static Uri ParseUri(string uriString)
    {
        if (uriString == null) return null;

        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri;
    }

    private static string[] ParseAuthors(string authors)
    {
        if (string.IsNullOrEmpty(authors))
        {
            return Array.Empty<string>();
        }

        return authors.Split(AuthorSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string[] ParseTags(IEnumerable<string> tags)
    {
        if (tags is null)
        {
            return Array.Empty<string>();
        }

        return tags
            .SelectMany(t => t.Split(TagSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
    }

    private List<PackageDependency> ToDependencies(PackageMetadata package)
    {
        if ((package.DependencyGroups?.Count ?? 0) == 0)
        {
            return new List<PackageDependency>();
        }

        return package.DependencyGroups
            .SelectMany(ToDependencies)
            .ToList();
    }

    private IEnumerable<PackageDependency> ToDependencies(DependencyGroupItem group)
    {
        // BaGetter stores a dependency group with no dependencies as a package dependency
        // with no package id nor package version.
        if ((group.Dependencies?.Count ?? 0) == 0)
        {
            return new[]
            {
                new PackageDependency
                {
                    Id = null,
                    VersionRange = null,
                    TargetFramework = group.TargetFramework,
                }
            };
        }

        return group.Dependencies.Select(d => new PackageDependency
        {
            Id = d.Id,
            VersionRange = d.Range,
            TargetFramework = group.TargetFramework,
        });
    }
}
