using System.Threading.Tasks;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.ServiceIndex;

public class BaGetterServiceIndexTests
{
    [Fact]
    public async Task ServiceIndexUsesOnlyBaGetterUrls()
    {
        var url = new Mock<IUrlGenerator>();
        url.Setup(u => u.GetPackagePublishResourceUrl()).Returns("http://localhost/api/v2/package");
        url.Setup(u => u.GetSymbolPublishResourceUrl()).Returns("http://localhost/api/v2/symbol");
        url.Setup(u => u.GetSearchResourceUrl()).Returns("http://localhost/v3/search");
        url.Setup(u => u.GetPackageMetadataResourceUrl()).Returns("http://localhost/v3/registration/");
        url.Setup(u => u.GetPackageContentResourceUrl()).Returns("http://localhost/v3/package/");
        url.Setup(u => u.GetAutocompleteResourceUrl()).Returns("http://localhost/v3/autocomplete");
        var target = new BaGetterServiceIndex(url.Object);

        var response = await target.GetAsync();

        Assert.NotEmpty(response.Resources);
        Assert.All(response.Resources, resource =>
        {
            Assert.StartsWith("http://localhost/", resource.ResourceUrl);
            Assert.DoesNotContain("nuget.org", resource.ResourceUrl);
        });
    }
}
