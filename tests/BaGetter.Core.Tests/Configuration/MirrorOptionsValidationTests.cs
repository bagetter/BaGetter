using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace BaGetter.Core.Tests.Configuration;

public class MirrorOptionsValidationTests
{
    [Fact]
    public void Validate_AzureIdentity_NoAdditionalFieldsRequired()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = new Uri("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity
            }
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AzureIdentity_WithCustomResource_NoErrors()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = new Uri("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity,
                Scope = "https://custom.scope/.default"
            }
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AzureIdentity_ServicePrincipal_NoErrors()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = new Uri("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity,
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret"
            }
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AzureIdentity_ClientSecretWithoutTenantId_ReturnsError()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = new Uri("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity,
                ClientId = "client-id",
                ClientSecret = "client-secret"
            }
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MirrorAuthenticationOptions.TenantId)));
    }

    [Fact]
    public void Validate_AzureIdentity_ClientSecretWithoutClientId_ReturnsError()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            PackageSource = new Uri("https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity,
                TenantId = "tenant-id",
                ClientSecret = "client-secret"
            }
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MirrorAuthenticationOptions.ClientId)));
    }

    [Fact]
    public void Validate_AzureIdentity_Legacy_ReturnsError()
    {
        var options = new MirrorOptions
        {
            Enabled = true,
            Legacy = true,
            PackageSource = new Uri("https://example.com/nuget"),
            Authentication = new MirrorAuthenticationOptions
            {
                Type = MirrorAuthenticationType.AzureIdentity
            }
        };

        var results = Validate(options);

        Assert.Single(results);
        Assert.Contains("Legacy", results[0].ErrorMessage);
    }

    private static List<ValidationResult> Validate(MirrorOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        options.Validate(context).ToList().ForEach(results.Add);
        return results;
    }
}
