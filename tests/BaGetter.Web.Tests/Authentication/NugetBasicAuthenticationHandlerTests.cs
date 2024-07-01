using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BaGetter.Core;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests;

public class NugetBasicAuthenticationHandlerTests
{
    private readonly Mock<IOptions<BaGetterOptions>> _bagetterOptions;
    private readonly UrlEncoder _encoder;
    private readonly Mock<HttpContext> _httpContext;
    private readonly Mock<HttpRequest> _httpRequest;
    private readonly Mock<HttpResponse> _httpResponse;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _options;

    public NugetBasicAuthenticationHandlerTests()
    {
        _options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _options.Setup(x => x.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        _loggerFactory = new Mock<ILoggerFactory>();
        _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger<NugetBasicAuthenticationHandler>>());

        _encoder = UrlEncoder.Default;

        _bagetterOptions = new Mock<IOptions<BaGetterOptions>>();

        _httpContext = new Mock<HttpContext>();
        _httpRequest = new Mock<HttpRequest>();
        _httpResponse = new Mock<HttpResponse>();

        _httpContext.SetupGet(x => x.Request).Returns(_httpRequest.Object);
        _httpContext.SetupGet(x => x.Response).Returns(_httpResponse.Object);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_AnonymousAllowed_ReturnsSuccessResult()
    {
        // Arrange
        SetupBaGetterOptions(new BaGetterOptions());
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.Principal.HasClaim(ClaimTypes.Anonymous, string.Empty));
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidAuthorizationHeader_ReturnsFailResult()
    {
        // Arrange
        SetupBaGetterOptions(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = "user", Password = "pass" }]
            }
        });
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues("InvalidHeader") }
        });
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid Authorization Header", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidCredentials_ReturnsFailResult()
    {
        // Arrange
        SetupBaGetterOptions(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = "user", Password = "pass" }]
            }
        });
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("invaliduser:invalidpass"))}") }
        });
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal("Invalid Username or Password", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_NoAuthorizationHeader_ReturnsNoResult()
    {
        // Arrange
        SetupBaGetterOptions(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = "user", Password = "pass" }]
            }
        });
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        const string username = "testuser";
        const string password = "testpass";
        SetupBaGetterOptions(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = username, Password = password }]
            }
        });
        _httpRequest.Setup(r => r.Headers).Returns(new HeaderDictionary
        {
            { "Authorization", new StringValues($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}") }
        });
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.Principal.HasClaim(ClaimTypes.Name, username));
    }

    private NugetBasicAuthenticationHandler CreateHandler()
    {
        var handler = new NugetBasicAuthenticationHandler(
            _options.Object,
            _loggerFactory.Object,
            _encoder,
            _bagetterOptions.Object);

        handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(NugetBasicAuthenticationHandler)), _httpContext.Object).GetAwaiter().GetResult();

        return handler;
    }

    private void SetupBaGetterOptions(BaGetterOptions options)
    {
        _bagetterOptions.Setup(x => x.Value).Returns(options);
    }
}
