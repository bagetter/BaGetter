using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using BaGetter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core.Authentication;

public class TokenService : ITokenService
{
    private const string TokenPrefix = "bg_";
    private const int TokenHexLength = 40;
    private const int TokenPrefixStoredLength = 8;

    private readonly IContext _context;
    private readonly NugetAuthenticationOptions _authOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IContext context,
        IOptionsSnapshot<NugetAuthenticationOptions> authOptions,
        ILogger<TokenService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TokenCreateResult> CreateTokenAsync(
        Guid userId,
        string name,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("Token expiry must be in the future.");
        }

        var maxExpiry = DateTime.UtcNow.AddDays(_authOptions.MaxTokenExpiryDays);
        if (expiresAtUtc > maxExpiry)
        {
            throw new ArgumentException(
                $"Token expiry cannot exceed {_authOptions.MaxTokenExpiryDays} days from now.");
        }

        var nameTaken = await _context.PersonalAccessTokens
            .AnyAsync(t => t.UserId == userId && t.Name == name, cancellationToken);
        if (nameTaken)
        {
            throw new ArgumentException($"A token named '{name}' already exists.");
        }

        var plaintextToken = GenerateToken();
        var tokenHash = ComputeHash(plaintextToken);

        var now = DateTime.UtcNow;
        var token = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            TokenHash = tokenHash,
            TokenPrefix = plaintextToken[..TokenPrefixStoredLength],
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = now,
            IsRevoked = false
        };

        _context.PersonalAccessTokens.Add(token);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (_context.IsUniqueConstraintViolationException(ex))
        {
            // Race: another request created a token with the same name between the
            // check above and the save. The (UserId, Name) unique index is the backstop.
            throw new ArgumentException($"A token named '{name}' already exists.");
        }

        _logger.LogInformation("Audit: {EventType} - Created PAT {TokenId} for user {UserId} with prefix {TokenPrefix}",
            "TokenCreated", token.Id, userId, token.TokenPrefix);

        return new TokenCreateResult(token, plaintextToken);
    }

    public async Task<PersonalAccessToken> ValidateTokenAsync(
        string plaintextToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(plaintextToken))
            return null;

        var tokenHash = ComputeHash(plaintextToken);

        var token = await _context.PersonalAccessTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token == null)
            return null;

        if (token.IsRevoked)
        {
            _logger.LogWarning("Audit: {EventType} - Attempted use of revoked token {TokenId}",
                "TokenUseRevoked", token.Id);
            return null;
        }

        if (token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            _logger.LogWarning("Audit: {EventType} - Attempted use of expired token {TokenId}",
                "TokenUseExpired", token.Id);
            return null;
        }

        if (!token.User.IsEnabled)
        {
            _logger.LogWarning("Audit: {EventType} - Attempted use of token {TokenId} for disabled user {UserId}",
                "TokenUseDisabledUser", token.Id, token.UserId);
            return null;
        }

        // Update last used timestamp
        token.LastUsedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Token {TokenId} used by user {UserId}",
            "TokenUsed", token.Id, token.UserId);

        return token;
    }

    public async Task RevokeTokenAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        var token = await _context.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

        if (token == null) return;

        token.IsRevoked = true;
        token.RevokedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Audit: {EventType} - Revoked token {TokenId}",
            "TokenRevoked", tokenId);
    }

    public async Task<List<PersonalAccessToken>> GetUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.PersonalAccessTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    private static string GenerateToken()
    {
        var bytes = new byte[TokenHexLength / 2];
        RandomNumberGenerator.Fill(bytes);
        return TokenPrefix + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
