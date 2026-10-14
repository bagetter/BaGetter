using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Web;

public class PackagePublishController : Controller
{
    private readonly IAuthenticationService _authentication;
    private readonly IPackageIndexingService _indexer;
    private readonly IPackageDatabase _packages;
    private readonly IPackageDeletionService _deleteService;
    private readonly IOptionsSnapshot<BaGetterOptions> _options;
    private readonly ILogger<PackagePublishController> _logger;

    public PackagePublishController(
        IAuthenticationService authentication,
        IPackageIndexingService indexer,
        IPackageDatabase packages,
        IPackageDeletionService deletionService,
        IOptionsSnapshot<BaGetterOptions> options,
        ILogger<PackagePublishController> logger)
    {
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _deleteService = deletionService ?? throw new ArgumentNullException(nameof(deletionService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // See: https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#push-a-package
    public async Task Upload(CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode)
        {
            _logger.LogWarning(
                "AUDIT package_upload_denied reason=read_only actor={Actor} ip={IpAddress}",
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            HttpContext.Response.StatusCode = 401;
            return;
        }

        if (!await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            _logger.LogWarning(
                "AUDIT package_upload_denied reason=unauthorized actor={Actor} ip={IpAddress}",
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());

            HttpContext.Response.StatusCode = 401;
            return;
        }

        try
        {
            using var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                _logger.LogWarning(
                    "AUDIT package_upload_failed reason=missing_body actor={Actor} ip={IpAddress}",
                    GetActor(),
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                HttpContext.Response.StatusCode = 400;
                return;
            }

            var result = await _indexer.IndexAsync(uploadStream, cancellationToken: cancellationToken);

            switch (result)
            {
                case PackageIndexingResult.InvalidPackage:
                    _logger.LogInformation(
                        "AUDIT package_upload_failed reason=invalid_package actor={Actor} ip={IpAddress}",
                        GetActor(),
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                    HttpContext.Response.StatusCode = 400;
                    break;

                case PackageIndexingResult.PackageAlreadyExists:
                    _logger.LogInformation(
                        "AUDIT package_upload_failed reason=already_exists actor={Actor} ip={IpAddress}",
                        GetActor(),
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                    HttpContext.Response.StatusCode = 409;
                    break;

                case PackageIndexingResult.Success:
                    _logger.LogInformation(
                        "AUDIT package_upload_succeeded actor={Actor} ip={IpAddress}",
                        GetActor(),
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                    HttpContext.Response.StatusCode = 201;
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown during package upload");

            HttpContext.Response.StatusCode = 500;
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string id, string version, CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode)
        {
            _logger.LogWarning(
                "AUDIT package_delete_denied reason=read_only package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                version,
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized();
        }

        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        var isAdminUser = HttpContext.User?.IsInRole("Admin") == true;
        var hasValidApiKey = await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken);
        if (!isAdminUser && !hasValidApiKey)
        {
            _logger.LogWarning(
                "AUDIT package_delete_denied reason=unauthorized package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                version,
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized();
        }

        if (await _deleteService.TryDeletePackageAsync(id, nugetVersion, cancellationToken))
        {
            _logger.LogInformation(
                "AUDIT package_delete_succeeded package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                nugetVersion.ToNormalizedString(),
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return NoContent();
        }
        else
        {
            _logger.LogInformation(
                "AUDIT package_delete_not_found package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                nugetVersion.ToNormalizedString(),
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Relist(string id, string version, CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode)
        {
            _logger.LogWarning(
                "AUDIT package_relist_denied reason=read_only package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                version,
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized();
        }

        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        if (!await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            _logger.LogWarning(
                "AUDIT package_relist_denied reason=unauthorized package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                version,
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized();
        }

        if (await _packages.RelistPackageAsync(id, nugetVersion, cancellationToken))
        {
            _logger.LogInformation(
                "AUDIT package_relist_succeeded package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                nugetVersion.ToNormalizedString(),
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok();
        }
        else
        {
            _logger.LogInformation(
                "AUDIT package_relist_not_found package_id={PackageId} package_version={PackageVersion} actor={Actor} ip={IpAddress}",
                id,
                nugetVersion.ToNormalizedString(),
                GetActor(),
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return NotFound();
        }
    }

    private string GetActor()
    {
        return HttpContext.User?.Identity?.Name ?? "anonymous";
    }
}
