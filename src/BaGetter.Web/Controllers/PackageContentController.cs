using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using BaGetter.Protocol.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;

namespace BaGetter.Web;

/// <summary>
/// The Package Content resource, used to download content from packages.
/// See: https://docs.microsoft.com/nuget/api/package-base-address-resource
/// </summary>

[Authorize(AuthenticationSchemes = AuthenticationConstants.NugetBasicAuthenticationScheme, Policy = AuthenticationConstants.NugetUserPolicy)]
public class PackageContentController : Controller
{
    private readonly IPackageContentService _content;

    public PackageContentController(IPackageContentService content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _content = content;
    }

    public async Task<ActionResult<PackageVersionsResponse>> GetPackageVersionsAsync(string id, CancellationToken cancellationToken)
    {
        var versions = await _content.GetPackageVersionsOrNullAsync(id, cancellationToken);
        if (versions == null)
        {
            return NotFound();
        }

        var etag = HttpCacheUtility.CreateStrongEtagFromText(JsonSerializer.Serialize(versions));
        if (HttpCacheUtility.MatchesIfNoneMatch(Request, etag))
        {
            HttpCacheUtility.SetEtag(Response, etag);
            HttpCacheUtility.SetMustRevalidate(Response);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        HttpCacheUtility.SetEtag(Response, etag);
        HttpCacheUtility.SetMustRevalidate(Response);
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

        var etag = HttpCacheUtility.CreateStrongEtagFromParts("package", id.ToLowerInvariant(), nugetVersion.ToNormalizedString().ToLowerInvariant());
        if (HttpCacheUtility.MatchesIfNoneMatch(Request, etag))
        {
            HttpCacheUtility.SetEtag(Response, etag);
            HttpCacheUtility.SetImmutable(Response);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var packageStream = await _content.GetPackageContentStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (packageStream == null)
        {
            return NotFound();
        }

        HttpCacheUtility.SetEtag(Response, etag);
        HttpCacheUtility.SetImmutable(Response);
        return File(packageStream, "application/octet-stream");
    }

    public async Task<IActionResult> DownloadNuspecAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var etag = HttpCacheUtility.CreateStrongEtagFromParts("nuspec", id.ToLowerInvariant(), nugetVersion.ToNormalizedString().ToLowerInvariant());
        if (HttpCacheUtility.MatchesIfNoneMatch(Request, etag))
        {
            HttpCacheUtility.SetEtag(Response, etag);
            HttpCacheUtility.SetImmutable(Response);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var nuspecStream = await _content.GetPackageManifestStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (nuspecStream == null)
        {
            return NotFound();
        }

        HttpCacheUtility.SetEtag(Response, etag);
        HttpCacheUtility.SetImmutable(Response);
        return File(nuspecStream, "text/xml");
    }

    public async Task<IActionResult> DownloadReadmeAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var etag = HttpCacheUtility.CreateStrongEtagFromParts("readme", id.ToLowerInvariant(), nugetVersion.ToNormalizedString().ToLowerInvariant());
        if (HttpCacheUtility.MatchesIfNoneMatch(Request, etag))
        {
            HttpCacheUtility.SetEtag(Response, etag);
            HttpCacheUtility.SetImmutable(Response);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var readmeStream = await _content.GetPackageReadmeStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (readmeStream == null)
        {
            return NotFound();
        }

        HttpCacheUtility.SetEtag(Response, etag);
        HttpCacheUtility.SetImmutable(Response);
        return File(readmeStream, "text/markdown");
    }

    public async Task<IActionResult> DownloadIconAsync(string id, string version, CancellationToken cancellationToken)
    {
        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var etag = HttpCacheUtility.CreateStrongEtagFromParts("icon", id.ToLowerInvariant(), nugetVersion.ToNormalizedString().ToLowerInvariant());
        if (HttpCacheUtility.MatchesIfNoneMatch(Request, etag))
        {
            HttpCacheUtility.SetEtag(Response, etag);
            HttpCacheUtility.SetImmutable(Response);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var iconStream = await _content.GetPackageIconStreamOrNullAsync(id, nugetVersion, cancellationToken);
        if (iconStream == null)
        {
            return NotFound();
        }

        await using var bufferedStream = new MemoryStream();
        await iconStream.CopyToAsync(bufferedStream, cancellationToken);
        var iconBytes = bufferedStream.ToArray();

        HttpCacheUtility.SetEtag(Response, etag);
        HttpCacheUtility.SetImmutable(Response);
        return File(iconBytes, DetectImageContentType(iconBytes));
    }

    private static string DetectImageContentType(byte[] bytes)
    {
        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 6
            && bytes[0] == 0x47
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x38
            && (bytes[4] == 0x37 || bytes[4] == 0x39)
            && bytes[5] == 0x61)
        {
            return "image/gif";
        }

        if (bytes.Length >= 2
            && bytes[0] == 0x42
            && bytes[1] == 0x4D)
        {
            return "image/bmp";
        }

        if (bytes.Length >= 12
            && bytes[0] == 0x52
            && bytes[1] == 0x49
            && bytes[2] == 0x46
            && bytes[3] == 0x46
            && bytes[8] == 0x57
            && bytes[9] == 0x45
            && bytes[10] == 0x42
            && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }
}
