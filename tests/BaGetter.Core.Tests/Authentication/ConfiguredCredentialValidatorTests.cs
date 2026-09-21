using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace BaGetter.Core.Tests;

public class ConfiguredCredentialValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithPlainPassword_ReturnsUser()
    {
        var validator = CreateValidator(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = "valdi", Password = "secret", Roles = ["Reader"] }]
            }
        });

        var result = await validator.ValidateAsync("valdi", "secret", default);

        Assert.NotNull(result);
        Assert.Equal("valdi", result.Username);
        Assert.Contains("Reader", result.Roles);
    }

    [Fact]
    public async Task ValidateAsync_WithPasswordHash_ReturnsUser()
    {
        var validator = CreateValidator(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials =
                [
                    new NugetCredentials
                    {
                        Username = "valdi",
                        PasswordHash = SecretHashing.HashSecret("secret")
                    }
                ]
            }
        });

        var result = await validator.ValidateAsync("valdi", "secret", default);

        Assert.NotNull(result);
        Assert.Equal("valdi", result.Username);
    }

    [Fact]
    public async Task ValidateAsync_WithUnknownUser_ReturnsNull()
    {
        var validator = CreateValidator(new BaGetterOptions
        {
            Authentication = new NugetAuthenticationOptions
            {
                Credentials = [new NugetCredentials { Username = "valdi", Password = "secret" }]
            }
        });

        var result = await validator.ValidateAsync("someone", "secret", default);

        Assert.Null(result);
    }

    private static ConfiguredCredentialValidator CreateValidator(BaGetterOptions options)
        => new(Options.Create(options));
}
