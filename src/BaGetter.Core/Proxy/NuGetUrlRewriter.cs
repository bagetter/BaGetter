using System;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

/// <inheritdoc/>
public class NuGetUrlRewriter : INuGetUrlRewriter
{
    private readonly FullProxyOptions _options;
    private readonly IUpstreamHostProvider _hosts;
    private readonly IUrlGenerator _url;

    public NuGetUrlRewriter(
        IOptions<FullProxyOptions> options,
        IUpstreamHostProvider hosts,
        IUrlGenerator url)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hosts);
        ArgumentNullException.ThrowIfNull(url);

        _options = options.Value;
        _hosts = hosts;
        _url = url;
    }

    /// <inheritdoc/>
    public string RewriteAssetUrl(string url)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !_hosts.IsTrustedHost(uri.Host))
        {
            return url;
        }

        return _url.GetPackageProxyUrl(url) ?? url;
    }
}
