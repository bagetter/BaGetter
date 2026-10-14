using System;
using System.Security.Cryptography;
using System.Text;

namespace BaGetter.Core;

/// <summary>
/// Utilities for hashing and verifying secrets used in configuration.
/// </summary>
public static class SecretHashing
{
    private const string Prefix = "PBKDF2";
    private const int DefaultIterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string HashSecret(string secret, int iterations = DefaultIterations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (iterations < 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be >= 10000.");
        }

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(secret),
            salt.ToArray(),
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{Prefix}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifySecret(string secret, string hashValue)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(hashValue))
        {
            return false;
        }

        var parts = hashValue.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations < 10_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(secret),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
