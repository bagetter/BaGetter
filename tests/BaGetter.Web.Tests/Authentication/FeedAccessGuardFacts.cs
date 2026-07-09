using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using BaGetter.Core.Feeds;
using BaGetter.Web.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BaGetter.Web.Tests.Authentication;

public class FeedAccessGuardFacts
{
    public class CanPushToCurrentFeedAsync
    {
        private readonly Mock<IPermissionService> _permissions = new();
        private readonly Mock<IFeedContext> _feedContext = new();
        private readonly Guid _feedId = Guid.NewGuid();
        private readonly CancellationToken _ct = CancellationToken.None;

        public CanPushToCurrentFeedAsync()
        {
            _feedContext.Setup(f => f.CurrentFeed).Returns(new Feed { Id = _feedId, Slug = "default" });
        }

        private static HttpContext UnauthenticatedContext()
            => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

        private static HttpContext AuthenticatedContext(params Claim[] claims)
            => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };

        private Task<bool> InvokeAsync(HttpContext context, AuthenticationMode mode)
            => FeedAccessGuard.CanPushToCurrentFeedAsync(
                context, _feedContext.Object, _permissions.Object, mode, _ct);

        [Fact]
        public async Task ConfigModeAlwaysAllows()
        {
            Assert.True(await InvokeAsync(UnauthenticatedContext(), AuthenticationMode.Config));
        }

        [Fact]
        public async Task UnauthenticatedIsDenied()
        {
            Assert.False(await InvokeAsync(UnauthenticatedContext(), AuthenticationMode.Entra));
        }

        [Fact]
        public async Task AnonymousClaimAllows()
        {
            var context = AuthenticatedContext(new Claim(ClaimTypes.Anonymous, "true"));

            Assert.True(await InvokeAsync(context, AuthenticationMode.Hybrid));
        }

        [Fact]
        public async Task PullOnlyUserIsDenied()
        {
            var userId = Guid.NewGuid();
            _permissions.Setup(p => p.CanPushAsync(userId, _feedId, _ct)).ReturnsAsync(false);
            var context = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            Assert.False(await InvokeAsync(context, AuthenticationMode.Entra));
        }

        [Fact]
        public async Task UserWithPushIsAllowed()
        {
            var userId = Guid.NewGuid();
            _permissions.Setup(p => p.CanPushAsync(userId, _feedId, _ct)).ReturnsAsync(true);
            var context = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            Assert.True(await InvokeAsync(context, AuthenticationMode.Entra));
        }
    }

    public class CanDeleteFromCurrentFeedAsync
    {
        private readonly Mock<IPermissionService> _permissions = new();
        private readonly Mock<IFeedContext> _feedContext = new();
        private readonly Guid _feedId = Guid.NewGuid();
        private readonly CancellationToken _ct = CancellationToken.None;

        public CanDeleteFromCurrentFeedAsync()
        {
            _feedContext.Setup(f => f.CurrentFeed).Returns(new Feed { Id = _feedId, Slug = "default" });
        }

        private static HttpContext UnauthenticatedContext()
            => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

        private static HttpContext AuthenticatedContext(params Claim[] claims)
            => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };

        private Task<bool> InvokeAsync(HttpContext context, AuthenticationMode mode)
            => FeedAccessGuard.CanDeleteFromCurrentFeedAsync(
                context, _feedContext.Object, _permissions.Object, mode, _ct);

        [Fact]
        public async Task ConfigModeDenies()
        {
            // Delete is a per-user/group permission; Config mode has no such model.
            Assert.False(await InvokeAsync(AuthenticatedContext(), AuthenticationMode.Config));
        }

        [Fact]
        public async Task UnauthenticatedIsDenied()
        {
            Assert.False(await InvokeAsync(UnauthenticatedContext(), AuthenticationMode.Entra));
        }

        [Fact]
        public async Task AnonymousClaimIsDenied()
        {
            var context = AuthenticatedContext(new Claim(ClaimTypes.Anonymous, "true"));

            Assert.False(await InvokeAsync(context, AuthenticationMode.Hybrid));
        }

        [Fact]
        public async Task UserWithoutDeleteIsDenied()
        {
            var userId = Guid.NewGuid();
            _permissions.Setup(p => p.CanDeleteAsync(userId, _feedId, _ct)).ReturnsAsync(false);
            var context = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            Assert.False(await InvokeAsync(context, AuthenticationMode.Entra));
        }

        [Fact]
        public async Task UserWithDeleteIsAllowed()
        {
            var userId = Guid.NewGuid();
            _permissions.Setup(p => p.CanDeleteAsync(userId, _feedId, _ct)).ReturnsAsync(true);
            var context = AuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            Assert.True(await InvokeAsync(context, AuthenticationMode.Entra));
        }
    }
}
