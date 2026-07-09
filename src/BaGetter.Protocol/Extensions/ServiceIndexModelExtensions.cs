using System;
using System.Linq;
using BaGetter.Protocol.Models;

namespace BaGetter.Protocol.Extensions;

/// <summary>
/// These are documented interpretations of values returned by the Service Index resource.
/// </summary>
public static class ServiceIndexModelExtensions
{
    // See: https://github.com/NuGet/NuGet.Client/blob/e08358296db5bfa6f7f32d6f4ec8de288f3b0388/src/NuGet.Core/NuGet.Protocol/ServiceTypes.cs
    private static readonly string _version200 = "/2.0.0";
    private static readonly string _version300Beta = "/3.0.0-beta";
    private static readonly string _version300 = "/3.0.0";
    private static readonly string _version340 = "/3.4.0";
    private static readonly string _version360 = "/3.6.0";
    private static readonly string _version470 = "/4.7.0";
    private static readonly string _version490 = "/4.9.0";
    private static readonly string _version500 = "/5.0.0";
    private static readonly string _version510 = "/5.1.0";

    private static readonly string[] _catalog = { "Catalog" + _version300 };
    private static readonly string[] _searchQueryService = { "SearchQueryService" + _version340, "SearchQueryService" + _version300Beta, "SearchQueryService" };
    private static readonly string[] _registrationsBaseUrl = { "RegistrationsBaseUrl" + _version360, "RegistrationsBaseUrl" + _version340, "RegistrationsBaseUrl" + _version300Beta, "RegistrationsBaseUrl" };
    private static readonly string[] _searchAutocompleteService = { "SearchAutocompleteService", "SearchAutocompleteService" + _version300Beta };
    private static readonly string[] _reportAbuse = { "ReportAbuseUriTemplate", "ReportAbuseUriTemplate" + _version300 };
    private static readonly string[] _packageDetailsUriTemplate = { "PackageDetailsUriTemplate" + _version510 };
    private static readonly string[] _legacyGallery = { "LegacyGallery" + _version200 };
    private static readonly string[] _packagePublish = { "PackagePublish" + _version200 };
    private static readonly string[] _packageBaseAddress = { "PackageBaseAddress" + _version300 };
    private static readonly string[] _repositorySignatures = { "RepositorySignatures" + _version500, "RepositorySignatures" + _version490, "RepositorySignatures" + _version470 };
    private static readonly string[] _symbolPackagePublish = { "SymbolPackagePublish" + _version490 };

    public static string GetPackageContentResourceUrl(this ServiceIndexResponse serviceIndex)
    {
        return serviceIndex.GetRequiredResourceUrl(_packageBaseAddress, "PackageBaseAddress");
    }

    public static string GetPackageMetadataResourceUrl(this ServiceIndexResponse serviceIndex)
    {
        return serviceIndex.GetRequiredResourceUrl(_registrationsBaseUrl, "RegistrationsBaseUrl");
    }

    public static string GetSearchQueryResourceUrl(this ServiceIndexResponse serviceIndex)
    {
        return serviceIndex.GetRequiredResourceUrl(_searchQueryService, "SearchQueryService");
    }

    public static string GetCatalogResourceUrl(this ServiceIndexResponse serviceIndex)
    {
        return serviceIndex.GetResourceUrl(_catalog);
    }

    public static string GetSearchAutocompleteResourceUrl(this ServiceIndexResponse serviceIndex)
    {
        return serviceIndex.GetResourceUrl(_searchAutocompleteService);
    }

    public static string GetResourceUrl(this ServiceIndexResponse serviceIndex, string[] types)
    {
        var resource = types.SelectMany(t => serviceIndex.Resources.Where(r => r.Type == t)).FirstOrDefault();

        return resource?.ResourceUrl.Trim('/');
    }

    public static string GetRequiredResourceUrl(this ServiceIndexResponse serviceIndex, string[] types, string resourceName)
    {
        // For more information on required resources,
        // see: https://docs.microsoft.com/en-us/nuget/api/overview#resources-and-schema
        var resourceUrl = serviceIndex.GetResourceUrl(types);
        if (string.IsNullOrEmpty(resourceUrl))
        {
            throw new InvalidOperationException(
                $"The service index does not have a resource named '{resourceName}'");
        }

        return resourceUrl;
    }
}
