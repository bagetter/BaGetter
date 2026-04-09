using System.Collections.Generic;

namespace BaGetter.Core;

public class MirrorAuthenticationOptions
{
    public MirrorAuthenticationType Type { get; set; } = MirrorAuthenticationType.None;

    /// <summary>
    /// Username for upstream mirror authentication.
    /// Avoid storing in appsettings.json — use environment variables or Docker secrets instead.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Password for upstream mirror authentication.
    /// Avoid storing in appsettings.json — use environment variables or Docker secrets instead.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Bearer token for upstream mirror authentication.
    /// Avoid storing in appsettings.json — use environment variables or Docker secrets instead.
    /// </summary>
    public string Token { get; set; }

    public Dictionary<string, string> CustomHeaders { get; set; } = [];
}
