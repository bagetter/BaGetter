using System.Threading.Tasks;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;

public class DisabledUpstreamClientTests
{
    private readonly DisabledUpstreamClient _disabledUpstreamClient;

    public DisabledUpstreamClientTests()
    {
        _disabledUpstreamClient = new DisabledUpstreamClient();
    }

    [Fact]
    public async Task ListPackageVersionsAsync_IsCalled_ShouldReturnEmptyListAsync()
    {
        // Act
        var result = await _disabledUpstreamClient.ListPackageVersionsAsync("dummy", default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListPackagesAsync_IsCalled_ShouldReturnEmptyListAsync()
    {
        // Act
        var result = await _disabledUpstreamClient.ListPackagesAsync("dummy", default);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DownloadPackageOrNullAsync_IsCalled_ShouldReturnNull()
    {
        // Act
        var result = await _disabledUpstreamClient.DownloadPackageOrNullAsync("dummy", default, default);

        // Assert
        Assert.Null(result);
    }
}
