using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Web.Pages.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests.Pages.Admin;

public class GroupsModelFacts
{
    public class OnPostSavePermissionsAsync : FactsBase
    {
        [Fact]
        public async Task GrantsRevokesAndSkipsEachRowInOnePost()
        {
            var groupId = Guid.NewGuid();
            var grantFeed = Guid.NewGuid();
            var revokeFeed = Guid.NewGuid();
            var emptyRow = Guid.Empty;

            var existing = new FeedPermission { Id = Guid.NewGuid() };
            _permissions
                .Setup(p => p.GetPermissionAsync(groupId, PrincipalType.Group, revokeFeed, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var input = new List<FeedPermissionInput>
            {
                new() { FeedId = grantFeed, CanPull = true, CanPush = true, CanDelete = true },
                new() { FeedId = revokeFeed, CanPull = false, CanPush = false, CanDelete = false },
                new() { FeedId = emptyRow, CanPull = true, CanPush = true },
            };

            var result = await _target.OnPostSavePermissionsAsync(groupId, input, CancellationToken.None);

            // Enabled row is granted, including the delete permission.
            _permissions.Verify(p => p.GrantPermissionAsync(
                groupId, PrincipalType.Group, grantFeed, true, true,
                It.IsAny<CancellationToken>(), It.IsAny<PermissionSource>(), true), Times.Once);

            // Unchecking pull, push and delete revokes the existing permission.
            _permissions.Verify(p => p.RevokePermissionAsync(existing.Id, It.IsAny<CancellationToken>()), Times.Once);

            // The empty-feed row is skipped entirely.
            _permissions.Verify(p => p.GrantPermissionAsync(
                groupId, PrincipalType.Group, emptyRow, It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), It.IsAny<PermissionSource>(), It.IsAny<bool>()), Times.Never);

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal(groupId, redirect.RouteValues["savedGroupId"]);
        }
    }

    public class FactsBase
    {
        protected readonly Mock<IGroupService> _groups = new();
        protected readonly Mock<IUserService> _users = new();
        protected readonly Mock<IPermissionService> _permissions = new();
        protected readonly Mock<IFeedService> _feeds = new();
        protected readonly GroupsModel _target;

        protected FactsBase()
        {
            var adminId = Guid.NewGuid();
            _users.Setup(u => u.IsAdminAsync(adminId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, adminId.ToString()) }, "TestAuth"));

            _target = new GroupsModel(_groups.Object, _users.Object, _permissions.Object, _feeds.Object)
            {
                PageContext = new PageContext(new ActionContext(
                    new DefaultHttpContext { User = principal },
                    new RouteData(),
                    new PageActionDescriptor())),
            };
        }
    }
}
