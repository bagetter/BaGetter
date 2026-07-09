using System;
using System.Collections.Generic;
using System.Linq;
using BaGetter.Core.Metadata;
using BaGetter.Protocol.Models;

namespace BaGetter.Core.Search;

public class SearchResponseBuilder : ISearchResponseBuilder
{
    private readonly IUrlGenerator _url;

    public SearchResponseBuilder(IUrlGenerator url)
    {
        ArgumentNullException.ThrowIfNull(url);

        _url = url;
    }

    public SearchResponse BuildSearch(IReadOnlyList<PackageRegistration> packageRegistrations, bool includeUnlistedState = false)
    {
        var result = new List<SearchResult>();

        foreach (var packageRegistration in packageRegistrations)
        {
            var versions = packageRegistration.Packages.OrderByDescending(p => p.Version).ToList();

            // When unlisted packages are included, surface the latest *listed* version and only
            // fall back to the latest overall (marking the package unlisted) if none are listed.
            var latestListed = includeUnlistedState ? versions.FirstOrDefault(p => p.Listed) : null;
            var latest = latestListed ?? versions.First();
            bool? listed = includeUnlistedState ? latestListed != null : null;

            var iconUrl = latest.HasEmbeddedIcon
                ? _url.GetPackageIconDownloadUrl(latest.Id, latest.Version)
                : latest.IconUrlString;

            result.Add(new SearchResult
            {
                PackageId = latest.Id,
                Version = latest.Version.ToFullString(),
                Description = latest.Description,
                Authors = latest.Authors,
                IconUrl = iconUrl,
                LicenseUrl = latest.LicenseUrlString,
                ProjectUrl = latest.ProjectUrlString,
                RegistrationIndexUrl = _url.GetRegistrationIndexUrl(latest.Id),
                Summary = latest.Summary,
                Tags = latest.Tags,
                Title = latest.Title,
                Listed = listed,
                TotalDownloads = versions.Sum(p => p.Downloads),
                Versions = versions
                    .Select(p => new SearchResultVersion
                    {
                        RegistrationLeafUrl = _url.GetRegistrationLeafUrl(p.Id, p.Version),
                        Version = p.Version.ToFullString(),
                        Downloads = p.Downloads,
                    })
                    .ToList(),
            });
        }

        return new SearchResponse
        {
            TotalHits = result.Count,
            Data = result,
            Context = SearchContext.Default(_url.GetPackageMetadataResourceUrl()),
        };
    }

    public AutocompleteResponse BuildAutocomplete(IReadOnlyList<string> data)
    {
        return new AutocompleteResponse
        {
            TotalHits = data.Count,
            Data = data,
            Context = AutocompleteContext.Default
        };
    }

    public DependentsResponse BuildDependents(IReadOnlyList<PackageDependent> packages)
    {
        return new DependentsResponse
        {
            TotalHits = packages.Count,
            Data = packages,
        };
    }
}
