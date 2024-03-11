using BaGetter.Protocol;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BaGetter.Core.Tests.Upstream;
public class UpstreamAuthenticationTests
{
    private static readonly ServiceIndexResponse TestServices = new()
    {
        Version = "3.0.0",
        Resources = [
            new ServiceIndexItem
            {
                Type = "PackageBaseAddress/3.0.0",
                ResourceUrl = "http://localhost/v3/package",
            },
            new ServiceIndexItem
            {
                Type = "RegistrationsBaseUrl",
                ResourceUrl = "http://localhost/v3/registration",
            },
            new ServiceIndexItem
            {
                Type = "SearchQueryService",
                ResourceUrl = "http://localhost/v3/search",
            }
        ]
    };

    [Fact]
    public async Task TestMirrorBasicAuthConfiguration()
    {
        var (services, mock) = SetupApp(opt =>
        {
            opt.Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.Basic,
                Username = "user",
                Password = "password"
            };
        });

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(TestServices)
            })
            .Callback<HttpRequestMessage, CancellationToken>((r, c) =>
            {
                Assert.Equal("Basic", r.Headers.Authorization.Scheme);
                Assert.Equal("dXNlcjpwYXNzd29yZA==", r.Headers.Authorization.Parameter);
            })
            .Verifiable(Times.Once());

        var client = services.GetRequiredService<HttpClient>();
        var res = await client.GetFromJsonAsync<ServiceIndexResponse>("http://localhost/v3/index.json");
    }

    [Fact]
    public async Task TestMirrorBearerAuthConfiguration()
    {
        var (services, mock) = SetupApp(opt =>
        {
            opt.Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.Bearer,
                Token = "token"
            };
        });

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(TestServices)
            })
            .Callback<HttpRequestMessage, CancellationToken>((r, c) =>
            {
                Assert.Equal("Bearer", r.Headers.Authorization.Scheme);
                Assert.Equal("token", r.Headers.Authorization.Parameter);
            })
            .Verifiable(Times.Once());

        var client = services.GetRequiredService<HttpClient>();
        var res = await client.GetFromJsonAsync<ServiceIndexResponse>("http://localhost/v3/index.json");
    }

    [Fact]
    public async Task TestMirrorCustomAuthConfiguration()
    {
        var (services, mock) = SetupApp(opt =>
        {
            opt.Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.Custom,
                CustomHeaders = new Dictionary<string, string>
                {
                    { "X-Auth", "value1" },
                }
            };
        });

        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(TestServices)
            })
            .Callback<HttpRequestMessage, CancellationToken>((r, c) =>
            {
                Assert.Equal("value1", r.Headers.GetValues("X-Auth").First());
            })
            .Verifiable(Times.Once());

        var client = services.GetRequiredService<HttpClient>();
        var res = await client.GetFromJsonAsync<ServiceIndexResponse>("http://localhost/v3/index.json");
    }

    private static (IServiceProvider serivces, Mock<HttpMessageHandler> mockHandler) SetupApp(Action<MirrorOptions> setupOptions)
    {
        Mock<HttpMessageHandler> mockHandler = new();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton(new HttpClient(mockHandler.Object))
            .AddBaGetterApplication(app => { })
            .Configure<MirrorOptions>(opt =>
            {
                opt.PackageSource = new Uri("http://localhost/v3/index.json");
                setupOptions(opt);
            })
            .BuildServiceProvider();
        serviceProvider.GetRequiredService<NuGetClientFactory>();

        return (serviceProvider, mockHandler);
    }
}
