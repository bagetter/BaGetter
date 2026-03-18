using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace BaGetter.Azure.Tests.Configuration;

public class AzureTableOptionsTests
{
    [Fact]
    public void Validate_ConnectionString_NoErrors()
    {
        var options = new AzureTableOptions
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=key;"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AccountNameOnly_ManagedIdentity_NoErrors()
    {
        var options = new AzureTableOptions
        {
            AccountName = "mystorageaccount"
        };

        var results = Validate(options);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_MissingBothConnectionStringAndAccountName_ReturnsError()
    {
        var options = new AzureTableOptions();

        var results = Validate(options);

        Assert.Single(results);
        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(AzureTableOptions.ConnectionString)) &&
            r.MemberNames.Contains(nameof(AzureTableOptions.AccountName)));
    }

    [Fact]
    public void Validate_DefaultTableName_IsPackages()
    {
        var options = new AzureTableOptions();

        Assert.Equal("Packages", options.TableName);
    }

    private static List<ValidationResult> Validate(AzureTableOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        options.Validate(context).ToList().ForEach(results.Add);
        return results;
    }
}
