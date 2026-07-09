namespace BaGetter.Core.Configuration;

/// <summary>
/// Configuration for Azure Entra ID (OpenID Connect) authentication.
/// </summary>
public class EntraOptions
{
    /// <summary>
    /// The Azure AD instance URL, e.g. "https://login.microsoftonline.com/".
    /// </summary>
    public string Instance { get; set; }

    /// <summary>
    /// The Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// The application (client) ID registered in Azure AD.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// The client secret for the Azure AD application.
    /// Should be provided via environment variable or Docker secrets in production.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// The callback path for the OpenID Connect sign-in response.
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// The claim name in the token that contains App Role values.
    /// Defaults to the standard Microsoft claim for roles, but can be overridden if your token uses a different claim.
    /// </summary>
    public string RoleClaim { get; set; } = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
}
