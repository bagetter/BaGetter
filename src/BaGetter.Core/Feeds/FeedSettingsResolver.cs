using System;
using System.Collections.Generic;
using System.Text.Json;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.Extensions.Options;

namespace BaGetter.Core.Feeds;

public class FeedSettingsResolver : IFeedSettingsResolver
{
    private readonly IOptionsSnapshot<BaGetterOptions> _options;

    public FeedSettingsResolver(IOptionsSnapshot<BaGetterOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public PackageOverwriteAllowed GetAllowPackageOverwrites(Feed feed)
    {
        return feed?.AllowPackageOverwrites ?? _options.Value.AllowPackageOverwrites;
    }

    public PackageDeletionBehavior GetPackageDeletionBehavior(Feed feed)
    {
        return feed?.PackageDeletionBehavior ?? _options.Value.PackageDeletionBehavior;
    }

    public bool GetIsReadOnlyMode(Feed feed)
    {
        return feed?.IsReadOnlyMode ?? _options.Value.IsReadOnlyMode;
    }

    public uint GetMaxPackageSizeGiB(Feed feed)
    {
        return feed?.MaxPackageSizeGiB ?? _options.Value.MaxPackageSizeGiB;
    }

    public RetentionOptions GetRetentionOptions(Feed feed)
    {
        var global = _options.Value.Retention ?? new RetentionOptions();

        if (feed == null)
            return global;

        return new RetentionOptions
        {
            MaxMajorVersions = feed.RetentionMaxMajorVersions.HasValue
                ? (uint?)feed.RetentionMaxMajorVersions.Value
                : global.MaxMajorVersions,
            MaxMinorVersions = feed.RetentionMaxMinorVersions.HasValue
                ? (uint?)feed.RetentionMaxMinorVersions.Value
                : global.MaxMinorVersions,
            MaxPatchVersions = feed.RetentionMaxPatchVersions.HasValue
                ? (uint?)feed.RetentionMaxPatchVersions.Value
                : global.MaxPatchVersions,
            MaxPrereleaseVersions = feed.RetentionMaxPrereleaseVersions.HasValue
                ? (uint?)feed.RetentionMaxPrereleaseVersions.Value
                : global.MaxPrereleaseVersions,
        };
    }

    public MirrorOptions GetMirrorOptions(Feed feed)
    {
        if (feed == null || !feed.MirrorEnabled)
            return new MirrorOptions { Enabled = false };

        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = string.IsNullOrEmpty(feed.MirrorPackageSource)
                ? null
                : new Uri(feed.MirrorPackageSource),
            Legacy = feed.MirrorLegacy,
            PackageDownloadTimeoutSeconds = feed.MirrorDownloadTimeoutSeconds ?? 600,
        };

        if (feed.MirrorAuthType.HasValue && feed.MirrorAuthType.Value != MirrorAuthenticationType.None)
        {
            options.Authentication = new MirrorAuthenticationOptions
            {
                Type = feed.MirrorAuthType.Value,
                Username = feed.MirrorAuthUsername,
                Password = feed.MirrorAuthPassword,
                Token = feed.MirrorAuthToken,
                CustomHeaders = DeserializeCustomHeaders(feed.MirrorAuthCustomHeaders),
            };
        }

        return options;
    }

    private static Dictionary<string, string> DeserializeCustomHeaders(string json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
