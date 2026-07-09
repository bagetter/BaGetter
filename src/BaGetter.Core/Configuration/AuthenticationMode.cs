namespace BaGetter.Core.Configuration;

/// <summary>
/// Controls which authentication mechanisms are active.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Config-file-based API key and basic auth. No database-backed users.
    /// Credentials are defined in appsettings.json.
    /// </summary>
    Config = 0,

    /// <summary>
    /// Only Entra ID (OIDC) authentication is enabled. Local accounts are not accepted.
    /// </summary>
    Entra = 1,

    /// <summary>
    /// Only local account authentication is enabled. Entra ID is not available.
    /// </summary>
    Local = 2,

    /// <summary>
    /// Both Entra ID and local account authentication are enabled.
    /// </summary>
    Hybrid = 3
}
