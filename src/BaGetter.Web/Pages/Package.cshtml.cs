using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Content;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Indexing;
using BaGetter.Core.Search;
using BaGetter.Web.Authentication;
using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace BaGetter.Web.Pages;

public class PackageModel : PageModel
{
    private static readonly MarkdownPipeline _markdownPipeline;

    private readonly IPackageService _packages;
    private readonly IPackageContentService _content;
    private readonly ISearchService _search;
    private readonly IUrlGenerator _url;
    private readonly IFeedContext _feedContext;
    private readonly IPermissionService _permissions;
    private readonly IPackageDeletionService _deletionService;
    private readonly IOptionsSnapshot<NugetAuthenticationOptions> _authOptions;

    static PackageModel()
    {
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public PackageModel(
        IPackageService packages,
        IPackageContentService content,
        ISearchService search,
        IUrlGenerator url,
        IFeedContext feedContext,
        IPermissionService permissions,
        IPackageDeletionService deletionService,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _search = search ?? throw new ArgumentNullException(nameof(search));
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _feedContext = feedContext ?? throw new ArgumentNullException(nameof(feedContext));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _deletionService = deletionService ?? throw new ArgumentNullException(nameof(deletionService));
        _authOptions = authOptions ?? throw new ArgumentNullException(nameof(authOptions));
    }

    public bool Found { get; private set; }

    /// <summary>
    /// Whether the current user may unlist/delete packages in this feed. Gates the
    /// Unlist and Delete buttons on the page. False in Config mode and for anonymous users.
    /// </summary>
    public bool CanDelete { get; private set; }

    public Package Package { get; private set; }

    public bool IsDotnetTemplate { get; private set; }
    public bool IsDotnetTool { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public long TotalDownloads { get; private set; }

    public IReadOnlyList<PackageDependent> UsedBy { get; set; }
    public IReadOnlyList<DependencyGroupModel> DependencyGroups { get; private set; }
    public IReadOnlyList<VersionModel> Versions { get; private set; }

    public HtmlString Readme { get; private set; }

    public HtmlString ParsedReleaseNotes { get; private set; }

    public string IconUrl { get; private set; }
    public string LicenseUrl { get; private set; }
    public string PackageDownloadUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, string version, CancellationToken cancellationToken)
    {
        var denied = await FeedAccessGuard.CheckReadAccessAsync(
            HttpContext, _feedContext, _permissions, _authOptions.Value.Mode, cancellationToken);
        if (denied != null) return denied;

        CanDelete = await FeedAccessGuard.CanDeleteFromCurrentFeedAsync(
            HttpContext, _feedContext, _permissions, _authOptions.Value.Mode, cancellationToken);

        var packages = await _packages.FindPackagesAsync(_feedContext.CurrentFeed.Id, id, cancellationToken);
        var listedPackages = packages.Where(p => p.Listed).ToList();

        // Try to find the requested version.
        if (NuGetVersion.TryParse(version, out var requestedVersion))
        {
            Package = packages.SingleOrDefault(p => p.Version == requestedVersion);
        }

        // Otherwise try to display the latest version.
        if (Package == null)
        {
            Package = listedPackages.OrderByDescending(p => p.Version).FirstOrDefault();
        }

        if (Package == null)
        {
            Package = new Package { Id = id };
            Found = false;
            return Page();
        }

        var packageVersion = Package.Version;

        Found = true;
        IsDotnetTemplate = Package.PackageTypes.Any(t => t.Name.Equals("Template", StringComparison.OrdinalIgnoreCase));
        IsDotnetTool = Package.PackageTypes.Any(t => t.Name.Equals("DotnetTool", StringComparison.OrdinalIgnoreCase));
        LastUpdated = packages.Max(p => p.Published);
        TotalDownloads = packages.Sum(p => p.Downloads);

        var dependents = await _search.FindDependentsAsync(_feedContext.CurrentFeed.Id, Package.Id, cancellationToken);

        UsedBy = dependents.Data;
        DependencyGroups = ToDependencyGroups(Package);

        // Managers (CanDelete) also see unlisted versions so they can relist them; the
        // versions table strikes those through. Everyone else only sees listed versions.
        var versionsToShow = CanDelete ? packages : listedPackages;
        Versions = ToVersions(versionsToShow, packageVersion);

        if (Package.HasReadme)
        {
            Readme = await GetReadmeHtmlStringOrNullAsync(Package.Id, packageVersion, cancellationToken);
        }

        ParsedReleaseNotes = ParseReleaseNotes();

        IconUrl = Package.HasEmbeddedIcon
            ? _url.GetPackageIconDownloadUrl(Package.Id, packageVersion)
            : Package.IconUrlString;
        LicenseUrl = Package.LicenseUrlString;
        PackageDownloadUrl = _url.GetPackageDownloadUrl(Package.Id, packageVersion);

        return Page();
    }

    public async Task<IActionResult> OnPostUnlistAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return NotFound();

        if (!await CanDeleteCurrentFeedAsync(cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _deletionService.TryUnlistPackageAsync(_feedContext.CurrentFeed.Id, id, nugetVersion, cancellationToken);

        return RedirectToPage(new { id, version });
    }

    public async Task<IActionResult> OnPostRelistAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return NotFound();

        if (!await CanDeleteCurrentFeedAsync(cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _deletionService.TryRelistPackageAsync(_feedContext.CurrentFeed.Id, id, nugetVersion, cancellationToken);

        return RedirectToPage(new { id, version });
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion)) return NotFound();

        if (!await CanDeleteCurrentFeedAsync(cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden);

        await _deletionService.TryHardDeletePackageAsync(
            _feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, nugetVersion, cancellationToken);

        // The version is gone; land on the package's default view (latest remaining or not-found).
        return RedirectToPage(new { id });
    }

    private Task<bool> CanDeleteCurrentFeedAsync(CancellationToken cancellationToken)
        => FeedAccessGuard.CanDeleteFromCurrentFeedAsync(
            HttpContext, _feedContext, _permissions, _authOptions.Value.Mode, cancellationToken);

    private static List<DependencyGroupModel> ToDependencyGroups(Package package)
    {
        return package
            .Dependencies
            .GroupBy(d => d.TargetFramework)
            .Select(group =>
            {
                return new DependencyGroupModel
                {
                    Name = PrettifyTargetFramework(group.Key),
                    Dependencies = group
                        .Where(d => d.Id != null)
                        .Select(d => new DependencyModel
                        {
                            PackageId = d.Id,
                            VersionSpec = (d.VersionRange != null)
                                ? VersionRange.Parse(d.VersionRange).PrettyPrint()
                                : string.Empty
                        })
                        .ToList()
                };
            })
            .ToList();
    }

    private static string PrettifyTargetFramework(string targetFramework)
    {
        if (targetFramework == null) return "All Frameworks";

        NuGetFramework framework;
        try
        {
            framework = NuGetFramework.Parse(targetFramework);
        }
        catch (Exception)
        {
            return targetFramework;
        }

        string frameworkName;
        if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetCoreApp,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = (framework.Version.Major >= 5)
                ? ".NET"
                : ".NET Core";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.NetStandard,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Standard";
        }
        else if (framework.Framework.Equals(FrameworkConstants.FrameworkIdentifiers.Net,
            StringComparison.OrdinalIgnoreCase))
        {
            frameworkName = ".NET Framework";
        }
        else
        {
            frameworkName = framework.Framework;
        }

        var frameworkVersion = (framework.Version.Build == 0)
            ? framework.Version.ToString(2)
            : framework.Version.ToString(3);

        return $"{frameworkName} {frameworkVersion}";
    }

    private static List<VersionModel> ToVersions(IReadOnlyList<Package> packages, NuGetVersion selectedVersion)
    {
        return packages
            .Select(p => new VersionModel
            {
                Version = p.Version,
                Downloads = p.Downloads,
                Selected = p.Version == selectedVersion,
                LastUpdated = p.Published,
                Listed = p.Listed,
            })
            .OrderByDescending(m => m.Version)
            .ToList();
    }

    private async Task<HtmlString> GetReadmeHtmlStringOrNullAsync(
        string packageId,
        NuGetVersion packageVersion,
        CancellationToken cancellationToken)
    {
        await using var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, packageId, packageVersion, cancellationToken);
        if (readmeStream == null) return null;

        using var reader = new StreamReader(readmeStream);
        var readme = await reader.ReadToEndAsync(cancellationToken);

        var readmeHtml = Markdown.ToHtml(readme, _markdownPipeline);
        return new HtmlString(readmeHtml);
    }

    private HtmlString ParseReleaseNotes()
    {
        if (string.IsNullOrWhiteSpace(Package.ReleaseNotes))
        {
            return HtmlString.Empty;
        }

        var releseNotesHtml = Markdown.ToHtml(Package.ReleaseNotes, _markdownPipeline);
        return new HtmlString(releseNotesHtml);
    }

    public class DependencyGroupModel
    {
        public string Name { get; set; }
        public IReadOnlyList<DependencyModel> Dependencies { get; set; }
    }

    // TODO: Convert this to records.
    public class DependencyModel
    {
        public string PackageId { get; set; }
        public string VersionSpec { get; set; }
    }

    // TODO: Convert this to records.
    public class VersionModel
    {
        public NuGetVersion Version { get; set; }
        public long Downloads { get; set; }
        public bool Selected { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool Listed { get; set; }
    }
}
