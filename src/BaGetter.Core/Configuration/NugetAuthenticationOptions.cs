using BaGetter.Core.Configuration;

namespace BaGetter.Core;

public sealed class NugetAuthenticationOptions
{
    /// <summary>
    /// Username and password credentials for downloading packages.
    /// </summary>
    public NugetCredentials[] Credentials { get; set; }

    /// <summary>
    /// Api keys for pushing packages into the feed.
    /// </summary>
    public ApiKey[] ApiKeys { get; set; }
}
