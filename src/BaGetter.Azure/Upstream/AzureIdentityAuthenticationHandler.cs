using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace BaGetter.Azure;

/// <summary>
/// A <see cref="DelegatingHandler"/> that acquires Azure AD tokens and sets the
/// Authorization header on each outgoing request. Tokens are cached and refreshed
/// when within 5 minutes of expiry.
/// </summary>
public class AzureIdentityAuthenticationHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private AccessToken _cachedToken;

    public AzureIdentityAuthenticationHandler(
        TokenCredential credential,
        string scope,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _credential = credential ?? throw new ArgumentNullException(nameof(credential));
        _scopes = [scope ?? throw new ArgumentNullException(nameof(scope))];
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _cachedToken.Token;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _cachedToken.Token;
            }

            _cachedToken = await _credential.GetTokenAsync(
                new TokenRequestContext(_scopes),
                cancellationToken);

            return _cachedToken.Token;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
