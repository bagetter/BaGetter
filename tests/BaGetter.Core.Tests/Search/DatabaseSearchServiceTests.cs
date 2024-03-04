using System;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Search;

public class DatabaseSearchServiceTests
{
    [Fact]
    public void Ctor_ContextIsNull_ShouldThrow()
    {
        // Arrange
        var frameworkCompatibilityService = new Mock<IFrameworkCompatibilityService>();
        var searchResponseBuilder = new Mock<ISearchResponseBuilder>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DatabaseSearchService(null, frameworkCompatibilityService.Object, searchResponseBuilder.Object));
    }

    [Fact]
    public void Ctor_FrameworkCompatibilityServiceIsNull_ShouldThrow()
    {
        // Arrange
        var context = new Mock<IContext>();
        var searchResponseBuilder = new Mock<ISearchResponseBuilder>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DatabaseSearchService(context.Object, null, searchResponseBuilder.Object));
    }

    [Fact]
    public void Ctor_SearchResponseBuilderIsNull_ShouldThrow()
    {
        // Arrange
        var context = new Mock<IContext>();
        var frameworkCompatibilityService = new Mock<IFrameworkCompatibilityService>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DatabaseSearchService(context.Object, frameworkCompatibilityService.Object, null));
    }
}
