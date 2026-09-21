using System.DirectoryServices.Protocols;
using BaGetter.Core;
using Xunit;

namespace BaGetter.Core.Tests;

public class LdapCredentialValidatorTests
{
    [Fact]
    public void ExtractCommonName_ReturnsCnFromDistinguishedName()
    {
        var result = LdapCredentialValidator.ExtractCommonName("CN=NuGet Readers,OU=Groups,DC=example,DC=local");

        Assert.Equal("NuGet Readers", result);
    }

    [Fact]
    public void GetGroupNames_ReturnsDistinctCnValues()
    {
        var attribute = new DirectoryAttribute(
            "memberOf",
            "CN=NuGet Readers,OU=Groups,DC=example,DC=local",
            "CN=Admins,OU=Groups,DC=example,DC=local",
            "CN=NuGet Readers,OU=Other,DC=example,DC=local");

        var result = LdapCredentialValidator.GetGroupNames(attribute);

        Assert.Equal(["NuGet Readers", "Admins"], result);
    }

    [Fact]
    public void IsAllowedGroupMember_WhenAllowedGroupsMissing_ReturnsTrue()
    {
        var result = LdapCredentialValidator.IsAllowedGroupMember(
            new LdapAuthenticationOptions(),
            ["Some Group"]);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowedGroupMember_WhenUserIsInAllowedGroup_ReturnsTrue()
    {
        var result = LdapCredentialValidator.IsAllowedGroupMember(
            new LdapAuthenticationOptions
            {
                AllowedGroups = ["NuGet Readers", "NuGet Admins"]
            },
            ["Developers", "NuGet Admins"]);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowedGroupMember_WhenUserIsNotInAllowedGroup_ReturnsFalse()
    {
        var result = LdapCredentialValidator.IsAllowedGroupMember(
            new LdapAuthenticationOptions
            {
                AllowedGroups = ["NuGet Readers", "NuGet Admins"]
            },
            ["Developers"]);

        Assert.False(result);
    }
}
