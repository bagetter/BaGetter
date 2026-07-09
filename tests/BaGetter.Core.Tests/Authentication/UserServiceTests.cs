using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Authentication;

public class UserServiceTests
{
    public class FindByUsernameAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenUserDoesNotExist()
        {
            var result = await Target.FindByUsernameAsync("nonexistent", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsUserWhenExists()
        {
            var user = await CreateLocalUser("testuser");

            var result = await Target.FindByUsernameAsync("testuser", Ct);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal("testuser", result.Username);
        }
    }

    public class FindByIdAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenNotFound()
        {
            var result = await Target.FindByIdAsync(Guid.NewGuid(), Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsUserById()
        {
            var user = await CreateLocalUser("findme");

            var result = await Target.FindByIdAsync(user.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal("findme", result.Username);
        }
    }

    public class FindByEntraObjectIdAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenNotFound()
        {
            var result = await Target.FindByEntraObjectIdAsync("not-an-oid", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsEntraUser()
        {
            var user = await Target.CreateEntraUserAsync(
                "oid-123", "entrauser", "Entra User", Ct);

            var result = await Target.FindByEntraObjectIdAsync("oid-123", Ct);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(AuthProvider.Entra, result.AuthProvider);
        }
    }

    public class CreateEntraUserAsync : FactsBase
    {
        [Fact]
        public async Task CreatesEntraUserWithCorrectProperties()
        {
            var result = await Target.CreateEntraUserAsync(
                "oid-abc", "entrauser", "Entra Display", Ct);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("entrauser", result.Username);
            Assert.Equal("Entra Display", result.DisplayName);
            Assert.Equal(AuthProvider.Entra, result.AuthProvider);
            Assert.Equal("oid-abc", result.EntraObjectId);
            Assert.True(result.IsEnabled);
            Assert.True(result.CanLoginToUI);
            Assert.Null(result.PasswordHash);
        }
    }

    public class CreateLocalUserAsync : FactsBase
    {
        [Fact]
        public async Task CreatesLocalUserWithHashedPassword()
        {
            var result = await Target.CreateLocalUserAsync(
                "localuser", "Local User", "MyPassword123!", true, AdminUserId, Ct);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("localuser", result.Username);
            Assert.Equal(AuthProvider.Local, result.AuthProvider);
            Assert.True(result.IsEnabled);
            Assert.True(result.CanLoginToUI);
            Assert.NotNull(result.PasswordHash);
            Assert.NotEqual("MyPassword123!", result.PasswordHash);
            Assert.Equal(AdminUserId, result.CreatedByUserId);
        }
    }

    public class VerifyPasswordAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsTrueForCorrectPassword()
        {
            var user = await CreateLocalUser("pwduser", "CorrectPassword");

            var result = await Target.VerifyPasswordAsync(user, "CorrectPassword");

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsFalseForIncorrectPassword()
        {
            var user = await CreateLocalUser("pwduser", "CorrectPassword");

            var result = await Target.VerifyPasswordAsync(user, "WrongPassword");

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalseWhenNoPasswordHashSet()
        {
            var user = await Target.CreateEntraUserAsync(
                "oid-1", "entrauser", "Entra", Ct);

            var result = await Target.VerifyPasswordAsync(user, "anything");

            Assert.False(result);
        }
    }

    public class RecordFailedLoginAsync : FactsBase
    {
        [Fact]
        public async Task IncrementsFailedLoginCount()
        {
            var user = await CreateLocalUser("failuser");

            await Target.RecordFailedLoginAsync(user.Id, Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.Equal(1, updated.FailedLoginCount);
        }

        [Fact]
        public async Task LocksOutAfterMaxAttempts()
        {
            var user = await CreateLocalUser("lockuser");

            for (var i = 0; i < 5; i++)
            {
                await Target.RecordFailedLoginAsync(user.Id, Ct);
            }

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.Equal(5, updated.FailedLoginCount);
            Assert.NotNull(updated.LockedUntilUtc);
            Assert.True(updated.LockedUntilUtc > DateTime.UtcNow);
        }
    }

    public class IsLockedOutAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsFalseWhenNotLocked()
        {
            var user = await CreateLocalUser("notlocked");

            var result = await Target.IsLockedOutAsync(user);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrueWhenLockedInFuture()
        {
            var user = await CreateLocalUser("locked");
            user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(10);

            var result = await Target.IsLockedOutAsync(user);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsFalseWhenLockExpired()
        {
            var user = await CreateLocalUser("expired");
            user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(-1);

            var result = await Target.IsLockedOutAsync(user);

            Assert.False(result);
        }
    }

    public class ResetFailedLoginCountAsync : FactsBase
    {
        [Fact]
        public async Task ResetsCountAndLock()
        {
            var user = await CreateLocalUser("resetuser");

            // Simulate some failures and lockout
            for (var i = 0; i < 5; i++)
            {
                await Target.RecordFailedLoginAsync(user.Id, Ct);
            }

            await Target.ResetFailedLoginCountAsync(user.Id, Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.Equal(0, updated.FailedLoginCount);
            Assert.Null(updated.LockedUntilUtc);
        }
    }

    public class SetPasswordAsync : FactsBase
    {
        [Fact]
        public async Task UpdatesPassword()
        {
            var user = await CreateLocalUser("setpwd", "OldPassword");

            await Target.SetPasswordAsync(user.Id, "NewPassword", Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.True(await Target.VerifyPasswordAsync(updated, "NewPassword"));
            Assert.False(await Target.VerifyPasswordAsync(updated, "OldPassword"));
        }

        [Fact]
        public async Task ThrowsWhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.SetPasswordAsync(Guid.NewGuid(), "pwd", Ct));
        }
    }

    public class SetEnabledAsync : FactsBase
    {
        [Fact]
        public async Task DisablesUser()
        {
            var user = await CreateLocalUser("disableme");
            Assert.True(user.IsEnabled);

            await Target.SetEnabledAsync(user.Id, false, Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.False(updated.IsEnabled);
        }

        [Fact]
        public async Task ThrowsWhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.SetEnabledAsync(Guid.NewGuid(), false, Ct));
        }
    }

    public class SetCanLoginToUIAsync : FactsBase
    {
        [Fact]
        public async Task RevokesWebAccess()
        {
            var user = await CreateLocalUser("revokeui");
            Assert.True(user.CanLoginToUI);

            await Target.SetCanLoginToUIAsync(user.Id, false, Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.False(updated.CanLoginToUI);
        }

        [Fact]
        public async Task GrantsWebAccess()
        {
            var user = await CreateLocalUser("grantui");
            // Revoke first, then grant
            await Target.SetCanLoginToUIAsync(user.Id, false, Ct);
            var revoked = await Target.FindByIdAsync(user.Id, Ct);
            Assert.False(revoked.CanLoginToUI);

            await Target.SetCanLoginToUIAsync(user.Id, true, Ct);

            var updated = await Target.FindByIdAsync(user.Id, Ct);
            Assert.True(updated.CanLoginToUI);
        }

        [Fact]
        public async Task ThrowsWhenUserNotFound()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.SetCanLoginToUIAsync(Guid.NewGuid(), false, Ct));
        }
    }

    public class GetAllUsersAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsPreSeededAdminUser()
        {
            var result = await Target.GetAllUsersAsync(Ct);

            Assert.Single(result);
        }

        [Fact]
        public async Task ReturnsAllUsers()
        {
            await CreateLocalUser("user1");
            await CreateLocalUser("user2");

            var result = await Target.GetAllUsersAsync(Ct);

            // 2 local users + 1 pre-seeded admin
            Assert.Equal(3, result.Count);
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly UserService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;
        protected readonly Guid AdminUserId;

        protected FactsBase()
        {
            Context = TestDbContext.Create();

            // Create an admin user to serve as CreatedByUser for local accounts
            AdminUserId = Guid.NewGuid();
            Context.Users.Add(new User
            {
                Id = AdminUserId,
                Username = "admin",
                DisplayName = "Admin",
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = "oid-admin",
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            Context.SaveChanges();

            var authOptions = new NugetAuthenticationOptions
            {
                MaxFailedAttempts = 5,
                LockoutMinutes = 15
            };

            var optionsSnapshot = new Mock<IOptionsSnapshot<NugetAuthenticationOptions>>();
            optionsSnapshot.Setup(o => o.Value).Returns(authOptions);

            Target = new UserService(
                Context,
                optionsSnapshot.Object,
                Mock.Of<ILogger<UserService>>());
        }

        protected async Task<User> CreateLocalUser(string username, string password = "TestPassword123!")
        {
            return await Target.CreateLocalUserAsync(
                username, $"{username} Display",
                password, true, AdminUserId, Ct);
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
