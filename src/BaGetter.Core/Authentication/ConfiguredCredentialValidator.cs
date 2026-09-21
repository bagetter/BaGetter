using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public sealed class ConfiguredCredentialValidator : INugetCredentialValidator
{
    private readonly NugetCredentials[] _credentials;

    public ConfiguredCredentialValidator(IOptions<BaGetterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _credentials = options.Value.Authentication?.Credentials ?? [];
    }

    public Task<NugetCredentialValidationResult> ValidateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult<NugetCredentialValidationResult>(null);
        }

        var credential = _credentials.FirstOrDefault(a =>
            !string.IsNullOrWhiteSpace(a.Username) &&
            a.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            (
                (!string.IsNullOrEmpty(a.PasswordHash) && SecretHashing.VerifySecret(password, a.PasswordHash)) ||
                a.Password == password
            ));

        return Task.FromResult(
            credential == null
                ? null
                : new NugetCredentialValidationResult(credential.Username, credential.Roles));
    }
}
