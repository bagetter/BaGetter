using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;

public class DownloadsImporterTests
{
    [Fact]
    public void Ctor_ContextIsNull_ShouldThrow()
    {
        // Arrange
        var packageDownloadsSource = new Mock<IPackageDownloadsSource>();
        var logger = new Mock<ILogger<DownloadsImporter>>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DownloadsImporter(null, packageDownloadsSource.Object, logger.Object));
    }

    [Fact]
    public void Ctor_PackageDownloadsSourceIsNull_ShouldThrow()
    {
        // Arrange
        var context = new Mock<IContext>();
        var logger = new Mock<ILogger<DownloadsImporter>>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DownloadsImporter(context.Object, null, logger.Object));
    }

    [Fact]
    public void Ctor_LoggerIsNull_ShouldThrow()
    {
        // Arrange
        var context = new Mock<IContext>();
        var packageDownloadsSource = new Mock<IPackageDownloadsSource>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new DownloadsImporter(context.Object, packageDownloadsSource.Object, null));
    }
}
