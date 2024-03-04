using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;

public class PackageDownloadsJsonSourceTests
{
    [Fact]
    public void Ctor_HttpClientIsNull_ShouldThrow()
    {
        // Arrange
        var logger = new Mock<ILogger<PackageDownloadsJsonSource>>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new PackageDownloadsJsonSource(null, logger.Object));
    }

    [Fact]
    public void Ctor_LoggerIsNull_ShouldThrow()
    {
        // Arrange
        var httpClient = new Mock<HttpClient>();

        // Act/Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new PackageDownloadsJsonSource(httpClient.Object, null));
    }
}
