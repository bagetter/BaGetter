using System;
using System.Net.Http;
using Azure.Core;
using Azure.Identity;
using BaGetter.Core;
using Microsoft.Extensions.Options;

namespace BaGetter.Azure;

/// <summary>
/// Creates an <see cref="AzureIdentityAuthenticationHandler"/> that injects Azure AD
/// bearer tokens into outgoing mirror HTTP requests.
/// </summary>
public class AzureIdentityMirrorAuthHandlerFactory : IMirrorAuthenticationHandler
{
    private const string AzureDevOpsScope = "https://app.vssps.visualstudio.com/.default";

    private readonly MirrorOptions _options;

    public AzureIdentityMirrorAuthHandlerFactory(IOptions<MirrorOptions> options)
    {
        _options = options.Value;
    }

    public HttpMessageHandler CreateHandler(HttpMessageHandler innerHandler)
    {
        var scope = ResolveScope();
        var credential = ResolveCredential();
        return new AzureIdentityAuthenticationHandler(
            credential,
            scope,
            innerHandler);
    }

    internal TokenCredential ResolveCredential()
    {
        var auth = _options.Authentication;

        // Service principal: all three must be set
        if (!string.IsNullOrEmpty(auth?.TenantId) &&
            !string.IsNullOrEmpty(auth?.ClientId) &&
            !string.IsNullOrEmpty(auth?.ClientSecret))
        {
            return new ClientSecretCredential(auth.TenantId, auth.ClientId, auth.ClientSecret);
        }

        // User-assigned managed identity: only ClientId
        if (!string.IsNullOrEmpty(auth?.ClientId))
        {
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = auth.ClientId
            });
        }

        // System-assigned managed identity / environment / CLI
        return new DefaultAzureCredential();
    }

    internal string ResolveScope()
    {
        // Explicit config always wins
        if (!string.IsNullOrEmpty(_options.Authentication?.Scope))
        {
            return _options.Authentication.Scope;
        }

        // Derive scope from the mirror package source URL
        if (_options.PackageSource != null)
        {
            return ResolveScopeFromUrl(_options.PackageSource);
        }

        // Last resort fallback
        return AzureDevOpsScope;
    }

    internal static string ResolveScopeFromUrl(Uri packageSource)
    {
        var host = packageSource.Host;

        // Azure DevOps uses a different resource than its domain
        if (host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return AzureDevOpsScope;
        }

        // General case: https://{host}/.default
        return $"https://{host}/.default";
    }
}
