using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using BaGetter.Core.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Services;

public class PackageStorageServiceTests
{
    public class SavePackageContentAsync : FactsBase
    {
        [Fact]
        public async Task ThrowsIfPackageIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => Target.SavePackageContentAsync(
                    "default",
                    null,
                    packageStream: Stream.Null,
                    nuspecStream: Stream.Null,
                    readmeStream: Stream.Null,
                    iconStream: Stream.Null));
        }

        [Fact]
        public async Task ThrowsIfPackageStreamIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => Target.SavePackageContentAsync(
                    "default",
                    Package,
                    packageStream: null,
                    nuspecStream: Stream.Null,
                    readmeStream: Stream.Null,
                    iconStream: Stream.Null));
        }

        [Fact]
        public async Task ThrowsIfNuspecStreamIsNull()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => Target.SavePackageContentAsync(
                    "default",
                    Package,
                    packageStream: Stream.Null,
                    nuspecStream: null,
                    readmeStream: Stream.Null,
                    iconStream: Stream.Null));
        }

        [Fact]
        public async Task SavesContent()
        {
            // Arrange
            SetupPutResult(StoragePutResult.Success);

            using var packageStream = StringStream("My package");
            using var nuspecStream = StringStream("My nuspec");
            using var readmeStream = StringStream("My readme");
            using var iconStream = StringStream("My icon");
            // Act
            await Target.SavePackageContentAsync(
                "default",
                Package,
                packageStream: packageStream,
                nuspecStream: nuspecStream,
                readmeStream: readmeStream,
                iconStream: iconStream);

            // Assert
            Assert.True(Puts.ContainsKey(PackagePath));
            Assert.Equal("My package", await ToStringAsync(Puts[PackagePath].Content));
            Assert.Equal("binary/octet-stream", Puts[PackagePath].ContentType);

            Assert.True(Puts.ContainsKey(NuspecPath));
            Assert.Equal("My nuspec", await ToStringAsync(Puts[NuspecPath].Content));
            Assert.Equal("text/plain", Puts[NuspecPath].ContentType);

            Assert.True(Puts.ContainsKey(ReadmePath));
            Assert.Equal("My readme", await ToStringAsync(Puts[ReadmePath].Content));
            Assert.Equal("text/markdown", Puts[ReadmePath].ContentType);

            Assert.True(Puts.ContainsKey(IconPath));
            Assert.Equal("My icon", await ToStringAsync(Puts[IconPath].Content));
            Assert.Equal("image/xyz", Puts[IconPath].ContentType);
        }

        [Fact]
        public async Task DoesNotSaveReadmeIfItIsNull()
        {
            // Arrange
            SetupPutResult(StoragePutResult.Success);

            using (var packageStream = StringStream("My package"))
            using (var nuspecStream = StringStream("My nuspec"))
            {
                // Act
                await Target.SavePackageContentAsync(
                    "default",
                    Package,
                    packageStream: packageStream,
                    nuspecStream: nuspecStream,
                    readmeStream: null,
                    iconStream: null);
            }

            // Assert
            Assert.False(Puts.ContainsKey(ReadmePath));
        }

        [Fact]
        public async Task NormalizesVersionWhenContentIsSaved()
        {
            // Arrange
            SetupPutResult(StoragePutResult.Success);

            Package.Version = new NuGetVersion("1.2.3.0");
            using (var packageStream = StringStream("My package"))
            using (var nuspecStream = StringStream("My nuspec"))
            using (var readmeStream = StringStream("My readme"))
            using (var iconStream = StringStream("My icon"))
            {
                // Act
                await Target.SavePackageContentAsync(
                    "default",
                    Package,
                    packageStream: packageStream,
                    nuspecStream: nuspecStream,
                    readmeStream: readmeStream,
                    iconStream: iconStream);
            }

            // Assert
            Assert.True(Puts.ContainsKey(PackagePath));
            Assert.True(Puts.ContainsKey(NuspecPath));
            Assert.True(Puts.ContainsKey(ReadmePath));
        }

        [Fact]
        public async Task DoesNotThrowIfContentAlreadyExistsAndContentsMatch()
        {
            // Arrange
            SetupPutResult(StoragePutResult.AlreadyExists);

            using var packageStream = StringStream("My package");
            using var nuspecStream = StringStream("My nuspec");
            using var readmeStream = StringStream("My readme");
            using var iconStream = StringStream("My icon");
            await Target.SavePackageContentAsync(
                "default",
                Package,
                packageStream: packageStream,
                nuspecStream: nuspecStream,
                readmeStream: readmeStream,
                iconStream: iconStream);

            // Assert
            Assert.True(Puts.ContainsKey(PackagePath));
            Assert.Equal("My package", await ToStringAsync(Puts[PackagePath].Content));
            Assert.Equal("binary/octet-stream", Puts[PackagePath].ContentType);

            Assert.True(Puts.ContainsKey(NuspecPath));
            Assert.Equal("My nuspec", await ToStringAsync(Puts[NuspecPath].Content));
            Assert.Equal("text/plain", Puts[NuspecPath].ContentType);

            Assert.True(Puts.ContainsKey(ReadmePath));
            Assert.Equal("My readme", await ToStringAsync(Puts[ReadmePath].Content));
            Assert.Equal("text/markdown", Puts[ReadmePath].ContentType);

            Assert.True(Puts.ContainsKey(IconPath));
            Assert.Equal("My icon", await ToStringAsync(Puts[IconPath].Content));
            Assert.Equal("image/xyz", Puts[IconPath].ContentType);
        }

        [Fact]
        public async Task ThrowsIfContentAlreadyExistsButContentsDoNotMatch()
        {
            // Arrange
            SetupPutResult(StoragePutResult.Conflict);

            using var packageStream = StringStream("My package");
            using var nuspecStream = StringStream("My nuspec");
            using var readmeStream = StringStream("My readme");
            using var iconStream = StringStream("My icon");
            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Target.SavePackageContentAsync(
                    "default",
                    Package,
                    packageStream: packageStream,
                    nuspecStream: nuspecStream,
                    readmeStream: readmeStream,
                    iconStream: iconStream));
        }
    }

    public class GetPackageStreamAsync : FactsBase
    {
        [Fact]
        public async Task ThrowsIfStorageThrows()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            Storage
                .Setup(s => s.GetAsync(PackagePath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());
            // The default-feed legacy fallback also tries the non-slug path; set it up to throw too.
            Storage
                .Setup(s => s.GetAsync(LegacyPackagePath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());

            // Act
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                Target.GetPackageStreamAsync("default", Package.Id, Package.Version, cancellationToken));
        }

        [Fact]
        public async Task GetsStream()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            using var packageStream = StringStream("My package");
            Storage
                .Setup(s => s.GetAsync(PackagePath, cancellationToken))
                .ReturnsAsync(packageStream);

            // Act
            var result = await Target.GetPackageStreamAsync("default", Package.Id, Package.Version, cancellationToken);

            // Assert
            Assert.Equal("My package", await ToStringAsync(result));

            Storage.Verify(s => s.GetAsync(PackagePath, cancellationToken), Times.Once);
        }
    }

    public class GetNuspecStreamAsync : FactsBase
    {
        [Fact]
        public async Task ThrowsIfDoesntExist()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            Storage
                .Setup(s => s.GetAsync(NuspecPath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());
            // The default-feed legacy fallback also tries the non-slug path; set it up to throw too.
            Storage
                .Setup(s => s.GetAsync(LegacyNuspecPath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());

            // Act
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                Target.GetNuspecStreamAsync("default", Package.Id, Package.Version, cancellationToken));
        }

        [Fact]
        public async Task GetsStream()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            using var nuspecStream = StringStream("My nuspec");
            Storage
                .Setup(s => s.GetAsync(NuspecPath, cancellationToken))
                .ReturnsAsync(nuspecStream);

            // Act
            var result = await Target.GetNuspecStreamAsync("default", Package.Id, Package.Version, cancellationToken);

            // Assert
            Assert.Equal("My nuspec", await ToStringAsync(result));

            Storage.Verify(s => s.GetAsync(NuspecPath, cancellationToken), Times.Once);
        }
    }

    public class GetReadmeStreamAsync : FactsBase
    {
        [Fact]
        public async Task ThrowsIfDoesntExist()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            Storage
                .Setup(s => s.GetAsync(ReadmePath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());
            // The default-feed legacy fallback also tries the non-slug path; set it up to throw too.
            Storage
                .Setup(s => s.GetAsync(LegacyReadmePath, cancellationToken))
                .ThrowsAsync(new DirectoryNotFoundException());

            // Act
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
                Target.GetReadmeStreamAsync("default", Package.Id, Package.Version, cancellationToken));
        }

        [Fact]
        public async Task GetsStream()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            using var readmeStream = StringStream("My readme");
            Storage
                .Setup(s => s.GetAsync(ReadmePath, cancellationToken))
                .ReturnsAsync(readmeStream);

            // Act
            var result = await Target.GetReadmeStreamAsync("default", Package.Id, Package.Version, cancellationToken);

            // Assert
            Assert.Equal("My readme", await ToStringAsync(result));

            Storage.Verify(s => s.GetAsync(ReadmePath, cancellationToken), Times.Once);
        }
    }

    public class DeleteAsync : FactsBase
    {
        [Fact]
        public async Task Deletes()
        {
            // Act
            var cancellationToken = CancellationToken.None;
            await Target.DeleteAsync("default", Package.Id, Package.Version, cancellationToken);

            Storage.Verify(s => s.DeleteAsync(PackagePath, cancellationToken), Times.Once);
            Storage.Verify(s => s.DeleteAsync(NuspecPath, cancellationToken), Times.Once);
            Storage.Verify(s => s.DeleteAsync(ReadmePath, cancellationToken), Times.Once);
        }
    }

    public class FactsBase
    {
        protected readonly Package Package = new Package
        {
            Id = "My.Package",
            Version = new NuGetVersion("1.2.3")
        };

        protected readonly Mock<IStorageService> Storage;
        protected readonly PackageStorageService Target;

        protected readonly Dictionary<string, (Stream Content, string ContentType)> Puts;

        public FactsBase()
        {
            Storage = new Mock<IStorageService>();
            Target = new PackageStorageService(Storage.Object, Mock.Of<ILogger<PackageStorageService>>());

            Puts = new Dictionary<string, (Stream Content, string ContentType)>();
        }

        protected string PackagePath => Path.Combine("packages", "default", "my.package", "1.2.3", "my.package.1.2.3.nupkg");
        protected string NuspecPath => Path.Combine("packages", "default", "my.package", "1.2.3", "my.package.nuspec");
        protected string ReadmePath => Path.Combine("packages", "default", "my.package", "1.2.3", "readme");
        protected string IconPath => Path.Combine("packages", "default", "my.package", "1.2.3", "icon");

        // Legacy paths (no feed-slug prefix) — used by the default-feed legacy fallback.
        protected string LegacyPackagePath => Path.Combine("packages", "my.package", "1.2.3", "my.package.1.2.3.nupkg");
        protected string LegacyNuspecPath => Path.Combine("packages", "my.package", "1.2.3", "my.package.nuspec");
        protected string LegacyReadmePath => Path.Combine("packages", "my.package", "1.2.3", "readme");

        protected Stream StringStream(string input)
        {
            var bytes = Encoding.ASCII.GetBytes(input);

            return new MemoryStream(bytes);
        }

        protected async Task<string> ToStringAsync(Stream input)
        {
            using var reader = new StreamReader(input);
            return await reader.ReadToEndAsync();
        }

        protected void SetupPutResult(StoragePutResult result)
        {
            Storage
                .Setup(
                    s => s.PutAsync(
                        It.IsAny<string>(),
                        It.IsAny<Stream>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .Callback((string path, Stream content, string contentType, CancellationToken cancellationToken) =>
                {
                    Puts[path] = (content, contentType);
                })
                .ReturnsAsync(result);
        }
    }
}
