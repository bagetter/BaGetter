using BaGetter.Core.Configuration;

namespace BaGetter.Core;

public sealed class NugetAuthenticationOptions
{
    public NugetCredentials[] Credentials { get; set; }

    public ApiKey[] ApiKeys { get; set; }
}
