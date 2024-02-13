namespace BaGetter.Core.Tests.Search;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BaGetter.Core;
using BaGetter.Core.Tests.Support;
using Moq;
using Xunit;

public class SearchResponseBuilderTests
{
    private readonly Mock<IUrlGenerator> _urlGenerator;
    private Mock<IUrlGenerator> _url;

    public SearchResponseBuilderTests()
    {
        _url = new Mock<IUrlGenerator>();
        _urlGenerator = new Mock<IUrlGenerator>();
    }

    #region helper methods

    private static PackageRegistration GetPackageRegistration()
    {
        //Bogus - System.SemVer

        var packageId = "BaGetter.Test";
        var packages = new List<Package>
        {
            Generator.GetPackage(packageId, "3.1.0"),
            Generator.GetPackage(packageId, "10.0.5"),
            Generator.GetPackage(packageId, "3.2.0"),
            Generator.GetPackage(packageId, "3.1.0-pre"),
            Generator.GetPackage(packageId, "1.0.0-beta1"),
            Generator.GetPackage(packageId, "1.0.0"),
        };

        return new PackageRegistration(packageId, packages);
    }

    #endregion

    [Fact]
    public void Ctor_UrlGeneratorIsNull_ShouldThrow()
    {
        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new SearchResponseBuilder(null));
    }

    [Fact]
    public void BuildAutocomplete_DataIsEmpty_ShouldReturnEmptyResponse()
    {
        // Arrange
        var data = ReadOnlyCollection<string>.Empty;
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildAutocomplete(data);

        // Assert
        Assert.Equal(0, result.TotalHits);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void BuildAutocomplete_DataHasData_ShouldReturnCorrectResponse()
    {
        // Arrange
        var data = new List<string>
        {
            "dummy",
            "another dummy"
        };
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildAutocomplete(data);

        // Assert
        Assert.Equal(data.Count, result.TotalHits);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public void BuildDependents_DataIsEmpty_ShouldReturnEmptyResponse()
    {
        // Arrange
        var data = ReadOnlyCollection<PackageDependent>.Empty;
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildDependents(data);

        // Assert
        Assert.Equal(0, result.TotalHits);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void BuildDependents_DataHasData_ShouldReturnCorrectResponse()
    {
        // Arrange
        var data = new List<PackageDependent>
        {
           new Mock<PackageDependent>().Object,
           new Mock<PackageDependent>().Object
        };
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildDependents(data);

        // Assert
        Assert.Equal(data.Count, result.TotalHits);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public void BuildSearch_DataIsEmpty_ShouldReturnEmptyResponse()
    {
        // Arrange
        var data = ReadOnlyCollection<PackageRegistration>.Empty;
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildSearch(data);

        // Assert
        Assert.Equal(0, result.TotalHits);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void BuildSearch_DataIsNotEmpty_ShouldReturnCorrectResponse()
    {
        // Arrange
        var packageRegistration = GetPackageRegistration();

        var data = new List<PackageRegistration>
        {
            packageRegistration
        };
        var searchResponseBuilder = new SearchResponseBuilder(_urlGenerator.Object);

        // Act
        var result = searchResponseBuilder.BuildSearch(data);

        // Assert
        Assert.Equal(data.Count, result.TotalHits);
    }
}
