using System;
using System.Collections.Generic;
using System.Linq;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Core;

public class RegistrationBuilder
{
    private readonly IUrlGenerator _url;
    private readonly int _registrationPageSize;

    public RegistrationBuilder(IUrlGenerator url, IOptions<BaGetterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(options);

        _url = url;
        _registrationPageSize = options.Value.RegistrationPageSize;
        if (_registrationPageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "RegistrationPageSize must be greater than 0.");
        }
    }

    public virtual BaGetterRegistrationIndexResponse BuildIndex(PackageRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var sortedPackages = registration.Packages.OrderBy(p => p.Version).ToList();
        var pagedPackages = sortedPackages.Chunk(_registrationPageSize).ToList();
        var isPaged = pagedPackages.Count > 1;

        return new BaGetterRegistrationIndexResponse
        {
            RegistrationIndexUrl = _url.GetRegistrationIndexUrl(registration.PackageId),
            Type = RegistrationIndexResponse.DefaultType,
            Count = pagedPackages.Count,
            TotalDownloads = registration.Packages.Sum(p => p.Downloads),
            Pages = pagedPackages
                .Select(page =>
                {
                    var lower = page.First().Version;
                    var upper = page.Last().Version;

                    return new BaGetterRegistrationIndexPage
                    {
                        RegistrationPageUrl = isPaged
                            ? _url.GetRegistrationPageUrl(registration.PackageId, lower, upper)
                            : _url.GetRegistrationIndexUrl(registration.PackageId),
                        Count = page.Length,
                        Lower = lower.ToNormalizedString().ToLowerInvariant(),
                        Upper = upper.ToNormalizedString().ToLowerInvariant(),
                        ItemsOrNull = isPaged ? null : page.Select(ToRegistrationIndexPageItem).ToList(),
                    };
                })
                .ToList()
        };
    }

    public virtual BaGetterRegistrationPageResponse BuildPage(PackageRegistration registration, NuGetVersion lower, NuGetVersion upper)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(upper);

        if (VersionComparer.VersionRelease.Compare(lower, upper) > 0)
        {
            return null;
        }

        var packages = registration.Packages
            .Where(p => VersionComparer.VersionRelease.Compare(p.Version, lower) >= 0
                        && VersionComparer.VersionRelease.Compare(p.Version, upper) <= 0)
            .OrderBy(p => p.Version, VersionComparer.VersionRelease)
            .ToList();

        if (!packages.Any())
        {
            return null;
        }

        return new BaGetterRegistrationPageResponse
        {
            RegistrationPageUrl = _url.GetRegistrationPageUrl(registration.PackageId, lower, upper),
            Count = packages.Count,
            Lower = lower.ToNormalizedString().ToLowerInvariant(),
            Upper = upper.ToNormalizedString().ToLowerInvariant(),
            ItemsOrNull = packages.Select(ToRegistrationIndexPageItem).ToList(),
        };
    }

    public virtual RegistrationLeafResponse BuildLeaf(Package package)
    {
        var id = package.Id;
        var version = package.Version;

        return new RegistrationLeafResponse
        {
            Type = RegistrationLeafResponse.DefaultType,
            Listed = package.Listed,
            Published = package.Published,
            RegistrationLeafUrl = _url.GetRegistrationLeafUrl(id, version),
            PackageContentUrl = _url.GetPackageDownloadUrl(id, version),
            RegistrationIndexUrl = _url.GetRegistrationIndexUrl(id)
        };
    }

    private BaGetRegistrationIndexPageItem ToRegistrationIndexPageItem(Package package) =>
        new BaGetRegistrationIndexPageItem
        {
            RegistrationLeafUrl = _url.GetRegistrationLeafUrl(package.Id, package.Version),
            PackageContentUrl = _url.GetPackageDownloadUrl(package.Id, package.Version),
            PackageMetadata = new BaGetterPackageMetadata
            {
                PackageId = package.Id,
                Version = package.Version.ToFullString(),
                Authors = string.Join(", ", package.Authors),
                Description = package.Description,
                Downloads = package.Downloads,
                HasReadme = package.HasReadme,
                IconUrl = package.HasEmbeddedIcon
                    ? _url.GetPackageIconDownloadUrl(package.Id, package.Version)
                    : package.IconUrlString,
                Language = package.Language,
                LicenseUrl = package.LicenseUrlString,
                Listed = package.Listed,
                MinClientVersion = package.MinClientVersion,
                ReleaseNotes = package.ReleaseNotes,
                PackageContentUrl = _url.GetPackageDownloadUrl(package.Id, package.Version),
                PackageTypes = package.PackageTypes.Select(t => t.Name).ToList(),
                ProjectUrl = package.ProjectUrlString,
                RepositoryUrl = package.RepositoryUrlString,
                RepositoryType = package.RepositoryType,
                Published = package.Published,
                RequireLicenseAcceptance = package.RequireLicenseAcceptance,
                Summary = package.Summary,
                Tags = package.Tags,
                Title = package.Title,
                DependencyGroups = ToDependencyGroups(package)
            },
        };

    private static List<DependencyGroupItem> ToDependencyGroups(Package package)
    {
        return package.Dependencies
            .GroupBy(d => d.TargetFramework)
            .Select(group => new DependencyGroupItem
            {
                TargetFramework = group.Key,

                // A package that supports a target framework but does not have dependencies while on
                // that target framework is represented by a fake dependency with a null "Id" and "VersionRange".
                // This fake dependency should not be included in the output.
                Dependencies = group
                    .Where(d => d.Id != null && d.VersionRange != null)
                    .Select(d => new DependencyItem
                    {
                        Id = d.Id,
                        Range = d.VersionRange
                    })
                    .ToList()
            })
            .ToList();
    }
}
