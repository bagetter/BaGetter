using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Upstream.Clients;
using BaGetter.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core.Upstream;

public class UpstreamClientFactory : IUpstreamClientFactory
{
    private readonly IFeedSettingsResolver _feedSettings;
    private readonly DisabledUpstreamClient _disabled;
    private readonly ILoggerFactory _loggerFactory;
    // Allows tests (and other callers) to override the HttpClient used for upstream requests.
    private readonly HttpClient _httpClientOverride;

    public UpstreamClientFactory(
        IFeedSettingsResolver feedSettings,
        DisabledUpstreamClient disabled,
        ILoggerFactory loggerFactory,
        HttpClient httpClientOverride = null)
    {
        _feedSettings = feedSettings ?? throw new ArgumentNullException(nameof(feedSettings));
        _disabled = disabled ?? throw new ArgumentNullException(nameof(disabled));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientOverride = httpClientOverride;
    }

    public IUpstreamClient CreateForFeed(Feed feed)
    {
        var mirrorOptions = _feedSettings.GetMirrorOptions(feed);

        if (!mirrorOptions.Enabled || mirrorOptions.PackageSource == null)
            return _disabled;

        if (mirrorOptions.Legacy)
        {
            var snapshot = new StaticOptionsSnapshot<MirrorOptions>(mirrorOptions);
            return new V2UpstreamClient(snapshot, _loggerFactory.CreateLogger<V2UpstreamClient>());
        }

        var logger = _loggerFactory.CreateLogger<UpstreamClientFactory>();
        var httpClient = _httpClientOverride ?? CreateHttpClient(mirrorOptions, feed.Id, logger);
        var clientFactory = new NuGetClientFactory(httpClient, mirrorOptions.PackageSource.ToString());
        var nugetClient = new NuGetClient(clientFactory);
        return new V3UpstreamClient(nugetClient, _loggerFactory.CreateLogger<V3UpstreamClient>());
    }

    // Headers that must never be forwarded to upstream feeds regardless of what is stored in the DB.
    // Mirrors the blocklist enforced on the write path in FeedSettings.cshtml.cs.
    private static readonly HashSet<string> _blockedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Host", "Content-Length", "Transfer-Encoding",
        "Connection", "Upgrade", "Proxy-Authorization", "Set-Cookie"
    };

    private static HttpClient CreateHttpClient(MirrorOptions options, Guid feedId, ILogger logger)
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        if (options.PackageDownloadTimeoutSeconds > 0)
            client.Timeout = TimeSpan.FromSeconds(options.PackageDownloadTimeoutSeconds);

        if (options.Authentication is { } auth)
        {
            switch (auth.Type)
            {
                case MirrorAuthenticationType.Basic:
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    break;

                case MirrorAuthenticationType.Bearer:
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    break;

                case MirrorAuthenticationType.Custom:
                    if (auth.CustomHeaders != null)
                    {
                        foreach (var (header, value) in auth.CustomHeaders)
                        {
                            if (_blockedHeaderNames.Contains(header))
                            {
                                logger.LogWarning(
                                    "Skipping blocked header '{HeaderName}' from feed {FeedId} MirrorAuthCustomHeaders.",
                                    header, feedId);
                                continue;
                            }
                            client.DefaultRequestHeaders.Add(header, value);
                        }
                    }
                    break;
            }
        }

        return client;
    }

    private sealed class StaticOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class
    {
        public StaticOptionsSnapshot(T value)
        {
            Value = value;
        }

        public T Value { get; }

        public T Get(string name) => Value;
    }
}
