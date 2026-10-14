using System;
using System.DirectoryServices.Protocols;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public sealed class LdapCredentialValidator : INugetCredentialValidator
{
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ILogger<LdapCredentialValidator> _logger;

    public LdapCredentialValidator(
        IOptions<BaGetterOptions> options,
        ILogger<LdapCredentialValidator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<NugetCredentialValidationResult> ValidateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var ldap = _options.Value.Authentication?.Ldap;
        if (!IsEnabled(ldap) || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult<NugetCredentialValidationResult>(null);
        }

        try
        {
            var identifier = LdapUserIdentifier.Parse(username);
            var userEntry = FindUserEntry(ldap, identifier, username, password);
            if (userEntry == null)
            {
                return Task.FromResult<NugetCredentialValidationResult>(null);
            }

            if (!BindAsUser(ldap, userEntry.DistinguishedName, password))
            {
                return Task.FromResult<NugetCredentialValidationResult>(null);
            }

            if (!IsAllowedGroupMember(ldap, userEntry.GroupNames))
            {
                return Task.FromResult<NugetCredentialValidationResult>(null);
            }

            var resolvedUsername = !string.IsNullOrWhiteSpace(userEntry.SamAccountName)
                ? userEntry.SamAccountName
                : username;

            return Task.FromResult(new NugetCredentialValidationResult(resolvedUsername, userEntry.GroupNames));
        }
        catch (LdapException ex)
        {
            _logger.LogInformation(ex, "LDAP authentication failed for user {Username}", username);
            return Task.FromResult<NugetCredentialValidationResult>(null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected LDAP authentication failure for user {Username}", username);
            return Task.FromResult<NugetCredentialValidationResult>(null);
        }
    }

    private static bool IsEnabled(LdapAuthenticationOptions ldap)
        => ldap?.Enabled == true &&
           !string.IsNullOrWhiteSpace(ldap.Server) &&
           !string.IsNullOrWhiteSpace(ldap.BaseDn);

    private LdapUserEntry FindUserEntry(
        LdapAuthenticationOptions ldap,
        LdapUserIdentifier identifier,
        string username,
        string password)
    {
        using var connection = CreateConnection(ldap);

        if (!TryBindSearchConnection(connection, ldap, username, password))
        {
            return null;
        }

        var request = new System.DirectoryServices.Protocols.SearchRequest(
            ldap.BaseDn,
            BuildSearchFilter(identifier),
            SearchScope.Subtree,
            new[] { "distinguishedName", "sAMAccountName", "memberOf" });

        var response = (SearchResponse)connection.SendRequest(request);
        var entry = response.Entries.Cast<SearchResultEntry>().FirstOrDefault();
        if (entry == null)
        {
            return null;
        }

        return new LdapUserEntry(
            entry.DistinguishedName,
            entry.Attributes["sAMAccountName"]?[0]?.ToString(),
            GetGroupNames(entry.Attributes["memberOf"]));
    }

    private bool TryBindSearchConnection(
        LdapConnection connection,
        LdapAuthenticationOptions ldap,
        string username,
        string password)
    {
        if (!string.IsNullOrWhiteSpace(ldap.BindDn))
        {
            connection.Bind(new NetworkCredential(ldap.BindDn, ldap.BindPassword));
            return true;
        }

        try
        {
            connection.Bind();
            return true;
        }
        catch (LdapException ex)
        {
            _logger.LogDebug(ex, "Anonymous LDAP search bind failed. Falling back to the presented credentials.");
        }

        connection.Bind(new NetworkCredential(username, password));
        return true;
    }

    private bool BindAsUser(LdapAuthenticationOptions ldap, string distinguishedName, string password)
    {
        using var connection = CreateConnection(ldap);
        connection.Bind(new NetworkCredential(distinguishedName, password));
        return true;
    }

    private static LdapConnection CreateConnection(LdapAuthenticationOptions ldap)
    {
        var identifier = ldap.Port.HasValue
            ? new LdapDirectoryIdentifier(ldap.Server, ldap.Port.Value)
            : new LdapDirectoryIdentifier(ldap.Server, ldap.UseSsl ? 636 : 389);

        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Basic,
            Timeout = TimeSpan.FromSeconds(15),
        };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        connection.SessionOptions.SecureSocketLayer = ldap.UseSsl;

        return connection;
    }

    private static string BuildSearchFilter(LdapUserIdentifier identifier)
    {
        var escapedAccountName = EscapeFilterValue(identifier.AccountName);
        if (!string.IsNullOrWhiteSpace(identifier.UserPrincipalName))
        {
            return $"(|(sAMAccountName={escapedAccountName})(userPrincipalName={EscapeFilterValue(identifier.UserPrincipalName)}))";
        }

        return $"(sAMAccountName={escapedAccountName})";
    }

    private static string EscapeFilterValue(string value)
    {
        return value
            .Replace("\\", "\\5c", StringComparison.Ordinal)
            .Replace("*", "\\2a", StringComparison.Ordinal)
            .Replace("(", "\\28", StringComparison.Ordinal)
            .Replace(")", "\\29", StringComparison.Ordinal)
            .Replace("\0", "\\00", StringComparison.Ordinal);
    }

    internal static string[] GetGroupNames(DirectoryAttribute attribute)
    {
        if (attribute == null || attribute.Count == 0)
        {
            return [];
        }

        var groups = new List<string>(attribute.Count);
        foreach (var item in attribute)
        {
            var distinguishedName = item?.ToString();
            var groupName = ExtractCommonName(distinguishedName);
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                groups.Add(groupName);
            }
        }

        return groups
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ExtractCommonName(string distinguishedName)
    {
        if (string.IsNullOrWhiteSpace(distinguishedName))
        {
            return null;
        }

        foreach (var part in distinguishedName.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase) && trimmed.Length > 3)
            {
                return trimmed[3..];
            }
        }

        return null;
    }

    internal static bool IsAllowedGroupMember(LdapAuthenticationOptions ldap, string[] groupNames)
    {
        var allowedGroups = ldap?.AllowedGroups?
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Select(g => g.Trim())
            .ToArray();

        if (allowedGroups == null || allowedGroups.Length == 0)
        {
            return true;
        }

        if (groupNames == null || groupNames.Length == 0)
        {
            return false;
        }

        return groupNames.Intersect(allowedGroups, StringComparer.OrdinalIgnoreCase).Any();
    }

    private sealed class LdapUserEntry
    {
        public LdapUserEntry(string distinguishedName, string samAccountName, string[] groupNames)
        {
            DistinguishedName = distinguishedName;
            SamAccountName = samAccountName;
            GroupNames = groupNames ?? [];
        }

        public string DistinguishedName { get; }

        public string SamAccountName { get; }

        public string[] GroupNames { get; }
    }
}
