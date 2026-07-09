using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core.Authentication;

public class FeedAuthenticationService : IFeedAuthenticationService
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly NugetAuthenticationOptions _authOptions;
    private readonly ILogger<FeedAuthenticationService> _logger;

    public FeedAuthenticationService(
        IUserService userService,
        ITokenService tokenService,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions,
        ILogger<FeedAuthenticationService> logger)
    {
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentNullException.ThrowIfNull(authOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _userService = userService;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateByTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
            return new AuthResult(false, null, null);

        var pat = await _tokenService.ValidateTokenAsync(token, cancellationToken);
        if (pat == null)
        {
            _logger.LogWarning("Audit: {EventType} - Token authentication failed: invalid or expired token",
                "LoginFailure");
            return new AuthResult(false, null, null);
        }

        return new AuthResult(true, pat.UserId, pat.User.Username);
    }

    public async Task<AuthResult> AuthenticateByCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return new AuthResult(false, null, null);

        // First, try to authenticate as a local account
        if (_authOptions.Mode is AuthenticationMode.Local or AuthenticationMode.Hybrid)
        {
            var localResult = await TryAuthenticateLocalAccountAsync(username, password, cancellationToken);
            if (localResult.IsAuthenticated)
                return localResult;
        }

        // Then, try to authenticate using the password as a PAT
        // (NuGet clients send credentials as username/password in basic auth,
        //  where password is the PAT token)
        if (_authOptions.Mode is AuthenticationMode.Entra or AuthenticationMode.Hybrid)
        {
            var tokenResult = await AuthenticateByTokenAsync(password, cancellationToken);
            if (tokenResult.IsAuthenticated)
            {
                if (!string.Equals(tokenResult.Username, username, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Audit: {EventType} - PAT username mismatch: supplied {SuppliedUsername}, token belongs to {TokenUsername}",
                        "LoginFailure", username, tokenResult.Username);
                    return new AuthResult(false, null, null);
                }

                return tokenResult;
            }
        }

        _logger.LogWarning("Audit: {EventType} - Credential authentication failed for username {Username}",
            "LoginFailure", username);
        return new AuthResult(false, null, null);
    }

    private async Task<AuthResult> TryAuthenticateLocalAccountAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (user == null || user.AuthProvider != AuthProvider.Local)
            return new AuthResult(false, null, null);

        if (!user.IsEnabled)
        {
            _logger.LogWarning("Audit: {EventType} - Login attempt for disabled local account {Username} ({UserId})",
                "LoginFailure", username, user.Id);
            return new AuthResult(false, null, null);
        }

        if (await _userService.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Audit: {EventType} - Login attempt for locked out local account {Username} ({UserId})",
                "LoginFailure", username, user.Id);
            return new AuthResult(false, null, null);
        }

        var passwordValid = await _userService.VerifyPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userService.RecordFailedLoginAsync(user.Id, cancellationToken);
            _logger.LogWarning("Audit: {EventType} - Failed login attempt for local account {Username} ({UserId})",
                "LoginFailure", username, user.Id);
            return new AuthResult(false, null, null);
        }

        await _userService.ResetFailedLoginCountAsync(user.Id, cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Local account {Username} ({UserId}) authenticated successfully",
            "LoginSuccess", username, user.Id);

        return new AuthResult(true, user.Id, user.Username);
    }
}
