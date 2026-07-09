using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Entities;

namespace BaGetter.Core.Authentication;

public record TokenCreateResult(PersonalAccessToken Token, string PlaintextToken);

public interface ITokenService
{
    Task<TokenCreateResult> CreateTokenAsync(Guid userId, string name, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<PersonalAccessToken> ValidateTokenAsync(string plaintextToken, CancellationToken cancellationToken);
    Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken);
    Task<List<PersonalAccessToken>> GetUserTokensAsync(Guid userId, CancellationToken cancellationToken);
}
