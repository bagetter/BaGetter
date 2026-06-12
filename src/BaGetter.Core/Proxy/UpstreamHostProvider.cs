using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

/// <summary>
/// Derives the trusted upstream host set from the configured mirror: the package
/// source host plus every host advertised by the upstream service index. The set
/// is resolved once and cached for the lifetime of the application.
/// </summary>
public class UpstreamHostProvider : IUpstreamHostProvider
{
    private readonly NuGetClientFactory _clientFactory;
    private readonly MirrorOptions _mirror;
    private readonly FullProxyOptions _fullProxy;
    private readonly ILogger<UpstreamHostProvider> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    private volatile HashSet<string> _hosts;

    public UpstreamHostProvider(
        NuGetClientFactory clientFactory,
        IOptions<MirrorOptions> mirror,
        IOptions<FullProxyOptions> fullProxy,
        ILogger<UpstreamHostProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(mirror);
        ArgumentNullException.ThrowIfNull(fullProxy);
        ArgumentNullException.ThrowIfNull(logger);

        _clientFactory = clientFactory;
        _mirror = mirror.Value;
        _fullProxy = fullProxy.Value;
        _logger = logger;
    }

    public async Task EnsureResolvedAsync(CancellationToken cancellationToken)
    {
        if (!_fullProxy.Enabled || !_mirror.Enabled || _hosts != null)
        {
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_hosts != null)
            {
                return;
            }

            var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sourceHost = _mirror.PackageSource?.Host;
            if (!string.IsNullOrEmpty(sourceHost))
            {
                hosts.Add(sourceHost);
            }

            try
            {
                // Resolve lazily so mirror-disabled installations do not need a
                // configured package source just to construct the provider.
                var serviceIndex = await _clientFactory
                    .CreateServiceIndexClient()
                    .GetAsync(cancellationToken);

                foreach (var resource in serviceIndex.Resources ?? [])
                {
                    if (Uri.TryCreate(resource.ResourceUrl, UriKind.Absolute, out var uri))
                    {
                        hosts.Add(uri.Host);
                    }
                }
            }
            catch (Exception e)
            {
                // Legacy (v2) upstreams or transient failures: fall back to the
                // package source host only.
                _logger.LogWarning(e, "Failed to resolve upstream service index hosts for full proxy; using package source host only");
            }

            _hosts = hosts;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public bool IsTrustedHost(string host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        var hosts = _hosts;
        return hosts != null && hosts.Contains(host);
    }
}
