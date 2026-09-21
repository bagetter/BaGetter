using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Web.Admin;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Web.Tests.Admin;

public sealed class AdminPackageServiceFacts
{
    private readonly Mock<IPackageDatabase> _packages = new();
    private readonly Mock<IPackageStorageService> _storage = new();
    private readonly Mock<IPackageIndexingService> _indexer = new();
    private readonly Mock<ISearchIndexer> _searchIndexer = new();

    [Fact]
    public async Task SetListedAsync_UnlistsAndRefreshesSearch()
    {
        var package = CreatePackage();
        _packages
            .Setup(p => p.FindOrNullAsync(package.Id, package.Version, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        _packages
            .Setup(p => p.UnlistPackageAsync(package.Id, package.Version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().SetListedAsync(
            package.Id, package.Version, listed: false, CancellationToken.None);

        Assert.True(result.Succeeded);
        _searchIndexer.Verify(
            indexer => indexer.IndexAsync(package, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesMetadataContentAndRefreshesSearch()
    {
        var package = CreatePackage();
        _packages
            .Setup(p => p.FindOrNullAsync(package.Id, package.Version, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        _packages
            .Setup(p => p.HardDeletePackageAsync(package.Id, package.Version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().HardDeleteAsync(
            package.Id, package.Version, CancellationToken.None);

        Assert.True(result.Succeeded);
        _storage.Verify(
            storage => storage.DeleteAsync(package.Id, package.Version, It.IsAny<CancellationToken>()),
            Times.Once);
        _searchIndexer.Verify(
            indexer => indexer.IndexAsync(package, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CopyAsync_RejectsSignedPackage()
    {
        var package = CreatePackage();
        _packages
            .Setup(p => p.FindOrNullAsync(package.Id, package.Version, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(package);
        _storage
            .Setup(s => s.GetPackageStreamAsync(package.Id, package.Version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePackageArchive(signed: true));

        var result = await CreateService().CopyAsync(
            package.Id, package.Version, "Copied.Package", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("Signed packages", result.Message);
        _indexer.Verify(
            indexer => indexer.IndexAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private AdminPackageService CreateService()
    {
        return new AdminPackageService(
            _packages.Object,
            _storage.Object,
            _indexer.Object,
            _searchIndexer.Object);
    }

    private static Package CreatePackage()
    {
        return new Package
        {
            Id = "Source.Package",
            Version = NuGetVersion.Parse("1.2.3"),
            Listed = true,
        };
    }

    private static Stream CreatePackageArchive(bool signed)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var nuspec = archive.CreateEntry("Source.Package.nuspec");
            using (var writer = new StreamWriter(nuspec.Open(), Encoding.UTF8, leaveOpen: false))
            {
                writer.Write(
                    "<?xml version=\"1.0\"?><package><metadata><id>Source.Package</id><version>1.2.3</version></metadata></package>");
            }

            if (signed)
            {
                archive.CreateEntry(".signature.p7s");
            }
        }

        stream.Position = 0;
        return stream;
    }
}
