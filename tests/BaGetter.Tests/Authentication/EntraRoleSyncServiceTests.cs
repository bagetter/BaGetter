using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Authentication;
using BaGetter.Core.Authentication;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BaGetter.Tests.Authentication;

public class EntraRoleSyncServiceTests
{
    public class OnTokenValidatedAsync : FactsBase
    {
        [Fact]
        public async Task SkipsWhenObjectIdMissing()
        {
            var principal = CreatePrincipal(oid: null);

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(
                s => s.FindByEntraObjectIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProvisionsNewUser()
        {
            var principal = CreatePrincipal(oid: "oid-1", email: "alice@test.com", name: "Alice Test");
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-1", Ct)).ReturnsAsync((User)null);
            UserService.Setup(s => s.CreateEntraUserAsync("oid-1", "alice@test.com", "Alice Test", Ct))
                .ReturnsAsync(CreateUserEntity("oid-1", "alice@test.com"));

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(s => s.CreateEntraUserAsync("oid-1", "alice@test.com", "Alice Test", Ct));
        }

        [Fact]
        public async Task UpdatesDisplayNameForExistingUser()
        {
            var user = CreateUserEntity("oid-2", "bob@test.com", displayName: "Old Name");
            var principal = CreatePrincipal(oid: "oid-2", email: "bob@test.com", name: "New Name");
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-2", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            Assert.Equal("New Name", user.DisplayName);
            UserService.Verify(s => s.UpdateUserAsync(user, Ct));
        }

        [Fact]
        public async Task ThrowsWhenUserDisabled()
        {
            var user = CreateUserEntity("oid-3", "disabled@test.com");
            user.IsEnabled = false;
            var principal = CreatePrincipal(oid: "oid-3", roles: new[] { "Admin" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-3", Ct)).ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Target.OnTokenValidatedAsync(principal, Ct));

            GroupService.Verify(
                s => s.SyncAppRoleMembershipsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ThrowsWhenCanLoginToUIDisabled()
        {
            var user = CreateUserEntity("oid-nologin", "nologin@test.com");
            user.IsEnabled = true;
            user.CanLoginToUI = false;
            var principal = CreatePrincipal(oid: "oid-nologin", roles: new[] { "Admin" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-nologin", Ct)).ReturnsAsync(user);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => Target.OnTokenValidatedAsync(principal, Ct));

            GroupService.Verify(
                s => s.SyncAppRoleMembershipsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GrantsAdminWhenAdminRolePresent()
        {
            var user = CreateUserEntity("oid-4", "admin@test.com");
            user.IsAdmin = false;
            var principal = CreatePrincipal(oid: "oid-4", roles: new[] { "Admin" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-4", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(s => s.SetAdminAsync(user.Id, true, Ct));
        }

        [Fact]
        public async Task RevokesAdminWhenAdminRoleMissing()
        {
            var user = CreateUserEntity("oid-5", "wasadmin@test.com");
            user.IsAdmin = true;
            var principal = CreatePrincipal(oid: "oid-5", roles: new[] { "TeamFrontend" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-5", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(s => s.SetAdminAsync(user.Id, false, Ct));
        }

        [Fact]
        public async Task DoesNotChangeAdminWhenAlreadyCorrect()
        {
            var user = CreateUserEntity("oid-6", "alreadyadmin@test.com");
            user.IsAdmin = true;
            var principal = CreatePrincipal(oid: "oid-6", roles: new[] { "Admin" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-6", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(s => s.SetAdminAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SyncsGroupMembershipsFromRoles()
        {
            var user = CreateUserEntity("oid-7", "roleuser@test.com");
            var principal = CreatePrincipal(oid: "oid-7", roles: new[] { "TeamFrontend", "TeamBackend" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-7", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            GroupService.Verify(s => s.SyncAppRoleMembershipsAsync(
                user.Id,
                It.Is<IReadOnlyList<string>>(r => r.Count == 2),
                Ct));
        }

        [Fact]
        public async Task SyncsEmptyRolesList()
        {
            var user = CreateUserEntity("oid-8", "noroles@test.com");
            var principal = CreatePrincipal(oid: "oid-8", roles: Array.Empty<string>());
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-8", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            GroupService.Verify(s => s.SyncAppRoleMembershipsAsync(
                user.Id,
                It.Is<IReadOnlyList<string>>(r => r.Count == 0),
                Ct));
        }

        [Fact]
        public async Task ManualGroupsNotTouchedBySync()
        {
            var user = CreateUserEntity("oid-9", "manual@test.com");
            var principal = CreatePrincipal(oid: "oid-9", roles: new[] { "TeamQA" });
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-9", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            // SyncAppRoleMembershipsAsync is called which internally only touches role-linked groups
            GroupService.Verify(s => s.SyncAppRoleMembershipsAsync(
                user.Id,
                It.Is<IReadOnlyList<string>>(r => r.Count == 1 && r[0] == "TeamQA"),
                Ct));
        }

        [Fact]
        public async Task ReplacesNameIdentifierClaimWithBaGetterId()
        {
            var user = CreateUserEntity("oid-10", "claims@test.com");
            var principal = CreatePrincipal(oid: "oid-10");
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-10", Ct)).ReturnsAsync(user);

            await Target.OnTokenValidatedAsync(principal, Ct);

            var identity = principal.Identity as ClaimsIdentity;
            var nameIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier);
            Assert.NotNull(nameIdClaim);
            Assert.Equal(user.Id.ToString(), nameIdClaim.Value);

            var authProviderClaim = identity?.FindFirst("auth_provider");
            Assert.NotNull(authProviderClaim);
            Assert.Equal("Entra", authProviderClaim.Value);
        }

        [Fact]
        public async Task UsesCustomRoleClaim()
        {
            Options.Value.Authentication.Entra.RoleClaim = "custom_roles";
            var user = CreateUserEntity("oid-11", "custom@test.com");
            var principal = CreatePrincipal(oid: "oid-11", roles: new[] { "Admin" }, roleClaim: "custom_roles");
            UserService.Setup(s => s.FindByEntraObjectIdAsync("oid-11", Ct)).ReturnsAsync(user);
            user.IsAdmin = false;

            await Target.OnTokenValidatedAsync(principal, Ct);

            UserService.Verify(s => s.SetAdminAsync(user.Id, true, Ct));
        }
    }

    public class FactsBase
    {
        protected readonly Mock<IUserService> UserService;
        protected readonly Mock<IGroupService> GroupService;
        protected readonly IOptions<BaGetterOptions> Options;
        protected readonly EntraRoleSyncService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;

        protected FactsBase()
        {
            UserService = new Mock<IUserService>();
            GroupService = new Mock<IGroupService>();

            var opts = new BaGetterOptions
            {
                Authentication = new NugetAuthenticationOptions
                {
                    Entra = new EntraOptions
                    {
                        RoleClaim = "roles"
                    }
                }
            };
            Options = Microsoft.Extensions.Options.Options.Create(opts);

            Target = new EntraRoleSyncService(
                UserService.Object,
                GroupService.Object,
                Options,
                Mock.Of<ILogger<EntraRoleSyncService>>());
        }

        protected static User CreateUserEntity(string oid, string username, string displayName = null)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = displayName ?? username,
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = oid,
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        protected static ClaimsPrincipal CreatePrincipal(
            string oid = null,
            string email = null,
            string name = null,
            string[] roles = null,
            string roleClaim = "roles")
        {
            var claims = new List<Claim>();

            if (oid != null)
                claims.Add(new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", oid));

            if (email != null)
                claims.Add(new Claim(ClaimTypes.Email, email));

            if (name != null)
                claims.Add(new Claim("name", name));

            if (roles != null)
            {
                foreach (var role in roles)
                    claims.Add(new Claim(roleClaim, role));
            }

            // Add a default NameIdentifier that should be replaced
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "entra-sub-value"));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }
    }
}
