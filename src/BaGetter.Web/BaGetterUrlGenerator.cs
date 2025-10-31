using System;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NuGet.Versioning;

namespace BaGetter.Web;

// TODO: This should validate the "Host" header against known valid values
public class BaGetterUrlGenerator : IUrlGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public BaGetterUrlGenerator(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
    }

    public string GetServiceIndexUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.IndexRouteName, routeValues: null);
    }

    public string GetPackageContentResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, "v3/package/");
    }

    public string GetPackageMetadataResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, "v3/registration/");
    }

    public string GetPackagePublishResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.UploadPackageRouteName, routeValues: null);
    }

    public string GetSymbolPublishResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.UploadSymbolRouteName, routeValues: null);
    }

    public string GetSearchResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.SearchRouteName, routeValues: null);
    }

    public string GetAutocompleteResourceUrl()
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.AutocompleteRouteName, routeValues: null);
    }

    public string GetRegistrationIndexUrl(string id)
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.RegistrationIndexRouteName, new { Id = id.ToLowerInvariant() });
    }

    public string GetRegistrationPageUrl(string id, NuGetVersion lower, NuGetVersion upper)
    {
        // BaGetter does not support paging the registration resource.
        throw new NotImplementedException();
    }

    public string GetRegistrationLeafUrl(string id, NuGetVersion version)
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.RegistrationLeafRouteName, new
        {
            Id = id.ToLowerInvariant(),
            Version = version.ToNormalizedString().ToLowerInvariant(),
        });
    }

    public string GetPackageVersionsUrl(string id)
    {
        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.PackageVersionsRouteName, new { Id = id.ToLowerInvariant() });
    }

    public string GetPackageDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.PackageDownloadRouteName, new
        {
            Id = id,
            Version = versionString,
            IdVersion = $"{id}.{versionString}"
        });
    }

    public string GetPackageManifestDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.PackageDownloadRouteName, new
        {
            Id = id,
            Version = versionString,
            Id2 = id,
        });
    }

    public string GetPackageIconDownloadUrl(string id, NuGetVersion version)
    {
        id = id.ToLowerInvariant();
        var versionString = version.ToNormalizedString().ToLowerInvariant();

        return AbsoluteUrl(_httpContextAccessor.HttpContext, Routes.PackageDownloadIconRouteName, new
        {
            Id = id,
            Version = versionString
        });
    }

    private string AbsoluteUrl(HttpContext httpContext, string routeName, object routeValues)
    {
        var absoluteUrl = _linkGenerator.GetUriByRouteValues(httpContext, routeName, routeValues);

        var uri = new Uri(absoluteUrl, UriKind.Absolute);
        var result = AbsoluteUrl(httpContext, uri.AbsolutePath.TrimStart('/'));

        if (!string.IsNullOrEmpty(uri.Query))
        {
            result += ("?" + uri.Query);
        }

        return result;
    }

    private static string AbsoluteUrl(HttpContext httpContext, string relativePath)
    {
        var request = httpContext.Request;

        var scheme = httpContext.GetServerVariable("HTTP_X_FORWARDED_PROTO")?.Trim().ToLowerInvariant();
        var host = httpContext.GetServerVariable("HTTP_X_FORWARDED_HOST")?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(host))
        {
            host = httpContext.GetServerVariable("HTTP_X_FORWARDED_FOR")?.Trim().ToLowerInvariant();
        }

        var port = httpContext.GetServerVariable("HTTP_X_FORWARDED_PORT")?.Trim();

        if (string.IsNullOrEmpty(scheme))
        {
            scheme = request.Scheme;
        }

        if (string.IsNullOrEmpty(host))
        {
            host = request.Host.ToUriComponent();
            port = "";
        }
        else
        {
            port = ":" + port;
        }

        return string.Concat(
            scheme,
            "://",
            host,
            port,
            request.PathBase.ToUriComponent(),
            "/",
            relativePath);
    }
}
