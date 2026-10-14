namespace BaGetter.Core;

public sealed class NugetCredentialValidationResult
{
    public NugetCredentialValidationResult(string username, string[] roles = null)
    {
        Username = username;
        Roles = roles ?? [];
    }

    public string Username { get; }

    public string[] Roles { get; }
}
