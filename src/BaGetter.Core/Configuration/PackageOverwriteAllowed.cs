namespace BaGetter.Core;

/// <summary>
/// How BaGetter should react to a package being pushed that already exists.
/// </summary>
public enum PackageOverwriteAllowed
{
    /// <summary>
    /// Disallows all packages from being overwritten. This is the recommended setting and default behaviour for nuget.org and most other NuGet servers.
    /// </summary>
    False,

    /// <summary>
    /// Allows only prerelease packages to be overwritten.
    /// </summary>
    PrereleaseOnly,

    /// <summary>
    /// Allows all packages to be overwritten. Not recommended.
    /// </summary>
    True
}
