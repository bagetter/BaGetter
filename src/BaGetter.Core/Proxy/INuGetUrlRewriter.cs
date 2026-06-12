namespace BaGetter.Core;

/// <summary>
/// Rewrites upstream package-resource asset URLs to BaGetter asset-proxy URLs
/// when full proxy mode is enabled, so the asset is fetched server-side instead
/// of by the client. URLs that are not trusted upstream infrastructure are
/// returned unchanged. This is the single, reusable rewriting layer used by both
/// metadata and search.
/// </summary>
public interface INuGetUrlRewriter
{
    /// <summary>
    /// Rewrite a single asset URL. Returns the input unchanged when full proxy is
    /// disabled, the URL is not an absolute HTTPS URL, or its host is not a
    /// trusted upstream host.
    /// </summary>
    string RewriteAssetUrl(string url);
}
