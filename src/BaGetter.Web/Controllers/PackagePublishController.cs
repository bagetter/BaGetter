using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        if (_options.Value.IsReadOnlyMode ||
            !await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            HttpContext.Response.StatusCode = 401;
            return;
        }

        try
        {
            using var uploadStream = await Request.GetUploadStreamOrNullAsync(cancellationToken);
            if (uploadStream == null)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var result = await _indexer.IndexAsync(uploadStream, cancellationToken: cancellationToken);

            switch (result)
            {
                case PackageIndexingResult.InvalidPackage:
                    HttpContext.Response.StatusCode = 400;
                    break;

                case PackageIndexingResult.InvalidDependencyVersionRange:
                    await RejectAsync("A dependency has an invalid version range", cancellationToken);
                    break;

                case PackageIndexingResult.MissingRequiredMetadata:
                    await RejectAsync("The package must have authors and a description", cancellationToken);
                    break;

                case PackageIndexingResult.PackageAlreadyExists:
                    HttpContext.Response.StatusCode = 409;
                    break;

                case PackageIndexingResult.Success:
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
            return Unauthorized();
        }

        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        if (!await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            return Unauthorized();
        }

        if (await _deleteService.TryDeletePackageAsync(id, nugetVersion, cancellationToken))
        {
            return NoContent();
        }
        else
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Relist(string id, string version, CancellationToken cancellationToken)
    {
        if (_options.Value.IsReadOnlyMode)
        {
            return Unauthorized();
        }

        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return NotFound();
        }

        if (!await _authentication.AuthenticateAsync(Request.GetApiKey(), cancellationToken))
        {
            return Unauthorized();
        }

        if (await _packages.RelistPackageAsync(id, nugetVersion, cancellationToken))
        {
            return Ok();
        }
        else
        {
            return NotFound();
        }
    }

    // NuGet clients show the reason phrase of a failed push.
    private async Task RejectAsync(string reason, CancellationToken cancellationToken)
    {
        HttpContext.Response.StatusCode = 400;
        var responseFeature = HttpContext.Features.Get<IHttpResponseFeature>();
        if (responseFeature != null)
        {
            responseFeature.ReasonPhrase = reason;
        }

        await HttpContext.Response.WriteAsync(reason, cancellationToken);
    }
}
