using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Configuration;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public class ApiKeyAuthenticationService : IAuthenticationService
{
    private readonly string _apiKey;
    private readonly string _apiKeyHash;
    private readonly ApiKey[] _apiKeys;

    public ApiKeyAuthenticationService(IOptionsSnapshot<BaGetterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _apiKey = string.IsNullOrEmpty(options.Value.ApiKey) ? null : options.Value.ApiKey;
        _apiKeyHash = string.IsNullOrEmpty(options.Value.ApiKeyHash) ? null : options.Value.ApiKeyHash;
        _apiKeys = options.Value.Authentication?.ApiKeys ?? [];
    }

    public Task<bool> AuthenticateAsync(string apiKey, CancellationToken cancellationToken)
        => Task.FromResult(Authenticate(apiKey));

    private bool Authenticate(string apiKey)
    {
        var hasAuthKeys = !string.IsNullOrWhiteSpace(_apiKey) ||
                          !string.IsNullOrWhiteSpace(_apiKeyHash) ||
                          _apiKeys.Any(x => !string.IsNullOrWhiteSpace(x.Key) || !string.IsNullOrWhiteSpace(x.KeyHash));

        // No authentication is necessary if there is no required API key.
        if (!hasAuthKeys) return true;

        if (_apiKey == apiKey)
        {
            return true;
        }

        if (SecretHashing.VerifySecret(apiKey, _apiKeyHash))
        {
            return true;
        }

        return _apiKeys.Any(x =>
            (!string.IsNullOrEmpty(x.Key) && x.Key.Equals(apiKey)) ||
            SecretHashing.VerifySecret(apiKey, x.KeyHash));
    }
}
