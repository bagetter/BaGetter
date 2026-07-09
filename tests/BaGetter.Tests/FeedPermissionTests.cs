using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

/// <summary>
/// Verifies that push permissions are enforced per-feed: a user with CanPush on
/// one feed is allowed to push there and denied on other feeds.
/// </summary>
public class FeedPermissionTests : IDisposable
{
    private const string FeedA = "feed-x";
    private const string FeedB = "feed-y";
    private const string LocalUsername = "permtest";
    private const string LocalPassword = "PermTest123!";

    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;

    public FeedPermissionTests(ITestOutputHelper output)
    {
        _app = new BaGetterApplication(output, null, dict =>
        {
            dict["Authentication:Mode"] = "Local";
        });
        _client = _app.CreateClient();
    }

    // --- PAT flow ---

    [Fact]
    public async Task Pat_WithPushPermissionOnFeedA_CanPushToFeedA()
    {
        await _app.CreateFeedAsync(FeedA);
        var (_, token) = await SeedUserWithPermissionsAsync(FeedA, canPush: true, canPull: true);

        var request = BuildPushRequest($"feeds/{FeedA}/api/v2/package", token);
        using var response = await _client.SendAsync(request);

        // 400 = bad request due to empty body = auth + authz passed
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pat_WithPushPermissionOnFeedAOnly_CannotPushToFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);
        var (_, token) = await SeedUserWithPermissionsAsync(FeedA, canPush: true, canPull: true);

        var request = BuildPushRequest($"feeds/{FeedB}/api/v2/package", token);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pat_WithPushPermissionOnFeedA_CanPullFromFeedA()
    {
        await _app.CreateFeedAsync(FeedA);
        var (_, token) = await SeedUserWithPermissionsAsync(FeedA, canPush: false, canPull: true);

        var request = BuildGetRequest($"feeds/{FeedA}/v3/search", token);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Pat_WithPullPermissionOnFeedAOnly_CannotPullFromFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);
        var (_, token) = await SeedUserWithPermissionsAsync(FeedA, canPush: false, canPull: true);

        var request = BuildGetRequest($"feeds/{FeedB}/v3/search", token);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Basic-auth flow ---

    [Fact]
    public async Task BasicAuth_WithPullPermissionOnFeedA_CanSearchFeedA()
    {
        await _app.CreateFeedAsync(FeedA);
        await SeedUserWithPermissionsAsync(FeedA, canPush: false, canPull: true);

        SetBasicAuth();
        using var response = await _client.GetAsync($"feeds/{FeedA}/v3/search");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_WithPullPermissionOnFeedAOnly_CannotSearchFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);
        await SeedUserWithPermissionsAsync(FeedA, canPush: false, canPull: true);

        SetBasicAuth();
        using var response = await _client.GetAsync($"feeds/{FeedB}/v3/search");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_WithPushPermissionOnFeedA_CanPushToFeedA()
    {
        await _app.CreateFeedAsync(FeedA);
        await SeedUserWithPermissionsAsync(FeedA, canPush: true, canPull: true);

        SetBasicAuth();
        var request = new HttpRequestMessage(HttpMethod.Put, $"feeds/{FeedA}/api/v2/package");
        request.Content = new ByteArrayContent([]);
        using var response = await _client.SendAsync(request);

        // 400 = bad request (empty body) = auth + authz passed
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_WithPushPermissionOnFeedAOnly_CannotPushToFeedB()
    {
        await _app.CreateFeedAsync(FeedA);
        await _app.CreateFeedAsync(FeedB);
        await SeedUserWithPermissionsAsync(FeedA, canPush: true, canPull: true);

        SetBasicAuth();
        var request = new HttpRequestMessage(HttpMethod.Put, $"feeds/{FeedB}/api/v2/package");
        request.Content = new ByteArrayContent([]);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Helpers ---

    private void SetBasicAuth()
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{LocalUsername}:{LocalPassword}"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    private static HttpRequestMessage BuildPushRequest(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Add("X-NuGet-ApiKey", token);
        request.Content = new ByteArrayContent([]);
        return request;
    }

    private static HttpRequestMessage BuildGetRequest(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-NuGet-ApiKey", token);
        return request;
    }

    private async Task<(Guid userId, string plaintextToken)> SeedUserWithPermissionsAsync(
        string feedSlug, bool canPush, bool canPull)
    {
        using var scope = _app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

        Guid userId;
        try
        {
            var user = await userService.CreateLocalUserAsync(
                LocalUsername, "Permission Test User",
                LocalPassword, canLoginToUI: false,
                createdByUserId: null,
                CancellationToken.None);
            userId = user.Id;
        }
        catch (InvalidOperationException)
        {
            // User already created (called multiple times in a test class reuse scenario)
            var existing = await userService.FindByUsernameAsync(LocalUsername, CancellationToken.None);
            userId = existing.Id;
        }

        var feed = await feedService.GetFeedBySlugAsync(feedSlug, CancellationToken.None)
            ?? throw new InvalidOperationException($"Feed '{feedSlug}' not found — create it before seeding permissions.");

        await permissionService.GrantPermissionAsync(
            userId, PrincipalType.User, feed.Id,
            canPush: canPush, canPull: canPull,
            CancellationToken.None);

        var result = await tokenService.CreateTokenAsync(
            userId, $"token-{Guid.NewGuid():N}", DateTime.UtcNow.AddDays(30), CancellationToken.None);

        return (userId, result.PlaintextToken);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _app?.Dispose();
    }
}
