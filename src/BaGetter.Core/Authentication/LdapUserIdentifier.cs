using System;

namespace BaGetter.Core;

internal readonly struct LdapUserIdentifier
{
    private LdapUserIdentifier(
        string rawUsername,
        string accountName,
        string userPrincipalName)
    {
        RawUsername = rawUsername;
        AccountName = accountName;
        UserPrincipalName = userPrincipalName;
    }

    public string RawUsername { get; }

    public string AccountName { get; }

    public string UserPrincipalName { get; }

    public static LdapUserIdentifier Parse(string username)
    {
        var value = username?.Trim() ?? string.Empty;

        var separatorIndex = value.IndexOf('\\');
        if (separatorIndex > 0 && separatorIndex < value.Length - 1)
        {
            return new LdapUserIdentifier(
                value,
                value[(separatorIndex + 1)..],
                null);
        }

        var atIndex = value.IndexOf('@');
        if (atIndex > 0 && atIndex < value.Length - 1)
        {
            return new LdapUserIdentifier(
                value,
                value[..atIndex],
                value);
        }

        return new LdapUserIdentifier(value, value, null);
    }
}
