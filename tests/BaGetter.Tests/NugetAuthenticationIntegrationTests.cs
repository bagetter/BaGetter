using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class NugetAuthenticationIntegrationTests : IDisposable
{
    private const string Username = "username";
    private const string Password = "password";
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public NugetAuthenticationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(_output, null, dict =>
        {
            dict.Add("Authentication:Credentials:0:Username", Username);
            dict.Add("Authentication:Credentials:0:Password", Password);
        });
        _client = _app.CreateClient();
    }

    [Fact]
    public async Task AnonymousAccess_WhenAnonymousNotAllowed_ReturnsUnauthorized()
    {
        // Act
        using var response = await _client.GetAsync("v3/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidCredentialsAccess_WhenAnonymousNotAllowed_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new(AuthenticationConstants.NugetBasicAuthenticationScheme, $"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"))}");

        // Act
        using var response = await _client.GetAsync("v3/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidCredentialsAccess_WhenAnonymousNotAllowed_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new(AuthenticationConstants.NugetBasicAuthenticationScheme, $"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}x"))}");

        // Act
        using var response = await _client.GetAsync("v3/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _app.Dispose();
        _client.Dispose();
    }
}
