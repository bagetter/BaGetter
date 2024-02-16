using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;

namespace BaGetter.Core;

using NuGetPackageType = NuGet.Packaging.Core.PackageType;

public static class PackageArchiveReaderExtensions
{
    public static bool HasReadme(this PackageArchiveReader package)
        => !string.IsNullOrEmpty(package.NuspecReader.GetReadme());

    public static bool HasEmbeddedIcon(this PackageArchiveReader package)
        => !string.IsNullOrEmpty(package.NuspecReader.GetIcon());

    /// <summary>
    /// Indicates if the package has an embedded license file.
    /// </summary>    
    /// <returns><c>true</c> if the license file is embedded, otherwise <c>false</c>.</returns>
    public static bool HasEmbeddedLicense(this PackageArchiveReader package)
    {
        var licenseMetadata = package.NuspecReader.GetLicenseMetadata();
        if (licenseMetadata != null && licenseMetadata.Type == LicenseType.File)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Indicates if the license file format is Markdown.
    /// </summary>    
    /// <returns><c>true</c> if the license file format is Markdown, otherwise <c>false</c>.</returns>
    public static bool IsLicenseFormatMarkdown(this PackageArchiveReader package)
    {
        var licenseMetadata = package.NuspecReader.GetLicenseMetadata();
        if (licenseMetadata == null ||
            licenseMetadata.Type != LicenseType.File ||
            string.IsNullOrWhiteSpace(licenseMetadata.License))
        {
            return false;
        }

        return Path.GetFileName(licenseMetadata.License).EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<Stream> GetReadmeAsync(this PackageArchiveReader package, CancellationToken cancellationToken)
    {
        var readmePath = package.NuspecReader.GetReadme();
        if (readmePath == null)
        {
            throw new InvalidOperationException("Package does not have a readme!");
        }

        readmePath = PathUtility.StripLeadingDirectorySeparators(readmePath);

        return await package.GetStreamAsync(readmePath, cancellationToken);
    }

    public static async Task<Stream> GetIconAsync(this PackageArchiveReader package, CancellationToken cancellationToken)
    {
        return await package.GetStreamAsync(
            PathUtility.StripLeadingDirectorySeparators(package.NuspecReader.GetIcon()),
            cancellationToken);
    }

    public static async Task<Stream> GetLicenseAsync(this PackageArchiveReader package, CancellationToken cancellationToken)
    {
        var licenseMetadata = package.NuspecReader.GetLicenseMetadata();
        if (licenseMetadata.Type == LicenseType.File && licenseMetadata.License == null)
        {
            throw new InvalidOperationException("Package does not have a license file!");
        }

        var licensePath = PathUtility.StripLeadingDirectorySeparators(licenseMetadata.License);
        return await package.GetStreamAsync(licensePath, cancellationToken);
    }

    public static Package GetPackageMetadata(this PackageArchiveReader package)
    {
        var nuspec = package.NuspecReader;

        (var repositoryUri, var repositoryType) = GetRepositoryMetadata(nuspec);

        return new Package
        {
            Id = nuspec.GetId(),
            Version = nuspec.GetVersion(),
            Authors = ParseAuthors(nuspec.GetAuthors()),
            Description = nuspec.GetDescription(),
            HasReadme = package.HasReadme(),
            HasEmbeddedIcon = package.HasEmbeddedIcon(),
            HasEmbeddedLicense = package.HasEmbeddedLicense(),
            IsPrerelease = nuspec.GetVersion().IsPrerelease,
            Language = nuspec.GetLanguage() ?? string.Empty,
            LicenseFormatIsMarkdown = package.IsLicenseFormatMarkdown(),
            ReleaseNotes = nuspec.GetReleaseNotes() ?? string.Empty,
            Listed = true,
            MinClientVersion = nuspec.GetMinClientVersion()?.ToNormalizedString() ?? string.Empty,
            Published = DateTime.UtcNow,
            RequireLicenseAcceptance = nuspec.GetRequireLicenseAcceptance(),
            SemVerLevel = GetSemVerLevel(nuspec),
            Summary = nuspec.GetSummary(),
            Title = nuspec.GetTitle(),
            IconUrl = ParseUri(nuspec.GetIconUrl()),
            LicenseUrl = ParseUri(nuspec.GetLicenseUrl()),
            ProjectUrl = ParseUri(nuspec.GetProjectUrl()),
            RepositoryUrl = repositoryUri,
            RepositoryType = repositoryType,
            Dependencies = GetDependencies(nuspec),
            Tags = ParseTags(nuspec.GetTags()),
            PackageTypes = GetPackageTypes(nuspec),
            TargetFrameworks = GetTargetFrameworks(package),
        };
    }

    /// <remarks>Based off: <see href="https://github.com/NuGet/NuGetGallery/blob/master/src/NuGetGallery.Core/SemVerLevelKey.cs"/></remarks>
    private static SemVerLevel GetSemVerLevel(NuspecReader nuspec)
    {
        if (nuspec.GetVersion().IsSemVer2)
        {
            return SemVerLevel.SemVer2;
        }

        foreach (var dependencyGroup in nuspec.GetDependencyGroups())
        {
            foreach (var dependency in dependencyGroup.Packages)
            {
                if ((dependency.VersionRange.MinVersion != null && dependency.VersionRange.MinVersion.IsSemVer2)
                    || (dependency.VersionRange.MaxVersion != null && dependency.VersionRange.MaxVersion.IsSemVer2))
                {
                    return SemVerLevel.SemVer2;
                }
            }
        }

        return SemVerLevel.Unknown;
    }

    private static Uri ParseUri(string uriString)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            return null;
        }

        return new Uri(uriString);
    }

    private static readonly char[] Separator = { ',', ';', '\t', '\n', '\r' };

    private static string[] ParseAuthors(string authors)
    {
        if (string.IsNullOrEmpty(authors))
        {
            return Array.Empty<string>();
        }

        return authors.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] ParseTags(string tags)
    {
        if (string.IsNullOrEmpty(tags))
        {
            return Array.Empty<string>();
        }

        return tags.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    private static (Uri repositoryUrl, string repositoryType) GetRepositoryMetadata(NuspecReader nuspec)
    {
        var repository = nuspec.GetRepositoryMetadata();

        if (string.IsNullOrEmpty(repository?.Url) ||
            !Uri.TryCreate(repository.Url, UriKind.Absolute, out var repositoryUri))
        {
            return (null, null);
        }

        if (repositoryUri.Scheme != Uri.UriSchemeHttps)
        {
            return (null, null);
        }

        if (repository.Type.Length > 100)
        {
            throw new InvalidOperationException("Repository type must be less than or equal 100 characters");
        }

        return (repositoryUri, repository.Type);
    }

    private static List<PackageDependency> GetDependencies(NuspecReader nuspec)
    {
        var dependencies = new List<PackageDependency>();

        foreach (var group in nuspec.GetDependencyGroups())
        {
            var targetFramework = group.TargetFramework.GetShortFolderName();

            if (!group.Packages.Any())
            {
                dependencies.Add(new PackageDependency
                {
                    Id = null,
                    VersionRange = null,
                    TargetFramework = targetFramework,
                });
            }

            foreach (var dependency in group.Packages)
            {
                dependencies.Add(new PackageDependency
                {
                    Id = dependency.Id,
                    VersionRange = dependency.VersionRange?.ToString(),
                    TargetFramework = targetFramework,
                });
            }
        }

        return dependencies;
    }

    private static List<PackageType> GetPackageTypes(NuspecReader nuspec)
    {
        var packageTypes = nuspec
            .GetPackageTypes()
            .Select(t => new PackageType
            {
                Name = t.Name,
                Version = t.Version.ToString()
            })
            .ToList();

        // Default to the standard "dependency" package type if no types were found.
        if (packageTypes.Count == 0)
        {
            packageTypes.Add(new PackageType
            {
                Name = NuGetPackageType.Dependency.Name,
                Version = NuGetPackageType.Dependency.Version.ToString(),
            });
        }

        return packageTypes;
    }

    private static List<TargetFramework> GetTargetFrameworks(PackageArchiveReader package)
    {
        var targetFrameworks = package
            .GetSupportedFrameworks()
            .Select(f => new TargetFramework
            {
                Moniker = f.GetShortFolderName()
            })
            .ToList();

        // Default to the "any" framework if no frameworks were found.
        if (targetFrameworks.Count == 0)
        {
            targetFrameworks.Add(new TargetFramework { Moniker = "any" });
        }

        return targetFrameworks;
    }
}
