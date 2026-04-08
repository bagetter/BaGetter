using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using BaGetter.Azure;
using Moq;
using Xunit;

namespace BaGetter.Azure.Tests.Upstream;

public class AzureIdentityAuthenticationHandlerTests
{
    private const string TestScope = "https://test.scope/.default";

    [Fact]
    public async Task SendAsync_SetsAuthorizationBearerHeader()
    {
        var expectedToken = "test-token-123";
        var credential = CreateMockCredential(expectedToken, DateTimeOffset.UtcNow.AddHours(1));
        var innerHandler = new TestHandler();

        using var handler = new AzureIdentityAuthenticationHandler(credential, TestScope, innerHandler);
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test");

        Assert.NotNull(innerHandler.LastRequest);
        Assert.Equal("Bearer", innerHandler.LastRequest.Headers.Authorization.Scheme);
        Assert.Equal(expectedToken, innerHandler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_CachesToken_WhenNotExpired()
    {
        var callCount = 0;
        var credential = new Mock<TokenCredential>();
        credential
            .Setup(c => c.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new AccessToken("token-" + callCount, DateTimeOffset.UtcNow.AddHours(1));
            });

        var innerHandler = new TestHandler();
        using var handler = new AzureIdentityAuthenticationHandler(credential.Object, TestScope, innerHandler);
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test1");
        await client.GetAsync("http://localhost/test2");

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SendAsync_RefreshesToken_WhenNearExpiry()
    {
        var callCount = 0;
        var credential = new Mock<TokenCredential>();
        credential
            .Setup(c => c.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // Return a token that expires within 5 minutes (triggers refresh)
                return new AccessToken("token-" + callCount, DateTimeOffset.UtcNow.AddMinutes(3));
            });

        var innerHandler = new TestHandler();
        using var handler = new AzureIdentityAuthenticationHandler(credential.Object, TestScope, innerHandler);
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/test1");
        await client.GetAsync("http://localhost/test2");

        Assert.Equal(2, callCount);
    }

    private static TokenCredential CreateMockCredential(string token, DateTimeOffset expiresOn)
    {
        var mock = new Mock<TokenCredential>();
        mock.Setup(c => c.GetTokenAsync(It.IsAny<TokenRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessToken(token, expiresOn));
        return mock.Object;
    }

    private class TestHandler : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
