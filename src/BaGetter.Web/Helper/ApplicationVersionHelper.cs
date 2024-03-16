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
            var metadata = typeof(ApplicationVersionHelper)
                .Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .ToDictionary(a => a.Key, a => a.Value, StringComparer.OrdinalIgnoreCase);
            var infoVer = typeof(ApplicationVersionHelper)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            var informationalVersion = infoVer == null ?
                typeof(ApplicationVersionHelper).Assembly.GetName().Version.ToString() :
                infoVer.InformationalVersion;
            var version = informationalVersion.Split("+").First();
            var branch = TryGet(metadata, "Branch");
            var commit = TryGet(metadata, "CommitId");
            var dateString = TryGet(metadata, "BuildDateUtc");
            var repoUriString = TryGet(metadata, "RepositoryUrl");

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
                buildDate);
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
