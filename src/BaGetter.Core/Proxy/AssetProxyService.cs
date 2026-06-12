using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

/// <inheritdoc/>
public class AssetProxyService : IAssetProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IUpstreamHostProvider _hosts;
    private readonly IMemoryCache _cache;
    private readonly FullProxyOptions _options;
    private readonly ILogger<AssetProxyService> _logger;

    public AssetProxyService(
        HttpClient httpClient,
        IUpstreamHostProvider hosts,
        IMemoryCache cache,
        IOptions<FullProxyOptions> options,
        ILogger<AssetProxyService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(hosts);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _hosts = hosts;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AssetProxyResult> GetAssetAsync(string url, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return AssetProxyResult.Rejected;
        }

        await _hosts.EnsureResolvedAsync(cancellationToken);

        if (!TryValidate(url, out var uri) || !_hosts.IsTrustedHost(uri.Host))
        {
            return AssetProxyResult.Rejected;
        }

        // Defense in depth: reject hosts that resolve to loopback/private/link-local
        // addresses, even if they slipped into the trusted set (DNS rebinding).
        if (await ResolvesToDisallowedAddressCachedAsync(uri.Host, cancellationToken))
        {
            return AssetProxyResult.Rejected;
        }

        var cacheKey = "fullproxy:asset:" + uri.AbsoluteUri;
        if (_cache.TryGetValue(cacheKey, out CachedAsset cached))
        {
            return AssetProxyResult.Ok(new MemoryStream(cached.Content, writable: false), cached.ContentType);
        }

        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AssetProxyResult.NotFound;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            if (_options.AssetCacheMinutes > 0)
            {
                _cache.Set(
                    cacheKey,
                    new CachedAsset(bytes, contentType),
                    TimeSpan.FromMinutes(_options.AssetCacheMinutes));
            }

            return AssetProxyResult.Ok(new MemoryStream(bytes, writable: false), contentType);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to proxy upstream asset {Url}", uri);
            return AssetProxyResult.NotFound;
        }
    }

    /// <summary>
    /// Validate that <paramref name="url"/> is an absolute HTTPS URL. Pure helper
    /// (no host-allowlist or DNS check) for unit testing.
    /// </summary>
    public static bool TryValidate(string url, out Uri uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    /// <summary>
    /// Whether an IP address is loopback, private, link-local or unique-local and
    /// must not be proxied. Pure helper for unit testing.
    /// </summary>
    public static bool IsDisallowedAddress(IPAddress address)
    {
        if (address is null || IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] == 10                                   // 10.0.0.0/8
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16
                || (b[0] == 169 && b[1] == 254);                // 169.254.0.0/16 (incl. cloud metadata)
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return IsDisallowedAddress(address.MapToIPv4());
            }

            return address.IsIPv6LinkLocal      // fe80::/10
                || address.IsIPv6SiteLocal       // fec0::/10 (deprecated)
                || address.IsIPv6UniqueLocal;    // fc00::/7
        }

        return false;
    }

    private async Task<bool> ResolvesToDisallowedAddressCachedAsync(string host, CancellationToken cancellationToken)
    {
        var cacheKey = "dns:" + host;
        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        var result = await ResolvesToDisallowedAddressAsync(host, cancellationToken);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }

    private static async Task<bool> ResolvesToDisallowedAddressAsync(string host, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var literal))
        {
            return IsDisallowedAddress(literal);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            return addresses.Length == 0 || addresses.Any(IsDisallowedAddress);
        }
        catch (SocketException)
        {
            return true;
        }
    }

    private sealed record CachedAsset(byte[] Content, string ContentType);
}
