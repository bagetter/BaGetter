using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace BaGetter.Azure.Tests.Configuration;

public class AzureBlobStorageOptionsTests
{
    [Fact]
    public void Validate_ConnectionString_NoErrors()
    {
        var options = new AzureBlobStorageOptions
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;",
            Container = "packages"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AccountNameAndAccessKey_NoErrors()
    {
        var options = new AzureBlobStorageOptions
        {
            AccountName = "mystorageaccount",
            AccessKey = "myaccesskey",
            Container = "packages"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AccountNameOnly_ManagedIdentity_NoErrors()
    {
        var options = new AzureBlobStorageOptions
        {
            AccountName = "mystorageaccount",
            Container = "packages"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_MissingAccountNameAndConnectionString_ReturnsError()
    {
        var options = new AzureBlobStorageOptions
        {
            Container = "packages"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AzureBlobStorageOptions.AccountName)));
    }

    [Fact]
    public void Validate_MissingContainer_ReturnsError()
    {
        var options = new AzureBlobStorageOptions
        {
            AccountName = "mystorageaccount"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AzureBlobStorageOptions.Container)));
    }

    private static List<ValidationResult> Validate(AzureBlobStorageOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        options.Validate(context).ToList().ForEach(results.Add);
        return results;
    }
}
