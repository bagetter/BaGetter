namespace BaGetter.Core;

public class RetentionOptions
{
    /// <summary>
    /// If this is set to a value, it will limit the number of versions that can be pushed for a package.
    /// The limit is applied to each major version of the package, and if the limit is exceeded,
    /// the older versions will be deleted.
    /// </summary>
    public uint? MaxHistoryPerMajorVersion { get; set; } = null;

    /// <summary>
    /// This corresponds to the maximum number of minor versions for each major version.
    /// If this is set to a value, it will limit the number of versions that can be pushed for a package.
    /// The limit is applied to each minor version of the package, and if the limit is exceeded,
    /// the older versions will be deleted.
    /// </summary>
    public uint? MaxHistoryPerMinorVersion { get; set; }

    /// <summary>
    /// If this is set to a value, it will limit the number of versions that can be pushed for a package.
    /// The limit is applied to each patch number of the package, and if the limit is exceeded,
    /// the older versions will be deleted.
    /// </summary>
    public uint? MaxHistoryPerPatch { get; set; }

    /// <summary>
    /// If this is set to a value, it will limit the number of versions that can be pushed for a package.
    /// The limit is applied to each pre-release of the package, and if the limit is exceeded,
    /// the older versions will be deleted.
    /// </summary>
    public uint? MaxHistoryPerPrerelease { get; set; }
}
