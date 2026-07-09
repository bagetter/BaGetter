using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core.Authentication;

public class UserService : IUserService
{
    private const int BcryptWorkFactor = 12;

    private readonly IContext _context;
    private readonly NugetAuthenticationOptions _authOptions;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IContext context,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions,
        ILogger<UserService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(
            u => u.Username == username, cancellationToken);
    }

    public async Task<User> FindByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken)
    {
        return await _context.Users.FirstOrDefaultAsync(
            u => u.EntraObjectId == entraObjectId, cancellationToken);
    }

    public async Task<User> CreateEntraUserAsync(
        string entraObjectId,
        string username,
        string displayName,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            AuthProvider = AuthProvider.Entra,
            EntraObjectId = entraObjectId,
            IsEnabled = true,
            CanLoginToUI = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Created Entra user {Username} with ID {UserId}",
            "AccountCreated", username, user.Id);
        return user;
    }

    public async Task<User> CreateLocalUserAsync(
        string username,
        string displayName,
        string password,
        bool canLoginToUI,
        Guid? createdByUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName,
            AuthProvider = AuthProvider.Local,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor),
            IsEnabled = true,
            CanLoginToUI = canLoginToUI,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Created local user {Username} with ID {UserId} by {CreatedBy}",
            "AccountCreated", username, user.Id, createdByUserId);
        return user;
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BcryptWorkFactor);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Password updated for user {UserId}",
            "PasswordReset", userId);
    }

    public Task<bool> VerifyPasswordAsync(User user, string password)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrEmpty(user.PasswordHash)) return Task.FromResult(false);

        var result = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        return Task.FromResult(result);
    }

    public async Task RecordFailedLoginAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null) return;

        user.FailedLoginCount++;
        if (user.FailedLoginCount >= _authOptions.MaxFailedAttempts)
        {
            user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(_authOptions.LockoutMinutes);
            _logger.LogWarning("Audit: {EventType} - User {UserId} locked out until {LockedUntil} after {Attempts} failed attempts",
                "AccountLockedOut", userId, user.LockedUntilUtc, user.FailedLoginCount);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedLoginCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null) return;

        user.FailedLoginCount = 0;
        user.LockedUntilUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsLockedOutAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var isLocked = user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow;
        return Task.FromResult(isLocked);
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken cancellationToken)
    {
        return await _context.Users.ToListAsync(cancellationToken);
    }

    public async Task SetEnabledAsync(Guid userId, bool isEnabled, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found.");

        user.IsEnabled = isEnabled;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var eventType = isEnabled ? "AccountEnabled" : "AccountDisabled";
        _logger.LogInformation("Audit: {EventType} - User {UserId} enabled state set to {IsEnabled}",
            eventType, userId, isEnabled);
    }

    public async Task SetCanLoginToUIAsync(Guid userId, bool canLoginToUI, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found.");

        user.CanLoginToUI = canLoginToUI;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var eventType = canLoginToUI ? "UIAccessGranted" : "UIAccessRevoked";
        _logger.LogInformation("Audit: {EventType} - User {UserId} web UI access set to {CanLoginToUI}",
            eventType, userId, canLoginToUI);
    }

    public async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        return user?.IsAdmin == true;
    }

    public async Task SetAdminAsync(Guid userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found.");

        user.IsAdmin = isAdmin;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var eventType = isAdmin ? "AdminGranted" : "AdminRevoked";
        _logger.LogInformation("Audit: {EventType} - User {UserId} admin state set to {IsAdmin}",
            eventType, userId, isAdmin);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User {userId} not found.");

        if (user.IsEnabled)
            throw new InvalidOperationException($"User {userId} must be disabled before deletion.");

        var username = user.Username;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - User {Username} ({UserId}) was deleted",
            "AccountDeleted", username, userId);
    }
}
