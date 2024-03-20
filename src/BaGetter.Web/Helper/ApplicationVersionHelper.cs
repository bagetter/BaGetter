using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace BaGetter.Web.Helper;

/// <remarks>Based on: <see href="https://github.com/NuGet/NuGetGallery/tree/main/src/NuGetGallery/Infrastructure/ApplicationVersionHelper.cs"/></remarks>
internal static class ApplicationVersionHelper
{
    private static readonly Lazy<ApplicationVersion> _version = new(LoadVersion);

    public static ApplicationVersion GetVersion()
    {
        return _version.Value;
    }

    private static ApplicationVersion LoadVersion()
    {
        try
        {
            var assembly = typeof(ApplicationVersionHelper).Assembly;
            var metadataAttr = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);
            var infoVersionAttr = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var companyAttr = assembly
                .GetCustomAttribute<AssemblyCompanyAttribute>();

            var informationalVersion = infoVersionAttr?.InformationalVersion
                ?? typeof(ApplicationVersionHelper).Assembly.GetName().Version?.ToString()
                ?? string.Empty;
            var version = informationalVersion.Split("+").First();
            var authors = companyAttr?.Company ?? string.Empty;
            var branch = TryGet(metadataAttr, "CommitBranch");
            var commit = TryGet(metadataAttr, "CommitHash");
            var dateString = TryGet(metadataAttr, "BuildDateUtc");
            var repoUriString = TryGet(metadataAttr, "RepositoryUrl");

            if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var buildDate))
            {
                buildDate = DateTime.MinValue;
            }

            if (!Uri.TryCreate(repoUriString, UriKind.Absolute, out var repoUri))
            {
                repoUri = null;
            }

            return new ApplicationVersion(
                repoUri,
                informationalVersion,
                version,
                branch,
                commit,
                buildDate,
                authors);
        }
        catch
        {
            return ApplicationVersion.Empty;
        }
    }

    private static string TryGet(Dictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var val))
        {
            return string.Empty;
        }

        return val;
    }
}
