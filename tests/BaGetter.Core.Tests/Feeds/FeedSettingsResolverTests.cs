using System;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Feeds;

public class FeedSettingsResolverTests
{
    private readonly BaGetterOptions _globalOptions;
    private readonly FeedSettingsResolver _target;

    public FeedSettingsResolverTests()
    {
        _globalOptions = new BaGetterOptions
        {
            AllowPackageOverwrites = PackageOverwriteAllowed.False,
            PackageDeletionBehavior = PackageDeletionBehavior.Unlist,
            IsReadOnlyMode = false,
            MaxPackageSizeGiB = 8,
            Retention = new RetentionOptions
            {
                MaxMajorVersions = 5,
                MaxMinorVersions = null,
                MaxPatchVersions = null,
                MaxPrereleaseVersions = null,
            },
        };

        var snapshot = new Mock<IOptionsSnapshot<BaGetterOptions>>();
        snapshot.Setup(s => s.Value).Returns(() => _globalOptions);

        _target = new FeedSettingsResolver(snapshot.Object);
    }

    private static Feed DefaultFeed() => new Feed
    {
        Id = Guid.Empty,
        Slug = Feed.DefaultSlug,
        Name = "Default",
    };

    public class GetAllowPackageOverwrites : FeedSettingsResolverTests
    {
        [Fact]
        public void ReturnsGlobalWhenFeedHasNoOverride()
        {
            _globalOptions.AllowPackageOverwrites = PackageOverwriteAllowed.False;
            var feed = DefaultFeed();

            var result = _target.GetAllowPackageOverwrites(feed);

            Assert.Equal(PackageOverwriteAllowed.False, result);
        }

        [Fact]
        public void ReturnsFeedOverrideWhenSet()
        {
            _globalOptions.AllowPackageOverwrites = PackageOverwriteAllowed.False;
            var feed = DefaultFeed();
            feed.AllowPackageOverwrites = PackageOverwriteAllowed.True;

            var result = _target.GetAllowPackageOverwrites(feed);

            Assert.Equal(PackageOverwriteAllowed.True, result);
        }

        [Fact]
        public void FeedOverrideWinsOverGlobal()
        {
            _globalOptions.AllowPackageOverwrites = PackageOverwriteAllowed.True;
            var feed = DefaultFeed();
            feed.AllowPackageOverwrites = PackageOverwriteAllowed.False;

            var result = _target.GetAllowPackageOverwrites(feed);

            Assert.Equal(PackageOverwriteAllowed.False, result);
        }

        [Fact]
        public void ReturnsGlobalWhenFeedIsNull()
        {
            _globalOptions.AllowPackageOverwrites = PackageOverwriteAllowed.PrereleaseOnly;

            var result = _target.GetAllowPackageOverwrites(null);

            Assert.Equal(PackageOverwriteAllowed.PrereleaseOnly, result);
        }
    }

    public class GetPackageDeletionBehavior : FeedSettingsResolverTests
    {
        [Fact]
        public void ReturnsGlobalWhenFeedHasNoOverride()
        {
            _globalOptions.PackageDeletionBehavior = PackageDeletionBehavior.Unlist;
            var feed = DefaultFeed();

            var result = _target.GetPackageDeletionBehavior(feed);

            Assert.Equal(PackageDeletionBehavior.Unlist, result);
        }

        [Fact]
        public void ReturnsFeedOverride()
        {
            _globalOptions.PackageDeletionBehavior = PackageDeletionBehavior.Unlist;
            var feed = DefaultFeed();
            feed.PackageDeletionBehavior = PackageDeletionBehavior.HardDelete;

            var result = _target.GetPackageDeletionBehavior(feed);

            Assert.Equal(PackageDeletionBehavior.HardDelete, result);
        }
    }

    public class GetIsReadOnlyMode : FeedSettingsResolverTests
    {
        [Fact]
        public void ReturnsGlobalWhenFeedHasNoOverride()
        {
            _globalOptions.IsReadOnlyMode = true;
            var feed = DefaultFeed();

            var result = _target.GetIsReadOnlyMode(feed);

            Assert.True(result);
        }

        [Fact]
        public void ReturnsFeedOverride()
        {
            _globalOptions.IsReadOnlyMode = false;
            var feed = DefaultFeed();
            feed.IsReadOnlyMode = true;

            var result = _target.GetIsReadOnlyMode(feed);

            Assert.True(result);
        }
    }

    public class GetRetentionOptions : FeedSettingsResolverTests
    {
        [Fact]
        public void ReturnsGlobalWhenFeedHasNoOverrides()
        {
            _globalOptions.Retention = new RetentionOptions { MaxMajorVersions = 10 };
            var feed = DefaultFeed();

            var result = _target.GetRetentionOptions(feed);

            Assert.Equal((uint?)10, result.MaxMajorVersions);
        }

        [Fact]
        public void FeedOverrideReplacesGlobalForOverriddenFields()
        {
            _globalOptions.Retention = new RetentionOptions
            {
                MaxMajorVersions = 10,
                MaxMinorVersions = 5,
            };
            var feed = DefaultFeed();
            feed.RetentionMaxMajorVersions = 3;

            var result = _target.GetRetentionOptions(feed);

            Assert.Equal((uint?)3, result.MaxMajorVersions);
            Assert.Equal((uint?)5, result.MaxMinorVersions);
        }

        [Fact]
        public void ReturnsDefaultsWhenNoGlobalOrFeedRetention()
        {
            _globalOptions.Retention = null;
            var feed = DefaultFeed();

            var result = _target.GetRetentionOptions(feed);

            Assert.Null(result.MaxMajorVersions);
        }
    }

    public class GetMirrorOptions : FeedSettingsResolverTests
    {
        [Fact]
        public void ReturnsDisabledWhenFeedMirrorDisabled()
        {
            var feed = DefaultFeed();
            feed.MirrorEnabled = false;

            var result = _target.GetMirrorOptions(feed);

            Assert.False(result.Enabled);
        }

        [Fact]
        public void ReturnsDisabledWhenFeedIsNull()
        {
            var result = _target.GetMirrorOptions(null);

            Assert.False(result.Enabled);
        }

        [Fact]
        public void ReturnsFeedMirrorSettings()
        {
            var feed = DefaultFeed();
            feed.MirrorEnabled = true;
            feed.MirrorPackageSource = "https://api.nuget.org/v3/index.json";
            feed.MirrorLegacy = false;
            feed.MirrorDownloadTimeoutSeconds = 300;

            var result = _target.GetMirrorOptions(feed);

            Assert.True(result.Enabled);
            Assert.Equal(new Uri("https://api.nuget.org/v3/index.json"), result.PackageSource);
            Assert.False(result.Legacy);
            Assert.Equal(300, result.PackageDownloadTimeoutSeconds);
        }

        [Fact]
        public void ReturnsFeedMirrorWithBasicAuth()
        {
            var feed = DefaultFeed();
            feed.MirrorEnabled = true;
            feed.MirrorPackageSource = "https://example.com/v3/index.json";
            feed.MirrorAuthType = MirrorAuthenticationType.Basic;
            feed.MirrorAuthUsername = "user";
            feed.MirrorAuthPassword = "pass";

            var result = _target.GetMirrorOptions(feed);

            Assert.True(result.Enabled);
            Assert.NotNull(result.Authentication);
            Assert.Equal(MirrorAuthenticationType.Basic, result.Authentication.Type);
            Assert.Equal("user", result.Authentication.Username);
            Assert.Equal("pass", result.Authentication.Password);
        }
    }
}
