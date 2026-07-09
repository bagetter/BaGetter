using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Upstream.Clients;
using BaGetter.Protocol;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;

public class V3UpstreamClientTests
{
    [Fact]
    public void Ctor_NuGetClientIsNull_ShouldThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<V3UpstreamClient>>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new V3UpstreamClient(null, logger.Object));
    }

    [Fact]
    public void Ctor_LoggerIsNull_ShouldThrow()
    {
        // Arrange
        var nugetClient = new Mock<NuGetClient>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new V3UpstreamClient(nugetClient.Object, null));
    }

    public class ListPackageVersionsAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsEmpty()
        {
            Client
                .Setup(c => c.ListPackageVersionsAsync(
                    ID,
                    /*includeUnlisted: */ true,
                    Cancellation))
                .ReturnsAsync(new List<NuGetVersion>());

            var result = await Target.ListPackageVersionsAsync(ID, Cancellation);

            Assert.Empty(result);
        }

        [Fact]
        public async Task IgnoresExceptions()
        {
            Client
                .Setup(c => c.ListPackageVersionsAsync(
                    ID,
                    /*includeUnlisted: */ true,
                    Cancellation))
                .ThrowsAsync(new InvalidDataException("Hello"));

            var result = await Target.ListPackageVersionsAsync(ID, Cancellation);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsPackages()
        {
            Client
                .Setup(c => c.ListPackageVersionsAsync(
                    ID,
                    /*includeUnlisted: */ true,
                    Cancellation))
                .ReturnsAsync(new List<NuGetVersion> { Version });

            var result = await Target.ListPackageVersionsAsync(ID, Cancellation);

            var version = Assert.Single(result);
            Assert.Equal(Version, version);
        }
    }

    public class ListPackagesAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsEmpty()
        {
            Client
                .Setup(c => c.GetPackageMetadataAsync(ID, Cancellation))
                .ReturnsAsync(new List<PackageMetadata>());

            var result = await Target.ListPackagesAsync(ID, Cancellation);

            Assert.Empty(result);
        }

        [Fact]
        public async Task IgnoresExceptions()
        {
            Client
                .Setup(c => c.GetPackageMetadataAsync(ID, Cancellation))
                .ThrowsAsync(new InvalidDataException("Hello world"));

            var result = await Target.ListPackagesAsync(ID, Cancellation);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsPackages()
        {
            var published = DateTimeOffset.Now;

            Client
                .Setup(c => c.GetPackageMetadataAsync(ID, Cancellation))
                .ReturnsAsync(new List<PackageMetadata>
                {
                    new PackageMetadata
                    {
                        PackageId = "Foo",
                        Version = "1.2.3-prerelease+semver2",
                        Authors = "Author1, Author2",
                        Description = "Description",
                        IconUrl = "https://icon.test/",
                        Language = "Language",
                        LicenseUrl = "https://license.test/",
                        Listed = true,
                        MinClientVersion = "1.0.0",
                        PackageContentUrl = "https://content.test/",
                        Published = published,
                        RequireLicenseAcceptance = true,
                        Summary = "Summary",
                        Title = "Title",

                        Tags = new List<string> { "Tag1 Tag2" },

                        Deprecation = new PackageDeprecation
                        {
                            Reasons = new List<string> { "Reason1", "Reason2" },
                            Message = "Message",
                            AlternatePackage = new AlternatePackage
                            {
                                Id = "Alternate",
                                Range = "*",
                            },
                        },
                        DependencyGroups = new List<DependencyGroupItem>
                        {
                            new DependencyGroupItem
                            {
                                TargetFramework = "Target Framework",
                                Dependencies = new List<DependencyItem>
                                {
                                    new DependencyItem
                                    {
                                        Id = "Dependency",
                                        Range = "1.0.0",
                                    }
                                }
                            }
                        }
                    }
                });

            var result = await Target.ListPackagesAsync(ID, Cancellation);

            var package = Assert.Single(result);

            Assert.Equal("Foo", package.Id);
            Assert.Equal(new[] { "Author1", "Author2" }, package.Authors);
            Assert.Equal("Description", package.Description);
            Assert.False(package.HasReadme);
            Assert.False(package.HasEmbeddedIcon);
            Assert.True(package.IsPrerelease);
            Assert.Null(package.ReleaseNotes);
            Assert.Equal("Language", package.Language);
            Assert.True(package.Listed);
            Assert.Equal("1.0.0", package.MinClientVersion);
            Assert.Equal(published.UtcDateTime, package.Published);
            Assert.True(package.RequireLicenseAcceptance);
            Assert.Equal(SemVerLevel.SemVer2, package.SemVerLevel);
            Assert.Equal("Summary", package.Summary);
            Assert.Equal("Title", package.Title);
            Assert.Equal("https://icon.test/", package.IconUrlString);
            Assert.Equal("https://license.test/", package.LicenseUrlString);
            Assert.Equal("", package.ProjectUrlString);
            Assert.Equal("", package.RepositoryUrlString);
            Assert.Null(package.RepositoryType);
            Assert.Equal(new[] { "Tag1", "Tag2" }, package.Tags);
            Assert.Equal("1.2.3-prerelease", package.NormalizedVersionString);
            Assert.Equal("1.2.3-prerelease+semver2", package.OriginalVersionString);
        }
    }

    public class DownloadPackageOrNullAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNull()
        {
            Client
                .Setup(c => c.DownloadPackageAsync(ID, Version, Cancellation))
                .ThrowsAsync(new PackageNotFoundException(ID, Version));

            var result = await Target.DownloadPackageOrNullAsync(ID, Version, Cancellation);

            Assert.Null(result);
        }

        [Fact]
        public async Task IgnoresExceptions()
        {
            Client
                .Setup(c => c.DownloadPackageAsync(ID, Version, Cancellation))
                .ThrowsAsync(new InvalidDataException("Hello world"));

            var result = await Target.DownloadPackageOrNullAsync(ID, Version, Cancellation);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsPackage()
        {
            Client
                .Setup(c => c.DownloadPackageAsync(ID, Version, Cancellation))
                .ReturnsAsync(new MemoryStream());

            var result = await Target.DownloadPackageOrNullAsync(ID, Version, Cancellation);

            Assert.NotNull(result);
            Assert.True(result.CanSeek);
        }
    }

    public class FactsBase
    {
        protected readonly Mock<NuGetClient> Client;
        protected readonly V3UpstreamClient Target;

        protected readonly string ID = "Foo";
        protected readonly NuGetVersion Version = new NuGetVersion("1.2.3-prerelease+semver2");
        protected readonly CancellationToken Cancellation = CancellationToken.None;

        protected FactsBase()
        {
            Client = new Mock<NuGetClient>();
            Target = new V3UpstreamClient(
                Client.Object,
                Mock.Of<ILogger<V3UpstreamClient>>());
        }
    }
}
