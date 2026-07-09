using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Authentication;

/// <summary>
/// Handles user provisioning and App Role-based group synchronization on OIDC token validation.
/// </summary>
public class EntraRoleSyncService
{
    private const string AdminRoleValue = "Admin";

    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ILogger<EntraRoleSyncService> _logger;

    public EntraRoleSyncService(
        IUserService userService,
        IGroupService groupService,
        IOptions<BaGetterOptions> options,
        ILogger<EntraRoleSyncService> logger)
    {
        _userService = userService;
        _groupService = groupService;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Provisions or updates a user from Entra ID claims, syncs admin status and
    /// group memberships based on App Roles in the token.
    /// Called from the OnTokenValidated OIDC event.
    /// </summary>
    public async Task OnTokenValidatedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var oid = principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                  ?? principal.FindFirstValue("oid");

        if (string.IsNullOrEmpty(oid))
        {
            _logger.LogWarning("Entra ID token is missing the object identifier claim");
            return;
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("preferred_username");
        var displayName = principal.FindFirstValue(ClaimTypes.GivenName) is { } given
                          && principal.FindFirstValue(ClaimTypes.Surname) is { } surname
            ? $"{given} {surname}"
            : principal.FindFirstValue("name") ?? email ?? oid;
        var username = email ?? oid;

        // Upsert the user
        var user = await _userService.FindByEntraObjectIdAsync(oid, cancellationToken);
        if (user == null)
        {
            _logger.LogInformation("Provisioning new Entra user: {Username} (OID: {Oid})", username, oid);
            user = await _userService.CreateEntraUserAsync(oid, username, displayName, cancellationToken);
        }
        else
        {
            var changed = false;
            if (user.DisplayName != displayName) { user.DisplayName = displayName; changed = true; }
            if (user.Username != username) { user.Username = username; changed = true; }

            if (changed)
            {
                user.UpdatedAtUtc = DateTime.UtcNow;
                await _userService.UpdateUserAsync(user, cancellationToken);
            }
        }

        if (!user.IsEnabled)
        {
            _logger.LogWarning("Login denied: Entra user {Username} is disabled", user.Username);
            throw new UnauthorizedAccessException($"Account '{user.Username}' has been disabled.");
        }

        if (!user.CanLoginToUI)
        {
            _logger.LogWarning("Login denied: Entra user {Username} does not have UI login permission", user.Username);
            throw new UnauthorizedAccessException($"Account '{user.Username}' is not permitted to sign in to the web UI.");
        }

        // Read roles claim from the token
        var roleClaim = _options.Value.Authentication?.Entra?.RoleClaim ?? "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        var roles = principal.FindAll(roleClaim).Select(c => c.Value).ToList();

        // Bidirectional admin sync: grant or revoke based on token
        var hasAdminRole = roles.Contains(AdminRoleValue, StringComparer.OrdinalIgnoreCase);
        if (hasAdminRole != user.IsAdmin)
        {
            _logger.LogInformation(
                "{Action} admin for Entra user {Username} based on App Role",
                hasAdminRole ? "Granting" : "Revoking", user.Username);
            await _userService.SetAdminAsync(user.Id, hasAdminRole, cancellationToken);
        }

        // Sync group memberships from App Roles (full bidirectional reconciliation)
        await _groupService.SyncAppRoleMembershipsAsync(user.Id, roles, cancellationToken);

        // Add BaGetter-specific claims to the principal
        var identity = principal.Identity as ClaimsIdentity;
        if (identity != null)
        {
            var existing = identity.FindAll(ClaimTypes.NameIdentifier).ToList();
            foreach (var c in existing)
                identity.RemoveClaim(c);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim("auth_provider", AuthProvider.Entra.ToString()));
            identity.AddClaim(new Claim(AuthenticationConstants.IsAdminClaim, user.IsAdmin ? "true" : "false"));
        }
    }
}
