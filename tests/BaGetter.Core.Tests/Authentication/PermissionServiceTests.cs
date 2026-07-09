using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BaGetter.Core.Tests.Authentication;

public class PermissionServiceTests
{
    // Stable Guid constants used as feed IDs across tests
    private static readonly Guid _defaultFeedId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _feedAId = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid _feedBId = new("00000000-0000-0000-0000-000000000003");

    public class CanPushAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsFalseWhenNoPermissions()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "noperm");

            var result = await Target.CanPushAsync(userId, _defaultFeedId, Ct);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrueWhenUserHasDirectPushPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "pushuser");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: false, Ct);

            var result = await Target.CanPushAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsTrueWhenUserHasGroupPushPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "grouppush");
            var groupId = await CreateGroupWithUser(userId, "PushGroup");

            await Target.GrantPermissionAsync(
                groupId, PrincipalType.Group, _defaultFeedId,
                canPush: true, canPull: false, Ct);

            var result = await Target.CanPushAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsFalseForDifferentFeed()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "feedscope");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _feedAId,
                canPush: true, canPull: false, Ct);

            var result = await Target.CanPushAsync(userId, _feedBId, Ct);

            Assert.False(result);
        }
    }

    public class CanPullAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsFalseWhenNoPermissions()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "nopull");

            var result = await Target.CanPullAsync(userId, _defaultFeedId, Ct);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrueWhenUserHasPullPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "pulluser");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: false, canPull: true, Ct);

            var result = await Target.CanPullAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }
    }

    public class GrantPermissionAsync : FactsBase
    {
        [Fact]
        public async Task CreatesNewPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "grantee");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct);

            Assert.True(await Target.CanPushAsync(userId, _defaultFeedId, Ct));
            Assert.True(await Target.CanPullAsync(userId, _defaultFeedId, Ct));
        }

        [Fact]
        public async Task UpdatesExistingPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "updategrant");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: false, Ct);

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: false, canPull: true, Ct);

            Assert.False(await Target.CanPushAsync(userId, _defaultFeedId, Ct));
            Assert.True(await Target.CanPullAsync(userId, _defaultFeedId, Ct));
        }
    }

    public class CanDeleteAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsFalseWhenNoPermissions()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "nodelete");

            var result = await Target.CanDeleteAsync(userId, _defaultFeedId, Ct);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalseWhenOnlyPushAndPullGranted()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "pushpullnodelete");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct);

            Assert.False(await Target.CanDeleteAsync(userId, _defaultFeedId, Ct));
        }

        [Fact]
        public async Task ReturnsTrueWhenUserHasDirectDeletePermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "deleteuser");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: false, canPull: false, Ct,
                canDelete: true);

            Assert.True(await Target.CanDeleteAsync(userId, _defaultFeedId, Ct));
        }

        [Fact]
        public async Task ReturnsTrueWhenUserHasGroupDeletePermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "groupdelete");
            var groupId = await CreateGroupWithUser(userId, "DeleteGroup");

            await Target.GrantPermissionAsync(
                groupId, PrincipalType.Group, _defaultFeedId,
                canPush: false, canPull: false, Ct,
                canDelete: true);

            Assert.True(await Target.CanDeleteAsync(userId, _defaultFeedId, Ct));
        }
    }

    public class AdminBypass : FactsBase
    {
        [Fact]
        public async Task AdminCanPushWithoutExplicitPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "adminuser");
            UserService.Setup(s => s.IsAdminAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await Target.CanPushAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task AdminCanPullWithoutExplicitPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "adminpull");
            UserService.Setup(s => s.IsAdminAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await Target.CanPullAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task AdminCanDeleteWithoutExplicitPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "admindelete");
            UserService.Setup(s => s.IsAdminAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await Target.CanDeleteAsync(userId, _defaultFeedId, Ct);

            Assert.True(result);
        }
    }

    public class RevokePermissionAsync : FactsBase
    {
        [Fact]
        public async Task RevokesPermission()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "revokee");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct);

            var permission = await Context.FeedPermissions
                .FirstOrDefaultAsync(p => p.PrincipalId == userId, Ct);
            Assert.NotNull(permission);

            await Target.RevokePermissionAsync(permission.Id, Ct);

            Assert.False(await Target.CanPushAsync(userId, _defaultFeedId, Ct));
            Assert.False(await Target.CanPullAsync(userId, _defaultFeedId, Ct));
        }

        [Fact]
        public async Task DoesNothingWhenPermissionNotFound()
        {
            await Target.RevokePermissionAsync(Guid.NewGuid(), Ct);

            // No exception should be thrown
        }
    }

    public class GrantPermissionWithSourceAsync : FactsBase
    {
        [Fact]
        public async Task DefaultsToManualSource()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "defaultsource");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct);

            var perm = await Context.FeedPermissions
                .FirstOrDefaultAsync(p => p.PrincipalId == userId, Ct);
            Assert.NotNull(perm);
            Assert.Equal(PermissionSource.Manual, perm.Source);
        }

        [Fact]
        public async Task SetsEntraRoleSource()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "entrasource");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct,
                source: PermissionSource.EntraRole);

            var perm = await Context.FeedPermissions
                .FirstOrDefaultAsync(p => p.PrincipalId == userId, Ct);
            Assert.NotNull(perm);
            Assert.Equal(PermissionSource.EntraRole, perm.Source);
        }

        [Fact]
        public async Task UpdateExistingPermissionUpdatesSource()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "updatesource");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: false, Ct);

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct,
                source: PermissionSource.EntraRole);

            var perm = await Context.FeedPermissions
                .FirstOrDefaultAsync(p => p.PrincipalId == userId, Ct);
            Assert.Equal(PermissionSource.EntraRole, perm.Source);
        }
    }

    public class RevokePermissionsBySourceAsync : FactsBase
    {
        [Fact]
        public async Task RevokesOnlyMatchingSource()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "sourcerevoke");

            // Create manual permission on feed-a
            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _feedAId,
                canPush: true, canPull: true, Ct,
                source: PermissionSource.Manual);

            // Create EntraRole permission on feed-b
            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _feedBId,
                canPush: true, canPull: true, Ct,
                source: PermissionSource.EntraRole);

            // Revoke only EntraRole permissions on feed-b
            await Target.RevokePermissionsBySourceAsync(
                userId, PrincipalType.User, _feedBId, PermissionSource.EntraRole, Ct);

            // Manual permission on feed-a should still exist
            Assert.True(await Target.CanPushAsync(userId, _feedAId, Ct));
            // EntraRole permission on feed-b should be revoked
            Assert.False(await Target.CanPushAsync(userId, _feedBId, Ct));
        }

        [Fact]
        public async Task DoesNothingWhenNoMatchingPermissions()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "nomatch");

            await Target.RevokePermissionsBySourceAsync(
                userId, PrincipalType.User, _defaultFeedId, PermissionSource.EntraRole, Ct);

            // No exception
        }
    }

    public class HasPermissionWorksForBothSources : FactsBase
    {
        [Fact]
        public async Task EntraRolePermissionGrantsAccess()
        {
            var userId = Guid.NewGuid();
            await CreateUser(userId, "entraaccesstest");

            await Target.GrantPermissionAsync(
                userId, PrincipalType.User, _defaultFeedId,
                canPush: true, canPull: true, Ct,
                source: PermissionSource.EntraRole);

            Assert.True(await Target.CanPushAsync(userId, _defaultFeedId, Ct));
            Assert.True(await Target.CanPullAsync(userId, _defaultFeedId, Ct));
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly Mock<IUserService> UserService;
        protected readonly PermissionService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;

        protected FactsBase()
        {
            Context = TestDbContext.Create();

            // Seed the feeds referenced by tests so FK constraints are satisfied.
            Context.Feeds.AddRange(
                new Feed { Id = _defaultFeedId, Slug = "default", Name = "Default", MirrorEnabled = false, MirrorLegacy = false, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
                new Feed { Id = _feedAId, Slug = "feed-a", Name = "Feed A", MirrorEnabled = false, MirrorLegacy = false, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
                new Feed { Id = _feedBId, Slug = "feed-b", Name = "Feed B", MirrorEnabled = false, MirrorLegacy = false, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
            Context.SaveChanges();

            UserService = new Mock<IUserService>();
            Target = new PermissionService(
                Context,
                UserService.Object,
                Mock.Of<ILogger<PermissionService>>());
        }

        protected async Task CreateUser(Guid userId, string username)
        {
            Context.Users.Add(new User
            {
                Id = userId,
                Username = username,
                DisplayName = $"{username} Display",
                AuthProvider = AuthProvider.Local,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await Context.SaveChangesAsync(Ct);
        }

        protected async Task<Guid> CreateGroupWithUser(Guid userId, string groupName)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = groupName,
                CreatedAtUtc = DateTime.UtcNow
            };
            Context.Groups.Add(group);

            Context.UserGroups.Add(new UserGroup
            {
                UserId = userId,
                GroupId = group.Id
            });

            await Context.SaveChangesAsync(Ct);
            return group.Id;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
