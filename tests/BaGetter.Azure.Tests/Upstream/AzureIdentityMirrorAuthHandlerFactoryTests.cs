using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using BaGetter.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace BaGetter.Azure.Tests.Upstream;

public class AzureIdentityMirrorAuthHandlerFactoryTests
{
    [Fact]
    public void CreateHandler_ReturnsAzureIdentityHandler()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        var innerHandler = new TestHandler();
        var handler = factory.CreateHandler(innerHandler);

        Assert.IsType<AzureIdentityAuthenticationHandler>(handler);
    }

    [Fact]
    public void CreateHandler_WrapsInnerHandler()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        var innerHandler = new TestHandler();
        var handler = factory.CreateHandler(innerHandler) as DelegatingHandler;

        Assert.NotNull(handler);
        Assert.Same(innerHandler, handler.InnerHandler);
    }

    [Fact]
    public void ResolveScope_ExplicitScope_TakesPrecedence()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json",
            scope: "https://custom.scope/.default");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        Assert.Equal("https://custom.scope/.default", factory.ResolveScope());
    }

    [Theory]
    [InlineData("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json")]
    [InlineData("https://dev.azure.com/org/_packaging/feed/nuget/v3/index.json")]
    [InlineData("https://myorg.visualstudio.com/_packaging/feed/nuget/v3/index.json")]
    public void ResolveScope_AzureDevOpsUrls_ReturnsVsspsScope(string url)
    {
        var scope = AzureIdentityMirrorAuthHandlerFactory.ResolveScopeFromUrl(new Uri(url));

        Assert.Equal("https://app.vssps.visualstudio.com/.default", scope);
    }

    [Theory]
    [InlineData("https://nuget.example.com/v3/index.json", "https://nuget.example.com/.default")]
    [InlineData("https://myapi.contoso.com/packages/v3/index.json", "https://myapi.contoso.com/.default")]
    public void ResolveScope_NonAzureDevOpsUrls_DerivesScopeFromHost(string url, string expectedScope)
    {
        var scope = AzureIdentityMirrorAuthHandlerFactory.ResolveScopeFromUrl(new Uri(url));

        Assert.Equal(expectedScope, scope);
    }

    [Fact]
    public void ResolveScope_NoPackageSource_FallsBackToAzureDevOps()
    {
        var mirrorOptions = new MirrorOptions
        {
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity
            }
        };
        var factory = new AzureIdentityMirrorAuthHandlerFactory(Options.Create(mirrorOptions));

        Assert.Equal("https://app.vssps.visualstudio.com/.default", factory.ResolveScope());
    }

    [Fact]
    public void ResolveCredential_AllThreeSet_ReturnsClientSecretCredential()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json",
            tenantId: "tenant-id",
            clientId: "client-id",
            clientSecret: "client-secret");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        var credential = factory.ResolveCredential();

        Assert.IsType<ClientSecretCredential>(credential);
    }

    [Fact]
    public void ResolveCredential_OnlyClientId_ReturnsDefaultCredentialWithManagedIdentity()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json",
            clientId: "my-managed-identity-client-id");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        var credential = factory.ResolveCredential();

        Assert.IsType<DefaultAzureCredential>(credential);
    }

    [Fact]
    public void ResolveCredential_NothingSet_ReturnsDefaultCredential()
    {
        var options = CreateOptions(
            packageSource: "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json");
        var factory = new AzureIdentityMirrorAuthHandlerFactory(options);

        var credential = factory.ResolveCredential();

        Assert.IsType<DefaultAzureCredential>(credential);
    }

    private static IOptions<MirrorOptions> CreateOptions(
        string packageSource,
        string scope = null,
        string tenantId = null,
        string clientId = null,
        string clientSecret = null)
    {
        var mirrorOptions = new MirrorOptions
        {
            PackageSource = new Uri(packageSource),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity,
                Scope = scope,
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret
            }
        };
        return Options.Create(mirrorOptions);
    }

    private class TestHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
