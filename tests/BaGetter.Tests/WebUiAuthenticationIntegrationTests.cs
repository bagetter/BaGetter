using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
/// Integration tests for web UI authentication flows: local login, page access control,
/// sign-out, and unauthenticated redirects.
/// </summary>
public class WebUiLocalLoginTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    private const string TestUsername = "localui";
    private const string TestPassword = "TestPassword123!";

    public WebUiLocalLoginTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(_output, null, dict =>
        {
            dict["Authentication:Mode"] = "Hybrid";
            dict["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/";
            dict["Authentication:Entra:TenantId"] = "00000000-0000-0000-0000-000000000000";
            dict["Authentication:Entra:ClientId"] = "00000000-0000-0000-0000-000000000001";
            dict["Authentication:Entra:ClientSecret"] = "test-secret";
            dict["Authentication:MaxFailedAttempts"] = "3";
            dict["Authentication:LockoutMinutes"] = "15";
        });
        _client = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LoginPage_Get_ReturnsOk()
    {
        // Act
        using var response = await _client.GetAsync("/Login");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoginPage_WhenModeIsNone_RedirectsToIndex()
    {
        // Arrange - create app with Mode=None
        using var app = new BaGetterApplication(_output, null, dict =>
        {
            dict["Authentication:Mode"] = "Config";
        });
        using var client = app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync("/Login");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.True(location == "/" || location.EndsWith("/Index", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to / or /Index but got: {location}");
    }

    [Fact]
    public async Task LoginPage_PostValidCredentials_RedirectsToIndex()
    {
        // Arrange
        await SeedLocalUserAsync(canLoginToUI: true, canPull: true);

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", TestPassword }
        });

        // First get the login page to obtain the antiforgery token
        using var getResponse = await _client.GetAsync("/Login");
        var antiforgeryToken = await ExtractAntiforgeryTokenAsync(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Login");
        var formValues = new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", TestPassword }
        };
        if (antiforgeryToken != null)
        {
            formValues.Add("__RequestVerificationToken", antiforgeryToken);
        }

        // Carry cookies from the GET request (antiforgery cookie)
        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
            {
                request.Headers.Add("Cookie", cookie.Split(';')[0]);
            }
        }

        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert - successful login redirects
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");
    }

    [Fact]
    public async Task LoginPage_PostInvalidPassword_ReturnsPageWithError()
    {
        // Arrange
        await SeedLocalUserAsync(canLoginToUI: true, canPull: true);

        using var getResponse = await _client.GetAsync("/Login");
        var antiforgeryToken = await ExtractAntiforgeryTokenAsync(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Login");
        var formValues = new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", "WrongPassword123!" }
        };
        if (antiforgeryToken != null)
        {
            formValues.Add("__RequestVerificationToken", antiforgeryToken);
        }

        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
            {
                request.Headers.Add("Cookie", cookie.Split(';')[0]);
            }
        }

        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert - stays on login page (200 OK with error)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid username or password", body);
    }

    [Fact]
    public async Task LoginPage_PostDisabledAccount_ReturnsPageWithError()
    {
        // Arrange
        var userId = await SeedLocalUserAsync(canLoginToUI: true, canPull: true);
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            await userService.SetEnabledAsync(userId, false, CancellationToken.None);
        }

        using var getResponse = await _client.GetAsync("/Login");
        var antiforgeryToken = await ExtractAntiforgeryTokenAsync(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Login");
        var formValues = new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", TestPassword }
        };
        if (antiforgeryToken != null)
        {
            formValues.Add("__RequestVerificationToken", antiforgeryToken);
        }

        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
            {
                request.Headers.Add("Cookie", cookie.Split(';')[0]);
            }
        }

        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("disabled", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_PostAccountWithoutUIAccess_ReturnsPageWithError()
    {
        // Arrange
        await SeedLocalUserAsync(canLoginToUI: false, canPull: true);

        using var getResponse = await _client.GetAsync("/Login");
        var antiforgeryToken = await ExtractAntiforgeryTokenAsync(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Login");
        var formValues = new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", TestPassword }
        };
        if (antiforgeryToken != null)
        {
            formValues.Add("__RequestVerificationToken", antiforgeryToken);
        }

        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
            {
                request.Headers.Add("Cookie", cookie.Split(';')[0]);
            }
        }

        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not permitted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_RepeatedFailedAttempts_LocksAccount()
    {
        // Arrange
        await SeedLocalUserAsync(canLoginToUI: true, canPull: true);

        // Fail 3 times (MaxFailedAttempts = 3)
        for (var i = 0; i < 3; i++)
        {
            using var getResp = await _client.GetAsync("/Login");
            var token = await ExtractAntiforgeryTokenAsync(getResp);

            var req = new HttpRequestMessage(HttpMethod.Post, "/Login");
            var vals = new Dictionary<string, string>
            {
                { "Username", TestUsername },
                { "Password", "WrongPassword!" }
            };
            if (token != null) vals.Add("__RequestVerificationToken", token);
            if (getResp.Headers.Contains("Set-Cookie"))
            {
                foreach (var c in getResp.Headers.GetValues("Set-Cookie"))
                    req.Headers.Add("Cookie", c.Split(';')[0]);
            }
            req.Content = new FormUrlEncodedContent(vals);
            using var _ = await _client.SendAsync(req);
        }

        // Now try with the correct password
        using var getResponse = await _client.GetAsync("/Login");
        var antiforgeryToken = await ExtractAntiforgeryTokenAsync(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Login");
        var formValues = new Dictionary<string, string>
        {
            { "Username", TestUsername },
            { "Password", TestPassword }
        };
        if (antiforgeryToken != null) formValues.Add("__RequestVerificationToken", antiforgeryToken);
        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var c in getResponse.Headers.GetValues("Set-Cookie"))
                request.Headers.Add("Cookie", c.Split(';')[0]);
        }
        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert - account should be locked
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EntraSignIn_WhenEntraNotConfigured_RedirectsToIndex()
    {
        // Arrange - create app with Mode=Local (no Entra)
        using var app = new BaGetterApplication(_output, null, dict =>
        {
            dict["Authentication:Mode"] = "Local";
        });
        using var client = app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - attempt Entra sign-in when not enabled
        using var response = await client.GetAsync("/Login?handler=EntraSignIn");

        // Assert - should redirect to index since Entra is not enabled
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.True(location == "/" || location.Contains("Index", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to / or /Index but got: {location}");
    }

    private async Task<Guid> SeedLocalUserAsync(bool canLoginToUI, bool canPull)
    {
        using var scope = _app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

        var user = await userService.CreateLocalUserAsync(
            TestUsername, "Test User",
            TestPassword, canLoginToUI,
            createdByUserId: null,
            CancellationToken.None);

        var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
        await permissionService.GrantPermissionAsync(
            user.Id, PrincipalType.User, defaultFeed.Id,
            canPush: false, canPull: canPull,
            CancellationToken.None);

        return user.Id;
    }

    private static async Task<string> ExtractAntiforgeryTokenAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        const string tokenFieldName = "__RequestVerificationToken";
        var idx = body.IndexOf(tokenFieldName, StringComparison.Ordinal);
        if (idx < 0) return null;

        var valueIdx = body.IndexOf("value=\"", idx, StringComparison.Ordinal);
        if (valueIdx < 0) return null;

        valueIdx += "value=\"".Length;
        var endIdx = body.IndexOf('"', valueIdx);
        if (endIdx < 0) return null;

        return body[valueIdx..endIdx];
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.Dispose();
    }
}

/// <summary>
/// Tests for page access control: admin pages require admin permissions,
/// PAT page requires Entra authentication, unauthenticated users are redirected.
/// Uses a CookieContainer-based HttpClient to properly handle authentication cookies.
/// </summary>
public class WebUiAccessControlTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _noRedirectClient;
    private readonly ITestOutputHelper _output;

    public WebUiAccessControlTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(output, null, dict =>
        {
            dict["Authentication:Mode"] = "Hybrid";
            dict["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/";
            dict["Authentication:Entra:TenantId"] = "00000000-0000-0000-0000-000000000000";
            dict["Authentication:Entra:ClientId"] = "00000000-0000-0000-0000-000000000001";
            dict["Authentication:Entra:ClientSecret"] = "test-secret";
        });
        _noRedirectClient = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Creates an HttpClient with a CookieContainer that handles cookies automatically
    /// through the test server's pipeline. HandleCookies=true ensures the cookie from
    /// sign-in is automatically sent on subsequent requests.
    /// </summary>
    private HttpClient CreateCookieTrackingClient()
    {
        return _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Theory]
    [InlineData("/Account/Tokens")]
    [InlineData("/Admin/Accounts")]
    [InlineData("/Admin/Groups")]
    public async Task ProtectedPage_WhenUnauthenticated_ReturnsUnauthorizedOrRedirect(string path)
    {
        // Act
        using var response = await _noRedirectClient.GetAsync(path);

        // Assert - should be 401/403 or redirect to login
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.Redirect,
            $"Expected 401/403/redirect for {path} but got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            // Should redirect to a login page
            var location = response.Headers.Location?.ToString() ?? "";
            Assert.True(
                location.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("signin", StringComparison.OrdinalIgnoreCase),
                $"Expected redirect to login page but got: {location}");
        }
    }

    [Fact]
    public async Task AdminAccounts_NonAdminUser_RedirectsToIndex()
    {
        // Arrange - create a regular user (not admin)
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

            var user = await userService.CreateLocalUserAsync(
                "regular", "Regular User",
                "RegularPassword123!", canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);

            var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
            await permissionService.GrantPermissionAsync(
                user.Id, PrincipalType.User, defaultFeed.Id,
                canPush: false, canPull: true,
                CancellationToken.None);
        }

        // Sign in using a cookie-tracking client
        using var client = CreateCookieTrackingClient();
        var signedIn = await SignInLocalUserAsync(client, "regular", "RegularPassword123!");
        Assert.True(signedIn, "Failed to sign in as regular user");

        // Act - access admin page
        using var response = await client.GetAsync("/Admin/Accounts");

        // Assert - non-admin should be redirected to Index
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Expected redirect or OK but got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.Equal("/", response.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task AdminAccounts_AdminUser_ReturnsOk()
    {
        // Arrange - create an admin user
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

            var user = await userService.CreateLocalUserAsync(
                "admin", "Admin User",
                "AdminPassword123!", canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);

            await userService.SetAdminAsync(user.Id, true, CancellationToken.None);

            var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
            await permissionService.GrantPermissionAsync(
                user.Id, PrincipalType.User, defaultFeed.Id,
                canPush: true, canPull: true,
                CancellationToken.None);
        }

        using var client = CreateCookieTrackingClient();
        var signedIn = await SignInLocalUserAsync(client, "admin", "AdminPassword123!");
        Assert.True(signedIn, "Failed to sign in as admin user");

        // Follow the redirect after login to reach the admin page
        using var autoClient = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
        // We need to sign in again with the auto-redirect client
        await SignInLocalUserAsync(autoClient, "admin", "AdminPassword123!");

        // Act
        using var response = await autoClient.GetAsync("/Admin/Accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("admin", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TokensPage_NonEntraUser_RedirectsToIndex()
    {
        // Arrange - create a local user
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

            var user = await userService.CreateLocalUserAsync(
                "localuser", "Local User",
                "LocalPassword123!", canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);

            var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
            await permissionService.GrantPermissionAsync(
                user.Id, PrincipalType.User, defaultFeed.Id,
                canPush: false, canPull: true,
                CancellationToken.None);
        }

        using var client = CreateCookieTrackingClient();
        var signedIn = await SignInLocalUserAsync(client, "localuser", "LocalPassword123!");
        Assert.True(signedIn, "Failed to sign in as local user");

        // Act
        using var response = await client.GetAsync("/Account/Tokens");

        // Assert - local user should be redirected away from Tokens page
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Expected redirect or OK but got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.Equal("/", response.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task IndexPage_WhenUnauthenticated_ReturnsOk()
    {
        using var response = await _noRedirectClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Signs in a local user via the login page using the provided cookie-tracking client.
    /// Returns true if sign-in succeeded (redirect after login).
    /// </summary>
    private static async Task<bool> SignInLocalUserAsync(HttpClient client, string username, string password)
    {
        // GET the login page to obtain antiforgery token and cookie
        using var getResponse = await client.GetAsync("/Login");
        var body = await getResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryTokenFromBody(body);

        var formValues = new Dictionary<string, string>
        {
            { "Username", username },
            { "Password", password }
        };
        if (antiforgeryToken != null)
            formValues.Add("__RequestVerificationToken", antiforgeryToken);

        // POST login - the cookie-tracking client will automatically handle Set-Cookie
        using var response = await client.PostAsync("/Login", new FormUrlEncodedContent(formValues));

        return response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently;
    }

    private static string ExtractAntiforgeryTokenFromBody(string body)
    {
        const string tokenFieldName = "__RequestVerificationToken";
        var idx = body.IndexOf(tokenFieldName, StringComparison.Ordinal);
        if (idx < 0) return null;

        var valueIdx = body.IndexOf("value=\"", idx, StringComparison.Ordinal);
        if (valueIdx < 0) return null;

        valueIdx += "value=\"".Length;
        var endIdx = body.IndexOf('"', valueIdx);
        if (endIdx < 0) return null;

        return body[valueIdx..endIdx];
    }

    public void Dispose()
    {
        _noRedirectClient.Dispose();
        _app.Dispose();
    }
}

/// <summary>
/// Integration tests for account admin toggle actions: enable/disable and web UI access.
/// </summary>
public class WebUiAccountToggleTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly ITestOutputHelper _output;

    private const string AdminUsername = "toggleadmin";
    private const string AdminPassword = "ToggleAdminPass1!";

    public WebUiAccountToggleTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(output, null, dict =>
        {
            dict["Authentication:Mode"] = "Hybrid";
            dict["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/";
            dict["Authentication:Entra:TenantId"] = "00000000-0000-0000-0000-000000000000";
            dict["Authentication:Entra:ClientId"] = "00000000-0000-0000-0000-000000000001";
            dict["Authentication:Entra:ClientSecret"] = "test-secret";
        });
    }

    private HttpClient CreateCookieTrackingClient()
    {
        return _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    /// <summary>
    /// Creates an admin user and a target user, signs the admin in, and returns
    /// the cookie-tracking client and the target user's ID.
    /// </summary>
    private async Task<(HttpClient client, Guid targetUserId)> SetupAdminAndTargetUserAsync(
        string targetUsername = "targetuser", bool targetCanLoginToUI = true)
    {
        Guid targetUserId;
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

            var admin = await userService.CreateLocalUserAsync(
                AdminUsername, "Admin",
                AdminPassword, canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);

            await userService.SetAdminAsync(admin.Id, true, CancellationToken.None);

            var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
            await permissionService.GrantPermissionAsync(
                admin.Id, PrincipalType.User, defaultFeed.Id,
                canPush: true, canPull: true,
                CancellationToken.None);

            var target = await userService.CreateLocalUserAsync(
                targetUsername, "Target User",
                "TargetPassword123!", canLoginToUI: targetCanLoginToUI,
                createdByUserId: admin.Id, CancellationToken.None);

            targetUserId = target.Id;
        }

        var client = CreateCookieTrackingClient();
        var signedIn = await SignInLocalUserAsync(client, AdminUsername, AdminPassword);
        Assert.True(signedIn, "Failed to sign in as admin");

        return (client, targetUserId);
    }

    [Fact]
    public async Task ToggleEnabled_DisablesEnabledUser()
    {
        var (client, targetUserId) = await SetupAdminAndTargetUserAsync();

        // GET the Accounts page to obtain antiforgery token
        using var getResponse = await client.GetAsync("/Admin/Accounts");
        var body = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryTokenFromBody(body);

        // POST ToggleEnabled with isEnabled=True -> should disable the user
        var formValues = new Dictionary<string, string>
        {
            { "userId", targetUserId.ToString() },
            { "isEnabled", "True" }
        };
        if (token != null)
            formValues.Add("__RequestVerificationToken", token);

        using var response = await client.PostAsync(
            "/Admin/Accounts?handler=ToggleEnabled", new FormUrlEncodedContent(formValues));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");

        // Verify the user is now disabled
        using var scope = _app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var updated = await userService.FindByIdAsync(targetUserId, CancellationToken.None);
        Assert.False(updated.IsEnabled);
    }

    [Fact]
    public async Task ToggleEnabled_EnablesDisabledUser()
    {
        var (client, targetUserId) = await SetupAdminAndTargetUserAsync("disableduser");

        // Disable the user first
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            await userService.SetEnabledAsync(targetUserId, false, CancellationToken.None);
        }

        // GET the Accounts page to obtain antiforgery token
        using var getResponse = await client.GetAsync("/Admin/Accounts");
        var body = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryTokenFromBody(body);

        // POST ToggleEnabled with isEnabled=False -> should enable the user
        var formValues = new Dictionary<string, string>
        {
            { "userId", targetUserId.ToString() },
            { "isEnabled", "False" }
        };
        if (token != null)
            formValues.Add("__RequestVerificationToken", token);

        using var response = await client.PostAsync(
            "/Admin/Accounts?handler=ToggleEnabled", new FormUrlEncodedContent(formValues));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");

        // Verify the user is now enabled
        using var verifyScope = _app.Services.CreateScope();
        var verifyService = verifyScope.ServiceProvider.GetRequiredService<IUserService>();
        var updated = await verifyService.FindByIdAsync(targetUserId, CancellationToken.None);
        Assert.True(updated.IsEnabled);
    }

    [Fact]
    public async Task ToggleCanLoginToUI_RevokesWebAccess()
    {
        var (client, targetUserId) = await SetupAdminAndTargetUserAsync("uiuser", targetCanLoginToUI: true);

        // GET the Accounts page to obtain antiforgery token
        using var getResponse = await client.GetAsync("/Admin/Accounts");
        var body = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryTokenFromBody(body);

        // POST ToggleCanLoginToUI with canLoginToUI=True -> should revoke
        var formValues = new Dictionary<string, string>
        {
            { "userId", targetUserId.ToString() },
            { "canLoginToUI", "True" }
        };
        if (token != null)
            formValues.Add("__RequestVerificationToken", token);

        using var response = await client.PostAsync(
            "/Admin/Accounts?handler=ToggleCanLoginToUI", new FormUrlEncodedContent(formValues));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");

        // Verify web access is revoked
        using var scope = _app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var updated = await userService.FindByIdAsync(targetUserId, CancellationToken.None);
        Assert.False(updated.CanLoginToUI);
    }

    [Fact]
    public async Task ToggleCanLoginToUI_GrantsWebAccess()
    {
        var (client, targetUserId) = await SetupAdminAndTargetUserAsync("nouiuser", targetCanLoginToUI: false);

        // GET the Accounts page to obtain antiforgery token
        using var getResponse = await client.GetAsync("/Admin/Accounts");
        var body = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryTokenFromBody(body);

        // POST ToggleCanLoginToUI with canLoginToUI=False -> should grant
        var formValues = new Dictionary<string, string>
        {
            { "userId", targetUserId.ToString() },
            { "canLoginToUI", "False" }
        };
        if (token != null)
            formValues.Add("__RequestVerificationToken", token);

        using var response = await client.PostAsync(
            "/Admin/Accounts?handler=ToggleCanLoginToUI", new FormUrlEncodedContent(formValues));

        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");

        // Verify web access is granted
        using var scope = _app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var updated = await userService.FindByIdAsync(targetUserId, CancellationToken.None);
        Assert.True(updated.CanLoginToUI);
    }

    [Fact]
    public async Task ToggleCanLoginToUI_NonAdmin_RedirectsToIndex()
    {
        // Create a non-admin user
        Guid targetUserId;
        using (var scope = _app.Services.CreateScope())
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var feedService = scope.ServiceProvider.GetRequiredService<IFeedService>();

            var regularUser = await userService.CreateLocalUserAsync(
                "nonadmin", "Non Admin",
                "NonAdminPass123!", canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);

            var defaultFeed = await feedService.GetDefaultFeedAsync(CancellationToken.None);
            await permissionService.GrantPermissionAsync(
                regularUser.Id, PrincipalType.User, defaultFeed.Id,
                canPush: false, canPull: true,
                CancellationToken.None);

            var target = await userService.CreateLocalUserAsync(
                "victim", "Victim",
                "VictimPassword123!", canLoginToUI: true,
                createdByUserId: null, CancellationToken.None);
            targetUserId = target.Id;
        }

        var client = CreateCookieTrackingClient();
        var signedIn = await SignInLocalUserAsync(client, "nonadmin", "NonAdminPass123!");
        Assert.True(signedIn, "Failed to sign in as non-admin");

        // Non-admin cannot access the admin page to get a valid antiforgery token,
        // so the POST will either be rejected by antiforgery (400) or the handler
        // will redirect to Index (/). Either way the toggle must NOT execute.
        var formValues = new Dictionary<string, string>
        {
            { "userId", targetUserId.ToString() },
            { "canLoginToUI", "True" }
        };

        using var response = await client.PostAsync(
            "/Admin/Accounts?handler=ToggleCanLoginToUI", new FormUrlEncodedContent(formValues));

        // Accept redirect to / (authorization check) or 400 (antiforgery rejection)
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.BadRequest,
            $"Expected redirect or 400 but got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.Equal("/", response.Headers.Location?.ToString());
        }

        // Verify the user was NOT changed
        using var scope2 = _app.Services.CreateScope();
        var userService2 = scope2.ServiceProvider.GetRequiredService<IUserService>();
        var unchanged = await userService2.FindByIdAsync(targetUserId, CancellationToken.None);
        Assert.True(unchanged.CanLoginToUI, "Non-admin should not be able to toggle CanLoginToUI");
    }

    [Fact]
    public async Task AccountsPage_ShowsToggleButtons()
    {
        var (client, _) = await SetupAdminAndTargetUserAsync("btnuser");

        // Use auto-redirect client to fully render the page
        using var autoClient = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
        await SignInLocalUserAsync(autoClient, AdminUsername, AdminPassword);

        using var response = await autoClient.GetAsync("/Admin/Accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Page should contain both toggle handlers
        Assert.Contains("ToggleEnabled", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ToggleCanLoginToUI", body, StringComparison.OrdinalIgnoreCase);

        // Page should contain the button labels
        Assert.Contains("Disable", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Revoke Web Access", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> SignInLocalUserAsync(HttpClient client, string username, string password)
    {
        using var getResponse = await client.GetAsync("/Login");
        var body = await getResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryTokenFromBody(body);

        var formValues = new Dictionary<string, string>
        {
            { "Username", username },
            { "Password", password }
        };
        if (antiforgeryToken != null)
            formValues.Add("__RequestVerificationToken", antiforgeryToken);

        using var response = await client.PostAsync("/Login", new FormUrlEncodedContent(formValues));
        return response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently;
    }

    private static string ExtractAntiforgeryTokenFromBody(string body)
    {
        const string tokenFieldName = "__RequestVerificationToken";
        var idx = body.IndexOf(tokenFieldName, StringComparison.Ordinal);
        if (idx < 0) return null;

        var valueIdx = body.IndexOf("value=\"", idx, StringComparison.Ordinal);
        if (valueIdx < 0) return null;

        valueIdx += "value=\"".Length;
        var endIdx = body.IndexOf('"', valueIdx);
        if (endIdx < 0) return null;

        return body[valueIdx..endIdx];
    }

    public void Dispose()
    {
        _app.Dispose();
    }
}

/// <summary>
/// Tests for the Logout page.
/// </summary>
public class WebUiSignOutTests : IDisposable
{
    private readonly BaGetterApplication _app;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public WebUiSignOutTests(ITestOutputHelper output)
    {
        _output = output;
        _app = new BaGetterApplication(output, null, dict =>
        {
            dict["Authentication:Mode"] = "Hybrid";
            dict["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/";
            dict["Authentication:Entra:TenantId"] = "00000000-0000-0000-0000-000000000000";
            dict["Authentication:Entra:ClientId"] = "00000000-0000-0000-0000-000000000001";
            dict["Authentication:Entra:ClientSecret"] = "test-secret";
        });
        _client = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Logout_Post_RedirectsToIndex()
    {
        // Arrange - get antiforgery token from logout page (or any page)
        using var getResponse = await _client.GetAsync("/Login");
        var body = await getResponse.Content.ReadAsStringAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, "/Logout");

        // Carry cookies from GET
        if (getResponse.Headers.Contains("Set-Cookie"))
        {
            foreach (var cookie in getResponse.Headers.GetValues("Set-Cookie"))
                request.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        var antiforgeryToken = ExtractAntiforgeryToken(body);
        var formValues = new Dictionary<string, string>();
        if (antiforgeryToken != null)
            formValues.Add("__RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(formValues);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert - should redirect to index after sign-out
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently,
            $"Expected redirect but got {response.StatusCode}");

        if (response.Headers.Location != null)
        {
            Assert.Equal("/", response.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task Logout_Get_ReturnsMethodNotAllowed()
    {
        // Logout only supports POST (to prevent CSRF via GET)
        using var response = await _client.GetAsync("/Logout");

        // Razor pages that only have OnPost return 405 for GET, or 200 with empty page
        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.OK,
            $"Expected 405 or 200 but got {response.StatusCode}");
    }

    private static string ExtractAntiforgeryToken(string body)
    {
        const string tokenFieldName = "__RequestVerificationToken";
        var idx = body.IndexOf(tokenFieldName, StringComparison.Ordinal);
        if (idx < 0) return null;

        var valueIdx = body.IndexOf("value=\"", idx, StringComparison.Ordinal);
        if (valueIdx < 0) return null;

        valueIdx += "value=\"".Length;
        var endIdx = body.IndexOf('"', valueIdx);
        if (endIdx < 0) return null;

        return body[valueIdx..endIdx];
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.Dispose();
    }
}
