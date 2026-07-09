using System;
using System.Collections.Generic;
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

public class GroupServiceTests
{
    public class FindByIdAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenNotFound()
        {
            var result = await Target.FindByIdAsync(Guid.NewGuid(), Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsGroupById()
        {
            var group = await Target.CreateGroupAsync("TestGroup", null, "desc", Ct);

            var result = await Target.FindByIdAsync(group.Id, Ct);

            Assert.NotNull(result);
            Assert.Equal("TestGroup", result.Name);
        }
    }

    public class FindByNameAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenNotFound()
        {
            var result = await Target.FindByNameAsync("nonexistent", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsGroupByName()
        {
            await Target.CreateGroupAsync("MyGroup", null, "desc", Ct);

            var result = await Target.FindByNameAsync("MyGroup", Ct);

            Assert.NotNull(result);
            Assert.Equal("MyGroup", result.Name);
        }
    }

    public class FindByAppRoleValueAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsNullWhenNotFound()
        {
            var result = await Target.FindByAppRoleValueAsync("no-such-role", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsGroupByAppRoleValue()
        {
            await Target.CreateGroupAsync("RoleGroup", "TeamFrontend", "desc", Ct);

            var result = await Target.FindByAppRoleValueAsync("TeamFrontend", Ct);

            Assert.NotNull(result);
            Assert.Equal("RoleGroup", result.Name);
        }
    }

    public class CreateGroupAsync : FactsBase
    {
        [Fact]
        public async Task CreatesGroupWithCorrectProperties()
        {
            var result = await Target.CreateGroupAsync("NewGroup", "TeamFrontend", "A description", Ct);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("NewGroup", result.Name);
            Assert.Equal("TeamFrontend", result.AppRoleValue);
            Assert.Equal("A description", result.Description);
            Assert.True(result.CreatedAtUtc <= DateTime.UtcNow);
        }
    }

    public class GetAllGroupsAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsEmptyWhenNoGroups()
        {
            var result = await Target.GetAllGroupsAsync(Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsAllGroups()
        {
            await Target.CreateGroupAsync("Group1", null, null, Ct);
            await Target.CreateGroupAsync("Group2", null, null, Ct);

            var result = await Target.GetAllGroupsAsync(Ct);

            Assert.Equal(2, result.Count);
        }
    }

    public class AddUserToGroupAsync : FactsBase
    {
        [Fact]
        public async Task AddsUserToGroup()
        {
            var user = await CreateUser("member");
            var group = await Target.CreateGroupAsync("GroupA", null, null, Ct);

            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Single(groups);
            Assert.Equal("GroupA", groups[0].Name);
        }

        [Fact]
        public async Task DoesNotDuplicateMembership()
        {
            var user = await CreateUser("nodupe");
            var group = await Target.CreateGroupAsync("GroupB", null, null, Ct);

            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);
            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Single(groups);
        }

        [Fact]
        public async Task ThrowsWhenAddingEntraUserToRoleLinkedGroup()
        {
            var user = await CreateUser("entrablock");
            var group = await Target.CreateGroupAsync("RoleGroup", "TeamFrontend", null, Ct);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.AddUserToGroupAsync(user.Id, group.Id, Ct));
        }

        [Fact]
        public async Task AllowsAddingLocalUserToRoleLinkedGroup()
        {
            var user = await CreateLocalUser("localok");
            var group = await Target.CreateGroupAsync("RoleGroup", "TeamBackend", null, Ct);

            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Single(groups);
        }
    }

    public class RemoveUserFromGroupAsync : FactsBase
    {
        [Fact]
        public async Task RemovesUserFromGroup()
        {
            var user = await CreateUser("removable");
            var group = await Target.CreateGroupAsync("GroupC", null, null, Ct);

            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);
            await Target.RemoveUserFromGroupAsync(user.Id, group.Id, Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Empty(groups);
        }

        [Fact]
        public async Task ThrowsWhenRemovingEntraUserFromRoleLinkedGroup()
        {
            var user = await CreateUser("entraremoveblock");
            var group = await Target.CreateGroupAsync("RoleGroupR", "TeamFrontend", null, Ct);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Target.RemoveUserFromGroupAsync(user.Id, group.Id, Ct));
        }

        [Fact]
        public async Task DoesNothingWhenNotAMember()
        {
            var user = await CreateUser("notmember");
            var group = await Target.CreateGroupAsync("GroupD", null, null, Ct);

            await Target.RemoveUserFromGroupAsync(user.Id, group.Id, Ct);

            // No exception should be thrown
        }
    }

    public class GetUserGroupsAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsEmptyWhenUserHasNoGroups()
        {
            var user = await CreateUser("lonely");

            var result = await Target.GetUserGroupsAsync(user.Id, Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsMultipleGroups()
        {
            var user = await CreateUser("multi");
            var group1 = await Target.CreateGroupAsync("G1", null, null, Ct);
            var group2 = await Target.CreateGroupAsync("G2", null, null, Ct);

            await Target.AddUserToGroupAsync(user.Id, group1.Id, Ct);
            await Target.AddUserToGroupAsync(user.Id, group2.Id, Ct);

            var result = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Equal(2, result.Count);
        }
    }

    public class SyncAppRoleMembershipsAsync : FactsBase
    {
        [Fact]
        public async Task AddsNewGroupMemberships()
        {
            var user = await CreateUser("syncuser");
            var group = await Target.CreateGroupAsync("RoleSync", "TeamFrontend", null, Ct);

            await Target.SyncAppRoleMembershipsAsync(
                user.Id, new List<string> { "TeamFrontend" }, Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Single(groups);
            Assert.Equal("RoleSync", groups[0].Name);
        }

        [Fact]
        public async Task RemovesOldGroupMemberships()
        {
            var user = await CreateUser("syncremove");
            var group = await Target.CreateGroupAsync("OldGroup", "TeamBackend", null, Ct);

            // Add membership directly (bypassing service-layer guard, simulating prior sync)
            Context.UserGroups.Add(new UserGroup { UserId = user.Id, GroupId = group.Id });
            await Context.SaveChangesAsync(Ct);

            // Sync with empty list removes the role-linked group membership
            await Target.SyncAppRoleMembershipsAsync(
                user.Id, new List<string>(), Ct);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Empty(groups);
        }

        [Fact]
        public async Task SkipsUnknownAppRoleValue()
        {
            var user = await CreateUser("skipunknown");

            await Target.SyncAppRoleMembershipsAsync(
                user.Id, new List<string> { "UnknownRole" }, Ct);

            var createdGroup = await Target.FindByAppRoleValueAsync("UnknownRole", Ct);
            Assert.Null(createdGroup);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Empty(groups);
        }
    }

    public class IsRoleLinkedGroupAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsTrueForRoleLinkedGroup()
        {
            var group = await Target.CreateGroupAsync("RoleGroup", "TeamFrontend", null, Ct);

            var result = await Target.IsRoleLinkedGroupAsync(group.Id, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsFalseForManualGroup()
        {
            var group = await Target.CreateGroupAsync("ManualGroup", null, null, Ct);

            var result = await Target.IsRoleLinkedGroupAsync(group.Id, Ct);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsFalseForNonExistentGroup()
        {
            var result = await Target.IsRoleLinkedGroupAsync(Guid.NewGuid(), Ct);

            Assert.False(result);
        }
    }

    public class CanManuallyModifyMembershipAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsFalseForEntraUserOnRoleLinkedGroup()
        {
            var user = await CreateUser("entrauser");
            var group = await Target.CreateGroupAsync("RoleGroup", "TeamFrontend", null, Ct);

            var result = await Target.CanManuallyModifyMembershipAsync(group.Id, user.Id, Ct);

            Assert.False(result);
        }

        [Fact]
        public async Task ReturnsTrueForLocalUserOnRoleLinkedGroup()
        {
            var user = await CreateLocalUser("localuser");
            var group = await Target.CreateGroupAsync("RoleGroup", "TeamBackend", null, Ct);

            var result = await Target.CanManuallyModifyMembershipAsync(group.Id, user.Id, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsTrueForEntraUserOnManualGroup()
        {
            var user = await CreateUser("entramanual");
            var group = await Target.CreateGroupAsync("ManualGroup", null, null, Ct);

            var result = await Target.CanManuallyModifyMembershipAsync(group.Id, user.Id, Ct);

            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsTrueForLocalUserOnManualGroup()
        {
            var user = await CreateLocalUser("localmanual");
            var group = await Target.CreateGroupAsync("ManualGroup", null, null, Ct);

            var result = await Target.CanManuallyModifyMembershipAsync(group.Id, user.Id, Ct);

            Assert.True(result);
        }
    }

    public class DeleteGroupAsync : FactsBase
    {
        [Fact]
        public async Task DoesNothingWhenGroupNotFound()
        {
            // Should complete without throwing
            await Target.DeleteGroupAsync(Guid.NewGuid(), Ct);
        }

        [Fact]
        public async Task RemovesGroupAndMemberships()
        {
            var user = await CreateUser("deletemember");
            var group = await Target.CreateGroupAsync("ToDelete", null, null, Ct);
            await Target.AddUserToGroupAsync(user.Id, group.Id, Ct);

            await Target.DeleteGroupAsync(group.Id, Ct);

            var found = await Target.FindByIdAsync(group.Id, Ct);
            Assert.Null(found);

            var groups = await Target.GetUserGroupsAsync(user.Id, Ct);
            Assert.Empty(groups);
        }

        [Fact]
        public async Task RemovesFeedPermissionsForGroup()
        {
            var group = await Target.CreateGroupAsync("PermGroup", null, null, Ct);

            Context.FeedPermissions.Add(new BaGetter.Core.Entities.FeedPermission
            {
                Id = Guid.NewGuid(),
                FeedId = new Guid("00000000-0000-0000-0000-000000000011"),
                PrincipalType = BaGetter.Core.Entities.PrincipalType.Group,
                PrincipalId = group.Id,
                CanPull = true
            });
            await Context.SaveChangesAsync(Ct);

            await Target.DeleteGroupAsync(group.Id, Ct);

            var permCount = await Context.FeedPermissions
                .CountAsync(fp => fp.PrincipalId == group.Id, Ct);
            Assert.Equal(0, permCount);
        }

        [Fact]
        public async Task DoesNotRemoveOtherGroupsOrPermissions()
        {
            var group1 = await Target.CreateGroupAsync("Keep", null, null, Ct);
            var group2 = await Target.CreateGroupAsync("Delete", null, null, Ct);

            Context.FeedPermissions.Add(new BaGetter.Core.Entities.FeedPermission
            {
                Id = Guid.NewGuid(),
                FeedId = new Guid("00000000-0000-0000-0000-000000000012"),
                PrincipalType = BaGetter.Core.Entities.PrincipalType.Group,
                PrincipalId = group1.Id,
                CanPull = true
            });
            await Context.SaveChangesAsync(Ct);

            await Target.DeleteGroupAsync(group2.Id, Ct);

            var kept = await Target.FindByIdAsync(group1.Id, Ct);
            Assert.NotNull(kept);

            var permCount = await Context.FeedPermissions
                .CountAsync(fp => fp.PrincipalId == group1.Id, Ct);
            Assert.Equal(1, permCount);
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly GroupService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;

        protected FactsBase()
        {
            Context = TestDbContext.Create();

            // Seed feeds referenced in DeleteGroupAsync tests to satisfy FK constraints.
            Context.Feeds.AddRange(
                new Feed { Id = new Guid("00000000-0000-0000-0000-000000000011"), Slug = "feed-11", Name = "Feed 11", MirrorEnabled = false, MirrorLegacy = false, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
                new Feed { Id = new Guid("00000000-0000-0000-0000-000000000012"), Slug = "feed-12", Name = "Feed 12", MirrorEnabled = false, MirrorLegacy = false, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
            Context.SaveChanges();

            Target = new GroupService(
                Context,
                Mock.Of<ILogger<GroupService>>());
        }

        protected async Task<User> CreateUser(string username)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = $"{username} Display",
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = $"oid-{username}",
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync(Ct);
            return user;
        }

        protected async Task<User> CreateLocalUser(string username)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = $"{username} Display",
                AuthProvider = AuthProvider.Local,
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            Context.Users.Add(user);
            await Context.SaveChangesAsync(Ct);
            return user;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
