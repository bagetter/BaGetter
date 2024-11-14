using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using System;
using BaGetter.Core;
using System.Linq;

namespace BaGetter.Web.Authentication;

public class NugetBasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptions<BaGetterOptions> bagetterOptions;

    public NugetBasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BaGetterOptions> bagetterOptions)
        : base(options, logger, encoder)
    {
        this.bagetterOptions = bagetterOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (IsAnonymousAllowed())
        {
            return CreateAnonymousAuthenticatonResult();
        }

        if (!Request.Headers.TryGetValue("Authorization", out var auth))
            return Task.FromResult(AuthenticateResult.NoResult());

        string username = null;
        string password = null;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(auth);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split([':'], 2);
            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (!ValidateCredentials(username, password))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        return CreateUserAuthenticatonResult(username);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"NuGet Server\"";
        await base.HandleChallengeAsync(properties);
    }

    private Task<AuthenticateResult> CreateAnonymousAuthenticatonResult()
    {
        Claim[] claims = [new Claim(ClaimTypes.Anonymous, string.Empty)];
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private Task<AuthenticateResult> CreateUserAuthenticatonResult(string username)
    {
        Claim[] claims = [new Claim(ClaimTypes.Name, username)];
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsAnonymousAllowed()
    {
        return bagetterOptions.Value.Authentication is null ||
            bagetterOptions.Value.Authentication.Credentials is null ||
            bagetterOptions.Value.Authentication.Credentials.Length == 0 ||
            bagetterOptions.Value.Authentication.Credentials.All(a => string.IsNullOrWhiteSpace(a.Username) && string.IsNullOrWhiteSpace(a.Password));
    }

    private bool ValidateCredentials(string username, string password)
    {
        return bagetterOptions.Value.Authentication.Credentials.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && a.Password == password);
    }
}
