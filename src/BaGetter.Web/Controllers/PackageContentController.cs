using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Content;
using BaGetter.Core.Feeds;
using BaGetter.Protocol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;

namespace BaGetter.Web.Controllers;

/// <summary>
/// The Package Content resource, used to download content from packages.
/// See: https://docs.microsoft.com/nuget/api/package-base-address-resource
/// </summary>

[Authorize(AuthenticationSchemes = AuthenticationConstants.NugetBasicAuthenticationScheme, Policy = AuthenticationConstants.NugetUserPolicy)]
public class PackageContentController : Controller
{
    private readonly IPackageContentService _content;
    private readonly IFeedContext _feedContext;

    public PackageContentController(IPackageContentService content, IFeedContext feedContext)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(feedContext);

        _content = content;
        _feedContext = feedContext;
    }

    public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        var versions = await _content.GetPackageVersionsOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, cancellationToken);
        if (versions == null)
        {
            return NotFound();
        }

        return versions;
    }

    /// <summary>
    /// Download a specific package version.
    /// </summary>
    /// <param name="id">Package id, e.g. "BaGetter.Protocol".</param>
    /// <param name="version">Package version, e.g. "1.2.0".</param>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The requested package in an octet stream, or 404 not found if the package isn't found.</returns>
    public async Task<IActionResult> DownloadPackageAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var packageStream = await _content.GetPackageContentStreamOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, nugetVersion, cancellationToken);
        if (packageStream == null)
        {
            return NotFound();
        }

        return File(packageStream, "application/octet-stream");
    }

    public async Task<IActionResult> DownloadNuspecAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var nuspecStream = await _content.GetPackageManifestStreamOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, nugetVersion, cancellationToken);
        if (nuspecStream == null)
        {
            return NotFound();
        }

        return File(nuspecStream, "text/xml");
    }

    public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, nugetVersion, cancellationToken);
        if (readmeStream == null)
        {
            return NotFound();
        }

        return File(readmeStream, "text/markdown");
    }

    public async Task<IActionResult> DownloadIconAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var iconStream = await _content.GetPackageIconStreamOrNullAsync(_feedContext.CurrentFeed.Id, _feedContext.CurrentFeed.Slug, id, nugetVersion, cancellationToken);
        if (iconStream == null)
        {
            return NotFound();
        }

        return File(iconStream, "image/xyz");
    }
}
