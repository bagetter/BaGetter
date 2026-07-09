using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BaGetter.Core.Authentication;

public class PermissionService : IPermissionService
{
    private readonly IContext _context;
    private readonly IUserService _userService;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IContext context, IUserService userService, ILogger<PermissionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CanPushAsync(Guid userId, Guid feedId, CancellationToken cancellationToken)
    {
        return await HasPermissionAsync(userId, feedId, p => p.CanPush, cancellationToken);
    }

    public async Task<bool> CanPullAsync(Guid userId, Guid feedId, CancellationToken cancellationToken)
    {
        return await HasPermissionAsync(userId, feedId, p => p.CanPull, cancellationToken);
    }

    public async Task<bool> CanDeleteAsync(Guid userId, Guid feedId, CancellationToken cancellationToken)
    {
        return await HasPermissionAsync(userId, feedId, p => p.CanDelete, cancellationToken);
    }

    public async Task<FeedPermission> GetPermissionAsync(
        Guid principalId,
        PrincipalType principalType,
        Guid feedId,
        CancellationToken cancellationToken)
    {
        return await _context.FeedPermissions.FirstOrDefaultAsync(
            p => p.FeedId == feedId && p.PrincipalType == principalType && p.PrincipalId == principalId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<FeedPermission>> GetPermissionsByPrincipalTypeAsync(
        PrincipalType principalType,
        CancellationToken cancellationToken)
    {
        return await _context.FeedPermissions
            .Where(p => p.PrincipalType == principalType)
            .ToListAsync(cancellationToken);
    }

    public async Task GrantPermissionAsync(
        Guid principalId,
        PrincipalType principalType,
        Guid feedId,
        bool canPush,
        bool canPull,
        CancellationToken cancellationToken,
        PermissionSource source = PermissionSource.Manual,
        bool canDelete = false)
    {
        var existing = await _context.FeedPermissions.FirstOrDefaultAsync(
            p => p.FeedId == feedId && p.PrincipalType == principalType && p.PrincipalId == principalId,
            cancellationToken);

        if (existing != null)
        {
            existing.CanPush = canPush;
            existing.CanPull = canPull;
            existing.CanDelete = canDelete;
            existing.Source = source;
        }
        else
        {
            _context.FeedPermissions.Add(new FeedPermission
            {
                Id = Guid.NewGuid(),
                FeedId = feedId,
                PrincipalType = principalType,
                PrincipalId = principalId,
                CanPush = canPush,
                CanPull = canPull,
                CanDelete = canDelete,
                Source = source
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Audit: {EventType} - Granted permission on feed {FeedId} to {PrincipalType} {PrincipalId}: Push={CanPush}, Pull={CanPull}, Delete={CanDelete}, Source={Source}",
            "PermissionGranted", feedId, principalType, principalId, canPush, canPull, canDelete, source);
    }

    public async Task RevokePermissionsBySourceAsync(
        Guid principalId,
        PrincipalType principalType,
        Guid feedId,
        PermissionSource source,
        CancellationToken cancellationToken)
    {
        var permissions = await _context.FeedPermissions
            .Where(p => p.FeedId == feedId
                     && p.PrincipalType == principalType
                     && p.PrincipalId == principalId
                     && p.Source == source)
            .ToListAsync(cancellationToken);

        if (permissions.Count == 0) return;

        _context.FeedPermissions.RemoveRange(permissions);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit: {EventType} - Revoked {Count} {Source} permissions on feed {FeedId} for {PrincipalType} {PrincipalId}",
            "PermissionRevoked", permissions.Count, source, feedId, principalType, principalId);
    }

    public async Task RevokePermissionAsync(Guid permissionId, CancellationToken cancellationToken)
    {
        var permission = await _context.FeedPermissions
            .FirstOrDefaultAsync(p => p.Id == permissionId, cancellationToken);

        if (permission == null) return;

        _context.FeedPermissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Revoked permission {PermissionId}",
            "PermissionRevoked", permissionId);
    }

    private async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid feedId,
        System.Linq.Expressions.Expression<Func<FeedPermission, bool>> permissionSelector,
        CancellationToken cancellationToken)
    {
        // Admins always have permission
        if (await _userService.IsAdminAsync(userId, cancellationToken))
            return true;

        // Check direct user permissions
        var hasDirectPermission = await _context.FeedPermissions
            .Where(p => p.FeedId == feedId
                     && p.PrincipalType == PrincipalType.User
                     && p.PrincipalId == userId)
            .AnyAsync(permissionSelector, cancellationToken);

        if (hasDirectPermission) return true;

        // Check group permissions via user's group memberships
        var userGroupIds = _context.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId);

        var hasGroupPermission = await _context.FeedPermissions
            .Where(p => p.FeedId == feedId
                     && p.PrincipalType == PrincipalType.Group
                     && userGroupIds.Contains(p.PrincipalId))
            .AnyAsync(permissionSelector, cancellationToken);

        return hasGroupPermission;
    }
}
