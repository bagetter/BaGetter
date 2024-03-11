using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace BaGetter.Core;

using ILogger = Microsoft.Extensions.Logging.ILogger<V2UpstreamClient>;
using INuGetLogger = NuGet.Common.ILogger;

/// <summary>
/// The client to upstream a NuGet server that uses the V2 protocol.
/// </summary>
public class V2UpstreamClient : IUpstreamClient, IDisposable
{
    private readonly SourceCacheContext _cache;
    private readonly SourceRepository _repository;
    private readonly INuGetLogger _ngLogger;
    private readonly ILogger _logger;
    private static readonly char[] TagsSeparators = { ';' };
    private static readonly char[] AuthorsSeparators = new[] { ',', ';', '\t', '\n', '\r' };

    public V2UpstreamClient(
        IOptionsSnapshot<MirrorOptions> options,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Value?.PackageSource?.AbsolutePath == null)
        {
            throw new ArgumentException("No mirror package source has been set.");
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ngLogger = NullLogger.Instance;
        _cache = new SourceCacheContext();
        var packageSource = new PackageSource(options.Value.PackageSource.AbsoluteUri);
        if (options.Value.Authentication is { } auth)
        {
            switch (auth.Type)
            {
                case MirrorAuthenticationType.None:
                    break;
                case MirrorAuthenticationType.Basic:
                    packageSource.Credentials = new PackageSourceCredential(
                        packageSource.Source,
                        auth.Username,
                        auth.Password,
                        true,
                        null);
                    break;
                default:
                    throw new NotSupportedException($"The authentication type {auth.Type} is not supported.");
            }
        }
        _repository = Repository.Factory.GetCoreV2(packageSource);

    }

    public async Task<IReadOnlyList<NuGetVersion>> ListPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var versions = await resource.GetAllVersionsAsync(id, _cache, _ngLogger, cancellationToken);

            return versions.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to mirror {PackageId}'s upstream versions", id);
            return new List<NuGetVersion>();
        }
    }

    public async Task<IReadOnlyList<Package>> ListPackagesAsync(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var resource = await _repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);
            var packages = await resource.GetMetadataAsync(
                id,
                includePrerelease: true,
                includeUnlisted: true,
                _cache,
                _ngLogger,
                cancellationToken);

            return packages.Select(ToPackage).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to mirror {PackageId}'s upstream versions", id);
            return new List<Package>();
        }
    }

    public async Task<Stream> DownloadPackageOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken)
    {
        var packageStream = new MemoryStream();

        try
        {
            var resource = await _repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var success = await resource.CopyNupkgToStreamAsync(
                id, version, packageStream, _cache, _ngLogger,
                cancellationToken);

            if (!success)
            {
                packageStream.Dispose();
                return null;
            }

            packageStream.Position = 0;

            return packageStream;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to index package {Id} {Version} from upstream",
                id,
                version);

            packageStream.Dispose();
            return null;
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    private Package ToPackage(IPackageSearchMetadata package)
    {
        return new Package
        {
            Id = package.Identity.Id,
            Version = package.Identity.Version,
            Authors = ParseAuthors(package.Authors),
            Description = package.Description,
            Downloads = 0,
            HasReadme = false,
            Language = null,
            Listed = package.IsListed,
            MinClientVersion = null,
            Published = package.Published?.UtcDateTime ?? DateTime.MinValue,
            RequireLicenseAcceptance = package.RequireLicenseAcceptance,
            Summary = package.Summary,
            Title = package.Title,
            IconUrl = package.IconUrl,
            LicenseUrl = package.LicenseUrl,
            ProjectUrl = package.ProjectUrl,
            PackageTypes = new List<PackageType>(),
            RepositoryUrl = null,
            RepositoryType = null,
            Tags = package.Tags?.Split(TagsSeparators, StringSplitOptions.RemoveEmptyEntries),

            Dependencies = ToDependencies(package)
        };
    }

    private static string[] ParseAuthors(string authors)
    {
        if (string.IsNullOrEmpty(authors)) return Array.Empty<string>();

        return authors
            .Split(AuthorsSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim())
            .ToArray();
    }

    private List<PackageDependency> ToDependencies(IPackageSearchMetadata package)
    {
        return package
            .DependencySets
            .SelectMany(ToDependencies)
            .ToList();
    }

    private IEnumerable<PackageDependency> ToDependencies(PackageDependencyGroup group)
    {
        var framework = group.TargetFramework.GetShortFolderName();

        // BaGetter stores a dependency group with no dependencies as a package dependency
        // with no package id nor package version.
        if ((group.Packages?.Count() ?? 0) == 0)
        {
            return new[]
            {
                new PackageDependency
                {
                    Id = null,
                    VersionRange = null,
                    TargetFramework = framework,
                }
            };
        }

        return group.Packages.Select(d => new PackageDependency
        {
            Id = d.Id,
            VersionRange = d.VersionRange?.ToString(),
            TargetFramework = framework,
        });
    }
}
