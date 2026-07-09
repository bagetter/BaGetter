using System;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;

namespace BaGetter.Web.Models;

/// <summary>
/// Safe projection of a <see cref="Feed"/> for API responses.
/// Omits secret fields (password, token, custom headers) and replaces
/// them with boolean presence indicators.
/// </summary>
public class FeedResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public PackageOverwriteAllowed? AllowPackageOverwrites { get; set; }
    public PackageDeletionBehavior? PackageDeletionBehavior { get; set; }
    public bool? IsReadOnlyMode { get; set; }
    public uint? MaxPackageSizeGiB { get; set; }
    public int? RetentionMaxMajorVersions { get; set; }
    public int? RetentionMaxMinorVersions { get; set; }
    public int? RetentionMaxPatchVersions { get; set; }
    public int? RetentionMaxPrereleaseVersions { get; set; }

    public bool MirrorEnabled { get; set; }
    public string MirrorPackageSource { get; set; }
    public bool MirrorLegacy { get; set; }
    public int? MirrorDownloadTimeoutSeconds { get; set; }
    public MirrorAuthenticationType? MirrorAuthType { get; set; }
    public string MirrorAuthUsername { get; set; }

    /// <summary>True if a mirror password is configured; the value is never returned.</summary>
    public bool HasMirrorAuthPassword { get; set; }

    /// <summary>True if a mirror bearer token is configured; the value is never returned.</summary>
    public bool HasMirrorAuthToken { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public static FeedResponse FromFeed(Feed feed) => new FeedResponse
    {
        Id = feed.Id,
        Slug = feed.Slug,
        Name = feed.Name,
        Description = feed.Description,
        AllowPackageOverwrites = feed.AllowPackageOverwrites,
        PackageDeletionBehavior = feed.PackageDeletionBehavior,
        IsReadOnlyMode = feed.IsReadOnlyMode,
        MaxPackageSizeGiB = feed.MaxPackageSizeGiB,
        RetentionMaxMajorVersions = feed.RetentionMaxMajorVersions,
        RetentionMaxMinorVersions = feed.RetentionMaxMinorVersions,
        RetentionMaxPatchVersions = feed.RetentionMaxPatchVersions,
        RetentionMaxPrereleaseVersions = feed.RetentionMaxPrereleaseVersions,
        MirrorEnabled = feed.MirrorEnabled,
        MirrorPackageSource = feed.MirrorPackageSource,
        MirrorLegacy = feed.MirrorLegacy,
        MirrorDownloadTimeoutSeconds = feed.MirrorDownloadTimeoutSeconds,
        MirrorAuthType = feed.MirrorAuthType,
        MirrorAuthUsername = feed.MirrorAuthUsername,
        HasMirrorAuthPassword = !string.IsNullOrEmpty(feed.MirrorAuthPassword),
        HasMirrorAuthToken = !string.IsNullOrEmpty(feed.MirrorAuthToken),
        CreatedAtUtc = feed.CreatedAtUtc,
        UpdatedAtUtc = feed.UpdatedAtUtc,
    };
}
