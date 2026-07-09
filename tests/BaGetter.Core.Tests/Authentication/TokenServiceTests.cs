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

public class TokenServiceTests
{
    public class CreateTokenAsync : FactsBase
    {
        [Fact]
        public async Task CreatesTokenWithCorrectProperties()
        {
            var result = await Target.CreateTokenAsync(
                UserId, "My Token", DateTime.UtcNow.AddDays(30), Ct);

            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.PlaintextToken);
            Assert.Equal("My Token", result.Token.Name);
            Assert.Equal(UserId, result.Token.UserId);
            Assert.False(result.Token.IsRevoked);
            Assert.NotEqual(Guid.Empty, result.Token.Id);
        }

        [Fact]
        public async Task PlaintextTokenStartsWithPrefix()
        {
            var result = await Target.CreateTokenAsync(
                UserId, "Prefixed", DateTime.UtcNow.AddDays(30), Ct);

            Assert.StartsWith("bg_", result.PlaintextToken);
        }

        [Fact]
        public async Task TokenPrefixIsStoredCorrectly()
        {
            var result = await Target.CreateTokenAsync(
                UserId, "PrefixCheck", DateTime.UtcNow.AddDays(30), Ct);

            Assert.Equal(8, result.Token.TokenPrefix.Length);
            Assert.Equal(result.PlaintextToken[..8], result.Token.TokenPrefix);
        }

        [Fact]
        public async Task TokenHashIsNotPlaintext()
        {
            var result = await Target.CreateTokenAsync(
                UserId, "HashCheck", DateTime.UtcNow.AddDays(30), Ct);

            Assert.NotEqual(result.PlaintextToken, result.Token.TokenHash);
            Assert.NotEmpty(result.Token.TokenHash);
        }

        [Fact]
        public async Task ThrowsWhenExpiryExceedsMaxDays()
        {
            var tooFar = DateTime.UtcNow.AddDays(366);

            await Assert.ThrowsAsync<ArgumentException>(
                () => Target.CreateTokenAsync(UserId, "TooLong", tooFar, Ct));
        }

        [Fact]
        public async Task GeneratesUniqueTokensEachTime()
        {
            var result1 = await Target.CreateTokenAsync(
                UserId, "Token1", DateTime.UtcNow.AddDays(30), Ct);
            var result2 = await Target.CreateTokenAsync(
                UserId, "Token2", DateTime.UtcNow.AddDays(30), Ct);

            Assert.NotEqual(result1.PlaintextToken, result2.PlaintextToken);
            Assert.NotEqual(result1.Token.TokenHash, result2.Token.TokenHash);
        }

        [Fact]
        public async Task ThrowsWhenNameAlreadyExistsForUser()
        {
            await Target.CreateTokenAsync(
                UserId, "Duplicate", DateTime.UtcNow.AddDays(30), Ct);

            await Assert.ThrowsAsync<ArgumentException>(
                () => Target.CreateTokenAsync(UserId, "Duplicate", DateTime.UtcNow.AddDays(30), Ct));
        }

        [Fact]
        public async Task AllowsSameNameForDifferentUsers()
        {
            var otherUserId = Guid.NewGuid();
            Context.Users.Add(new User
            {
                Id = otherUserId,
                Username = "otheruser",
                DisplayName = "Other",
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = "oid-other",
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await Context.SaveChangesAsync(Ct);

            await Target.CreateTokenAsync(UserId, "Shared", DateTime.UtcNow.AddDays(30), Ct);
            var result = await Target.CreateTokenAsync(otherUserId, "Shared", DateTime.UtcNow.AddDays(30), Ct);

            Assert.Equal("Shared", result.Token.Name);
            Assert.Equal(otherUserId, result.Token.UserId);
        }
    }

    public class ValidateTokenAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsTokenForValidPlaintext()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "Valid", DateTime.UtcNow.AddDays(30), Ct);

            var result = await Target.ValidateTokenAsync(created.PlaintextToken, Ct);

            Assert.NotNull(result);
            Assert.Equal(created.Token.Id, result.Id);
        }

        [Fact]
        public async Task ReturnsNullForUnknownToken()
        {
            var result = await Target.ValidateTokenAsync("bg_unknowntoken1234567890abcdef12345678", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNullForEmptyToken()
        {
            var result = await Target.ValidateTokenAsync("", Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNullForNullToken()
        {
            var result = await Target.ValidateTokenAsync(null, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNullForRevokedToken()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "Revoked", DateTime.UtcNow.AddDays(30), Ct);

            await Target.RevokeTokenAsync(created.Token.Id, Ct);

            var result = await Target.ValidateTokenAsync(created.PlaintextToken, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNullForExpiredToken()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "Expired", DateTime.UtcNow.AddDays(30), Ct);

            // Manually set expiry to the past
            created.Token.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await Context.SaveChangesAsync(Ct);

            var result = await Target.ValidateTokenAsync(created.PlaintextToken, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ReturnsNullForDisabledUser()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "DisabledUser", DateTime.UtcNow.AddDays(30), Ct);

            // Disable the user
            var user = await Context.Users.FindAsync(UserId);
            user.IsEnabled = false;
            await Context.SaveChangesAsync(Ct);

            var result = await Target.ValidateTokenAsync(created.PlaintextToken, Ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatesLastUsedTimestamp()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "LastUsed", DateTime.UtcNow.AddDays(30), Ct);

            Assert.Null(created.Token.LastUsedAtUtc);

            await Target.ValidateTokenAsync(created.PlaintextToken, Ct);

            var token = await Context.PersonalAccessTokens.FindAsync(created.Token.Id);
            Assert.NotNull(token.LastUsedAtUtc);
        }
    }

    public class RevokeTokenAsync : FactsBase
    {
        [Fact]
        public async Task SetsRevokedFlag()
        {
            var created = await Target.CreateTokenAsync(
                UserId, "ToRevoke", DateTime.UtcNow.AddDays(30), Ct);

            await Target.RevokeTokenAsync(created.Token.Id, Ct);

            var token = await Context.PersonalAccessTokens.FindAsync(created.Token.Id);
            Assert.True(token.IsRevoked);
            Assert.NotNull(token.RevokedAtUtc);
        }

        [Fact]
        public async Task DoesNothingWhenTokenNotFound()
        {
            await Target.RevokeTokenAsync(Guid.NewGuid(), Ct);

            // No exception should be thrown
        }
    }

    public class GetUserTokensAsync : FactsBase
    {
        [Fact]
        public async Task ReturnsEmptyWhenNoTokens()
        {
            var result = await Target.GetUserTokensAsync(UserId, Ct);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReturnsTokensForUser()
        {
            await Target.CreateTokenAsync(UserId, "Token1", DateTime.UtcNow.AddDays(30), Ct);
            await Target.CreateTokenAsync(UserId, "Token2", DateTime.UtcNow.AddDays(60), Ct);

            var result = await Target.GetUserTokensAsync(UserId, Ct);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DoesNotReturnOtherUsersTokens()
        {
            var otherUserId = Guid.NewGuid();
            Context.Users.Add(new User
            {
                Id = otherUserId,
                Username = "otheruser",
                DisplayName = "Other",
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = "oid-other",
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await Context.SaveChangesAsync(Ct);

            await Target.CreateTokenAsync(UserId, "MyToken", DateTime.UtcNow.AddDays(30), Ct);
            await Target.CreateTokenAsync(otherUserId, "TheirToken", DateTime.UtcNow.AddDays(30), Ct);

            var result = await Target.GetUserTokensAsync(UserId, Ct);

            Assert.Single(result);
            Assert.Equal("MyToken", result[0].Name);
        }

        [Fact]
        public async Task ReturnsTokensOrderedByCreatedDescending()
        {
            var result1 = await Target.CreateTokenAsync(
                UserId, "First", DateTime.UtcNow.AddDays(30), Ct);

            // Manually adjust CreatedAtUtc to ensure ordering
            result1.Token.CreatedAtUtc = DateTime.UtcNow.AddHours(-2);
            await Context.SaveChangesAsync(Ct);

            await Target.CreateTokenAsync(
                UserId, "Second", DateTime.UtcNow.AddDays(30), Ct);

            var tokens = await Target.GetUserTokensAsync(UserId, Ct);

            Assert.Equal(2, tokens.Count);
            Assert.Equal("Second", tokens[0].Name);
            Assert.Equal("First", tokens[1].Name);
        }
    }

    public class FactsBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly TokenService Target;
        protected readonly CancellationToken Ct = CancellationToken.None;
        protected readonly Guid UserId;

        protected FactsBase()
        {
            Context = TestDbContext.Create();

            UserId = Guid.NewGuid();
            Context.Users.Add(new User
            {
                Id = UserId,
                Username = "tokenuser",
                DisplayName = "Token User",
                AuthProvider = AuthProvider.Entra,
                EntraObjectId = "oid-tokenuser",
                IsEnabled = true,
                CanLoginToUI = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            Context.SaveChanges();

            var authOptions = new NugetAuthenticationOptions
            {
                MaxTokenExpiryDays = 365
            };

            var optionsSnapshot = new Mock<IOptionsSnapshot<NugetAuthenticationOptions>>();
            optionsSnapshot.Setup(o => o.Value).Returns(authOptions);

            Target = new TokenService(
                Context,
                optionsSnapshot.Object,
                Mock.Of<ILogger<TokenService>>());
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
