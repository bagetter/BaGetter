namespace BaGetter.Core;

public sealed class NugetCredentials
{
    public string Username { get; set; }

    public string Password { get; set; }

    /// <summary>
    /// Optional list of roles assigned to this user (for example: "Admin").
    /// </summary>
    public string[] Roles { get; set; } = [];

    /// <summary>
    /// Optional password hash in format:
    /// PBKDF2$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;
    /// If set, this is used instead of <see cref="Password"/>.
    /// </summary>
    public string PasswordHash { get; set; }
}
