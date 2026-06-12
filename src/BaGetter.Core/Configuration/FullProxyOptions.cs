using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

/// <summary>
/// Options for "full proxy" mode. When enabled, BaGetter rewrites upstream
/// package-resource asset URLs (icon and license URLs) found in metadata and
/// search responses to BaGetter asset-proxy URLs and fetches those assets
/// server-side, so clients never contact the upstream directly.
///
/// The set of trusted upstream hosts is derived at runtime from the configured
/// <see cref="MirrorOptions"/> upstream (package source + service index
/// resources), so this works for any provider without a static host list.
/// </summary>
public class FullProxyOptions
{
    /// <summary>
    /// Whether full proxy mode is enabled. When <see langword="false"/>, behavior
    /// is identical to BaGetter without full proxy.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// How long upstream search and autocomplete responses are cached, in minutes.
    /// Set to 0 to disable search caching.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int CacheMinutes { get; set; } = 30;

    /// <summary>
    /// How long proxied upstream assets are cached, in minutes.
    /// Set to 0 to disable asset caching.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int AssetCacheMinutes { get; set; } = 1440;
}
