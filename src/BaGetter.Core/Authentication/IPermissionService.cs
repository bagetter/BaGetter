using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;

namespace BaGetter.Core.Authentication;

public interface IPermissionService
{
    Task<bool> CanPushAsync(Guid userId, Guid feedId, CancellationToken cancellationToken);
    Task<bool> CanPullAsync(Guid userId, Guid feedId, CancellationToken cancellationToken);
    Task<bool> CanDeleteAsync(Guid userId, Guid feedId, CancellationToken cancellationToken);
    Task<FeedPermission> GetPermissionAsync(Guid principalId, PrincipalType principalType, Guid feedId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeedPermission>> GetPermissionsByPrincipalTypeAsync(PrincipalType principalType, CancellationToken cancellationToken);
    Task GrantPermissionAsync(Guid principalId, PrincipalType principalType, Guid feedId, bool canPush, bool canPull, CancellationToken cancellationToken, PermissionSource source = PermissionSource.Manual, bool canDelete = false);
    Task RevokePermissionAsync(Guid permissionId, CancellationToken cancellationToken);
    Task RevokePermissionsBySourceAsync(Guid principalId, PrincipalType principalType, Guid feedId, PermissionSource source, CancellationToken cancellationToken);
}
