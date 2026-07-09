using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;
using BaGetter.Core.Configuration;
using BaGetter.Core.Authentication;
using BaGetter.Web.Extensions;

namespace BaGetter.Web.Authentication;

public class NugetBasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptions<BaGetterOptions> _bagetterOptions;
    private readonly IFeedAuthenticationService _feedAuthService;

    public NugetBasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BaGetterOptions> bagetterOptions,
        IFeedAuthenticationService feedAuthService)
        : base(options, logger, encoder)
    {
        _bagetterOptions = bagetterOptions;
        _feedAuthService = feedAuthService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authMode = _bagetterOptions.Value.Authentication?.Mode ?? AuthenticationMode.Config;

        if (authMode == AuthenticationMode.Config)
        {
            // Static auth mode: use configured credentials
            return await HandleStaticAuthAsync();
        }

        // New mode: use database-backed authentication
        return await HandleNewAuthenticateAsync();
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"NuGet Server\"";
        await base.HandleChallengeAsync(properties);
    }

    private Task<AuthenticateResult> HandleStaticAuthAsync()
    {
        if (IsOpenAccessAllowed())
            return CreateAnonymousAuthenticationResult();

        if (!Request.Headers.TryGetValue("Authorization", out var auth))
            return Task.FromResult(AuthenticateResult.NoResult());

        string username;
        string password;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(auth);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split([':'], 2);
            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (!ValidateStaticCredentials(username, password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        return CreateUserAuthenticationResult(username, null);
    }

    private async Task<AuthenticateResult> HandleNewAuthenticateAsync()
    {
        // Try X-NuGet-ApiKey first (used by dotnet nuget push -k <token>)
        var apiKey = Request.Headers[HttpRequestExtensions.ApiKeyHeader].ToString();
        if (!string.IsNullOrEmpty(apiKey))
        {
            var tokenResult = await _feedAuthService.AuthenticateByTokenAsync(apiKey, Context.RequestAborted);
            if (tokenResult.IsAuthenticated)
            {
                Logger.LogInformation("Audit: {EventType} - User {Username} ({UserId}) authenticated via API key from {IP}",
                    "LoginSuccess", tokenResult.Username, tokenResult.UserId, Context.Connection.RemoteIpAddress?.ToString());
                return await CreateUserAuthenticationResult(tokenResult.Username, tokenResult.UserId?.ToString());
            }
        }

        if (!Request.Headers.TryGetValue("Authorization", out var auth))
            return AuthenticateResult.NoResult();

        string username;
        string password;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(auth);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split([':'], 2);
            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
            return AuthenticateResult.Fail("Invalid Authorization Header");
        }

        var result = await _feedAuthService.AuthenticateByCredentialsAsync(
            username, password, Context.RequestAborted);

        if (!result.IsAuthenticated)
        {
            var failIp = Context.Connection.RemoteIpAddress?.ToString();
            Logger.LogWarning("Audit: {EventType} - Authentication failed for {Username} from {IP}",
                "LoginFailure", username, failIp);
            return AuthenticateResult.Fail("Invalid Username or Password");
        }

        var ip = Context.Connection.RemoteIpAddress?.ToString();
        Logger.LogInformation("Audit: {EventType} - User {Username} ({UserId}) authenticated from {IP}",
            "LoginSuccess", result.Username, result.UserId, ip);

        return await CreateUserAuthenticationResult(result.Username, result.UserId?.ToString());
    }

    private Task<AuthenticateResult> CreateAnonymousAuthenticationResult()
    {
        Claim[] claims = [new Claim(ClaimTypes.Anonymous, string.Empty)];
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private Task<AuthenticateResult> CreateUserAuthenticationResult(string username, string userId)
    {
        var claims = new System.Collections.Generic.List<Claim>
        {
            new(ClaimTypes.Name, username)
        };

        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsOpenAccessAllowed()
    {
        return _bagetterOptions.Value.Authentication is null ||
            _bagetterOptions.Value.Authentication.Credentials is null ||
            _bagetterOptions.Value.Authentication.Credentials.Length == 0 ||
            _bagetterOptions.Value.Authentication.Credentials.All(a => string.IsNullOrWhiteSpace(a.Username) && string.IsNullOrWhiteSpace(a.Password));
    }

    private bool ValidateStaticCredentials(string username, string password)
    {
        return _bagetterOptions.Value.Authentication.Credentials.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && a.Password == password);
    }
}
