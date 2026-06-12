using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Metadata;

public class DefaultPackageMetadataServiceTests
{
    private readonly Mock<IUrlGenerator> _urlGenerator;
    private readonly Mock<INuGetUrlRewriter> _rewriter;
    private readonly Mock<IUpstreamHostProvider> _upstreamHosts;
    private readonly RegistrationBuilder _registrationBuilder;

    public DefaultPackageMetadataServiceTests()
    {
        _urlGenerator = new Mock<IUrlGenerator>();
        _rewriter = new Mock<INuGetUrlRewriter>();
        _rewriter.Setup(r => r.RewriteAssetUrl(It.IsAny<string>())).Returns((string s) => s);
        _upstreamHosts = new Mock<IUpstreamHostProvider>();
        _upstreamHosts.Setup(h => h.EnsureResolvedAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _registrationBuilder = new RegistrationBuilder(_urlGenerator.Object, _rewriter.Object);
    }

    [Fact]
    public void Ctor_PackageServiceIsNull_ShouldThrow()
    {
        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DefaultPackageMetadataService(null, _registrationBuilder, _upstreamHosts.Object));
    }

    [Fact]
    public void Ctor_RegistrationBuilderIsNull_ShouldThrow()
    {
        // Arrange
        var packageService = new Mock<IPackageService>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DefaultPackageMetadataService(packageService.Object, null, _upstreamHosts.Object));
    }

    [Fact]
    public async Task GetRegistrationIndexOrNullAsync_PackageFindsNoPackages_ShouldReturnNullAsync()
    {
        // Arrange
        var packageService = new Mock<IPackageService>();
        packageService.Setup(x => x.FindPackagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IReadOnlyList<Package>>(new List<Package>()));

        var packageMetadataService = new DefaultPackageMetadataService(packageService.Object, _registrationBuilder, _upstreamHosts.Object);

        // Act
        var result = await packageMetadataService.GetRegistrationIndexOrNullAsync("dummy");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRegistrationLeafOrNullAsync_PackageFindsNoPackages_ShouldReturnNullAsync()
    {
        // Arrange
        var nugetVersion = new NuGetVersion("0.0.42");

        var packageService = new Mock<IPackageService>();
        packageService.Setup(x => x.FindPackageOrNullAsync(It.IsAny<string>(), It.IsAny<NuGetVersion>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Package>(null));

        var packageMetadataService = new DefaultPackageMetadataService(packageService.Object, _registrationBuilder, _upstreamHosts.Object);

        // Act
        var result = await packageMetadataService.GetRegistrationLeafOrNullAsync("dummy", nugetVersion);

        // Assert
        Assert.Null(result);
    }
}
