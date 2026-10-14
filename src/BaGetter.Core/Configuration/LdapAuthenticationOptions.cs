namespace BaGetter.Core;

public sealed class LdapAuthenticationOptions
{
    public bool Enabled { get; set; }

    public string Server { get; set; }

    public int? Port { get; set; }

    public string BaseDn { get; set; }

    public string BindDn { get; set; }

    public string BindPassword { get; set; }

    public bool UseSsl { get; set; } = true;

    public string[] AllowedGroups { get; set; } = [];
}
