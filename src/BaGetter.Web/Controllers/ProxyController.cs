using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaGetter.Web;

/// <summary>
/// Serves upstream package-resource assets through BaGetter when full proxy mode
/// is enabled, so clients never contact the upstream directly. The upstream URL
/// is validated server-side (HTTPS, trusted upstream host, non-private address)
/// before fetching.
/// </summary>
[Authorize(AuthenticationSchemes = AuthenticationConstants.NugetBasicAuthenticationScheme, Policy = AuthenticationConstants.NugetUserPolicy)]
public class ProxyController : Controller
{
    private readonly IAssetProxyService _assetProxy;

    public ProxyController(IAssetProxyService assetProxy)
    {
        ArgumentNullException.ThrowIfNull(assetProxy);

        _assetProxy = assetProxy;
    }

    public async Task<IActionResult> GetAsync(
        [FromQuery(Name = "url")] string url = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _assetProxy.GetAssetAsync(url, cancellationToken);

        return result.Status switch
        {
            AssetProxyStatus.Ok => File(result.Content, result.ContentType),
            AssetProxyStatus.NotFound => NotFound(),
            _ => BadRequest(),
        };
    }
}
