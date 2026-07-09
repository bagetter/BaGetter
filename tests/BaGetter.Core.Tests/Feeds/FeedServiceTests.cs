using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Core.Storage;
using BaGetter.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Feeds;

public class FeedServiceTests
{
    public class GetAllFeedsAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsDefaultFeedFirstThenAlphabeticalByName()
        {
            // Seeded out of order; "Default" would not sort first alphabetically (D > A),
            // so a correct result proves both the default-on-top and the alphabetical rules.
            AddFeed("zebra", "Zebra");
            AddFeed(Feed.DefaultSlug, "Default");
            AddFeed("apple", "Apple");
            await Context.SaveChangesAsync(Ct);

            var feeds = await Target.GetAllFeedsAsync(Ct);

            Assert.Equal(new[] { "Default", "Apple", "Zebra" }, feeds.Select(f => f.Name).ToArray());
        }
    }

    public class DeleteFeedAsync : FactsBase
    {
        [Fact]
        public async Task RemovesPackagesAndFeedAndCleansStorage()
        {
            var feed = AddFeed("toolfeed", "Tool Feed");
            AddPackage(feed.Id, "package.a", "1.0.0");
            AddPackage(feed.Id, "package.b", "2.1.0");
            await Context.SaveChangesAsync(Ct);

            var deleted = await Target.DeleteFeedAsync(feed.Id, Ct);

            Assert.True(deleted);
            Assert.False(await Context.Feeds.AnyAsync(f => f.Id == feed.Id, Ct));
            Assert.False(await Context.Packages.AnyAsync(p => p.FeedId == feed.Id, Ct));
            // Child rows must be gone too, or the package delete would hit a FK constraint.
            Assert.Empty(await Context.PackageDependencies.ToListAsync(Ct));
            Assert.Empty(await Context.TargetFrameworks.ToListAsync(Ct));

            PackageStorage.Verify(
                s => s.DeleteAsync("toolfeed", "package.a", It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
                Times.Once);
            PackageStorage.Verify(
                s => s.DeleteAsync("toolfeed", "package.b", It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SucceedsForFeedWithoutPackages()
        {
            var feed = AddFeed("emptyfeed", "Empty Feed");
            await Context.SaveChangesAsync(Ct);

            var deleted = await Target.DeleteFeedAsync(feed.Id, Ct);

            Assert.True(deleted);
            Assert.False(await Context.Feeds.AnyAsync(f => f.Id == feed.Id, Ct));
            PackageStorage.Verify(
                s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ReturnsFalseWhenFeedNotFound()
        {
            Assert.False(await Target.DeleteFeedAsync(Guid.NewGuid(), Ct));
        }

        [Fact]
        public async Task ThrowsWhenDeletingDefaultFeed()
        {
            var feed = AddFeed(Feed.DefaultSlug, "Default");
            await Context.SaveChangesAsync(Ct);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.DeleteFeedAsync(feed.Id, Ct));
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly Mock<IPackageStorageService> PackageStorage;
        protected readonly FeedService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;

        protected FactsBase()
        {
            Context = TestDbContext.Create();
            PackageStorage = new Mock<IPackageStorageService>();
            Target = new FeedService(Context, PackageStorage.Object, Mock.Of<ILogger<FeedService>>());
        }

        protected Feed AddFeed(string slug, string name)
        {
            var feed = new Feed
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                Name = name,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            Context.Feeds.Add(feed);
            return feed;
        }

        protected void AddPackage(Guid feedId, string id, string version)
        {
            // Include a dependency and a target framework so deleting the package exercises the
            // child-row cascade (PackageDependencies' FK is NoAction, so it must be removed too).
            Context.Packages.Add(new Package
            {
                FeedId = feedId,
                Id = id,
                Version = new NuGetVersion(version),
                Dependencies = new List<PackageDependency>
                {
                    new() { Id = "Some.Dependency", VersionRange = "1.0.0", TargetFramework = "net8.0" },
                },
                TargetFrameworks = new List<TargetFramework>
                {
                    new() { Moniker = "net8.0" },
                },
            });
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
