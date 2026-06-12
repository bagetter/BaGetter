using System;
using System.Collections.Generic;
using System.Linq;
using BaGetter.Core.Tests.Support;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Metadata;

public class RegistrationBuilderTests
{
    private readonly Mock<IUrlGenerator> _urlGenerator;
    private readonly Mock<INuGetUrlRewriter> _rewriter;
    private readonly RegistrationBuilder _registrationBuilder;

    public RegistrationBuilderTests()
    {
        _urlGenerator = new Mock<IUrlGenerator>();
        _rewriter = new Mock<INuGetUrlRewriter>();
        _rewriter.Setup(r => r.RewriteAssetUrl(It.IsAny<string>())).Returns((string s) => s);
        _registrationBuilder = new RegistrationBuilder(_urlGenerator.Object, _rewriter.Object);
    }

    #region helper methods

    private PackageRegistration GetPackageRegistration()
    {
        var packageId = "BaGetter.Test";
        var packages = new List<Package>
        {
            Generator.GetPackage(packageId, "3.1.0"),
            Generator.GetPackage(packageId, "10.0.5", downloads:42),
            Generator.GetPackage(packageId, "3.2.0"),
            Generator.GetPackage(packageId, "3.1.0-pre"),
            Generator.GetPackage(packageId, "1.0.0-beta1", downloads:21),
            Generator.GetPackage(packageId, "1.0.0"),
        };

        return new PackageRegistration(packageId, packages);
    }

    #endregion

    [Fact]
    public void Ctor_UrlGeneratorIsNull_ShouldThrow()
    {
        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new RegistrationBuilder(null, _rewriter.Object));
    }

    [Fact]
    public void BuildIndex_PackageRegistrationIsNull_ShouldThrow()
    {
        // Arrange
        var registrationBuilder = new RegistrationBuilder(_urlGenerator.Object, _rewriter.Object);

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => registrationBuilder.BuildIndex(null));
    }

    [Fact]
    public void BuildIndex_RegistrationIndexResponse_ShouldBeSortedByVersion()
    {
        // Arrange
        var registration = GetPackageRegistration();

        // Act
        var response = _registrationBuilder.BuildIndex(registration);

        // Assert
        Assert.Equal(registration.Packages.Count, response.Pages[0].ItemsOrNull.Count);

        var index = 0;
        foreach (var package in registration.Packages.OrderBy(p => p.Version))
        {
            Assert.Equal(package.Version.ToFullString(), response.Pages[0].ItemsOrNull[index++].PackageMetadata.Version);
        }
    }

    [Fact]
    public void BuildIndex_RegistrationIndexResponse_ShouldHaveCorrectTotalDownloads()
    {
        // Arrange
        var registration = GetPackageRegistration();

        // Act
        var response = _registrationBuilder.BuildIndex(registration);

        // Assert
        Assert.Equal(registration.Packages.Sum(x => x.Downloads), response.TotalDownloads);
    }

    [Fact]
    public void BuildLeaf_RegistrationLeafResponse_ShouldHaveCorrectProperties()
    {
        // Arrange
        var packageId = "dummy";
        var packageVersion = "1.0.42";
        var isPackageListed = true;
        var publishDate = DateTime.UtcNow;

        var package = Generator.GetPackage(packageId, packageVersion);
        package.Listed = isPackageListed;
        package.Published = publishDate;

        // Act
        var response = _registrationBuilder.BuildLeaf(package);

        // Assert
        Assert.Equal(isPackageListed, response.Listed);
        Assert.Equal(publishDate, response.Published);
    }

    [Fact]
    public void BuildIndex_RewritesUpstreamIconAndLicenseUrls()
    {
        // Arrange
        var package = Generator.GetPackage("Example.Test", "1.0.0");
        package.HasEmbeddedIcon = false;
        package.IconUrl = new Uri("https://api.example.com/example.test/1.0.0/icon");
        package.LicenseUrl = new Uri("https://api.example.com/example.test/1.0.0/license");

        _rewriter.Setup(r => r.RewriteAssetUrl("https://api.example.com/example.test/1.0.0/icon"))
            .Returns("http://localhost/v3/proxy/asset?url=icon");
        _rewriter.Setup(r => r.RewriteAssetUrl("https://api.example.com/example.test/1.0.0/license"))
            .Returns("http://localhost/v3/proxy/asset?url=license");

        var registration = new PackageRegistration("Example.Test", new List<Package> { package });

        // Act
        var response = _registrationBuilder.BuildIndex(registration);

        // Assert
        var metadata = response.Pages[0].ItemsOrNull[0].PackageMetadata;
        Assert.Equal("http://localhost/v3/proxy/asset?url=icon", metadata.IconUrl);
        Assert.Equal("http://localhost/v3/proxy/asset?url=license", metadata.LicenseUrl);
    }
}
