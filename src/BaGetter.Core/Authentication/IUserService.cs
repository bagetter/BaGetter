using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;

namespace BaGetter.Core.Authentication;

public interface IUserService
{
    Task<User> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<User> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<User> FindByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken);
    Task<User> CreateEntraUserAsync(string entraObjectId, string username, string displayName, CancellationToken cancellationToken);
    Task<User> CreateLocalUserAsync(string username, string displayName, string password, bool canLoginToUI, Guid? createdByUserId, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
    Task<bool> VerifyPasswordAsync(User user, string password);
    Task RecordFailedLoginAsync(Guid userId, CancellationToken cancellationToken);
    Task ResetFailedLoginCountAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsLockedOutAsync(User user);
    Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken);
    Task SetEnabledAsync(Guid userId, bool isEnabled, CancellationToken cancellationToken);
    Task SetCanLoginToUIAsync(Guid userId, bool canLoginToUI, CancellationToken cancellationToken);
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken);
    Task SetAdminAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken);
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);
}
