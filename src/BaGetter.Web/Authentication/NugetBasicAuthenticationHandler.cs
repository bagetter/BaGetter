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
using System.Collections.Generic;
using System.Threading;

namespace BaGetter.Web.Authentication;

public class NugetBasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptions<BaGetterOptions> bagetterOptions;
    private readonly IReadOnlyList<INugetCredentialValidator> credentialValidators;

    public NugetBasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BaGetterOptions> bagetterOptions,
        IEnumerable<INugetCredentialValidator> credentialValidators)
        : base(options, logger, encoder)
    {
        this.bagetterOptions = bagetterOptions;
        this.credentialValidators = credentialValidators?.ToArray() ?? [];
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (IsAnonymousAllowed())
        {
            return await CreateAnonymousAuthenticatonResult();
        }

        if (!Request.Headers.TryGetValue("Authorization", out var auth))
            return AuthenticateResult.NoResult();

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
            return AuthenticateResult.Fail("Invalid Authorization Header");
        }

        var validationResult = await ValidateCredentialsAsync(username, password, Context.RequestAborted);
        if (validationResult == null)
            return AuthenticateResult.Fail("Invalid Username or Password");

        return await CreateUserAuthenticatonResult(validationResult);
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

    private Task<AuthenticateResult> CreateUserAuthenticatonResult(NugetCredentialValidationResult validationResult)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, validationResult.Username)
        };

        foreach (var role in validationResult.Roles ?? [])
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool IsAnonymousAllowed()
    {
        var auth = bagetterOptions.Value.Authentication;
        var hasConfiguredCredentials = auth?.Credentials?.Any(a =>
                !string.IsNullOrWhiteSpace(a.Username) &&
                (!string.IsNullOrWhiteSpace(a.Password) || !string.IsNullOrWhiteSpace(a.PasswordHash))) == true;
        var hasLdap = auth?.Ldap?.Enabled == true &&
                      !string.IsNullOrWhiteSpace(auth.Ldap.Server) &&
                      !string.IsNullOrWhiteSpace(auth.Ldap.BaseDn);

        return !hasConfiguredCredentials && !hasLdap;
    }

    private async Task<NugetCredentialValidationResult> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        foreach (var validator in credentialValidators)
        {
            var result = await validator.ValidateAsync(username, password, cancellationToken);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
