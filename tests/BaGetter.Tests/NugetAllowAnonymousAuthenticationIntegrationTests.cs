using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class NugetAllowAnonymousAuthenticationIntegrationTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public NugetAllowAnonymousAuthenticationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(_output, null);
        _client = _app.CreateClient();
    }

    [Fact]
    public async Task AnonymousAccess_WhenAnonymousAllowed_ReturnsOk()
    {
        // Act
        using var response = await _client.GetAsync("v3/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Credentials_WhenAnonymousAllowed_ReturnsOk()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add(
            "Authorization",
            (IEnumerable<string?>)new StringValues($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"x:x"))}"));

        // Act
        using var response = await _client.GetAsync("v3/index.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _app.Dispose();
        _client.Dispose();
    }
}
