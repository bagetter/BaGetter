using BaGetter.Core;
using Xunit;

namespace BaGetter.Core.Tests;

public class LdapUserIdentifierTests
{
    [Fact]
    public void Parse_DomainQualifiedUsername_UsesSamAccountName()
    {
        var result = LdapUserIdentifier.Parse(@"domain\user");

        Assert.Equal(@"domain\user", result.RawUsername);
        Assert.Equal("user", result.AccountName);
        Assert.Null(result.UserPrincipalName);
    }

    [Fact]
    public void Parse_UserPrincipalName_PreservesUpn()
    {
        var result = LdapUserIdentifier.Parse("user@domain.local");

        Assert.Equal("user@domain.local", result.RawUsername);
        Assert.Equal("user", result.AccountName);
        Assert.Equal("user@domain.local", result.UserPrincipalName);
    }
}
