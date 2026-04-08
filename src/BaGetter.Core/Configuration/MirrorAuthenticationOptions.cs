using System.Collections.Generic;

namespace BaGetter.Core;

public class MirrorAuthenticationOptions
{
    public MirrorAuthenticationType Type { get; set; } = MirrorAuthenticationType.None;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Token { get; set; }

    public Dictionary<string, string> CustomHeaders { get; set; } = [];

    /// <summary>
    /// The OAuth2 scope used when acquiring tokens for mirror authentication.
    /// If not set, the scope is derived from the mirror package source URL.
    /// </summary>
    public string Scope { get; set; }

    /// <summary>
    /// The Microsoft Entra tenant ID. Required for service principal authentication.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// The client (application) ID. Used for service principal authentication
    /// or to select a user-assigned managed identity.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// The client secret. When set along with <see cref="TenantId"/> and
    /// <see cref="ClientId"/>, uses service principal authentication.
    /// </summary>
    public string ClientSecret { get; set; }
}
