using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BaGetter.Tests;

public class SearchTagProviderIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public SearchTagProviderIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> ProviderData()
    {
        yield return ["Sqlite", null, null];
        yield return ["MySql", Environment.GetEnvironmentVariable("BAGETTER_TEST_MYSQL_CONNECTION_STRING"), "BAGETTER_TEST_MYSQL_CONNECTION_STRING"];
        yield return ["PostgreSql", Environment.GetEnvironmentVariable("BAGETTER_TEST_POSTGRES_CONNECTION_STRING"), "BAGETTER_TEST_POSTGRES_CONNECTION_STRING"];
        yield return ["SqlServer", Environment.GetEnvironmentVariable("BAGETTER_TEST_SQLSERVER_CONNECTION_STRING"), "BAGETTER_TEST_SQLSERVER_CONNECTION_STRING"];
    }

    [Theory]
    [MemberData(nameof(ProviderData))]
    public async Task SearchTagQueryFiltersResultsByTag(string databaseType, string connectionString, string connectionStringEnvVar)
    {
        if (!string.Equals(databaseType, "Sqlite", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(connectionString))
        {
            _output.WriteLine($"Skipping {databaseType}. Set {connectionStringEnvVar} to run this provider integration test.");
            return;
        }

        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config =>
        {
            config["Database:Type"] = databaseType;
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                config["Database:ConnectionString"] = connectionString;
            }
        });
        using var client = app.CreateClient();

        var testTag = $"tagsearch-{Guid.NewGuid():N}";

        await using (var taggedPackage = TestResources.GetPackageStreamWithVersionAndTags("1.2.3", [testTag]))
        {
            await app.AddPackageAsync(taggedPackage);
        }

        using var response = await client.GetAsync($"v3/search?q=tag:{testTag}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.True(document.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.GetArrayLength() >= 1);

        var foundPackage = false;
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idProperty) ||
                !item.TryGetProperty("tags", out var tagsProperty))
            {
                continue;
            }

            var id = idProperty.GetString();
            var hasTag = false;
            foreach (var tagElement in tagsProperty.EnumerateArray())
            {
                if (string.Equals(tagElement.GetString(), testTag, StringComparison.OrdinalIgnoreCase))
                {
                    hasTag = true;
                    break;
                }
            }

            Assert.True(hasTag, $"Package '{id}' did not include expected tag '{testTag}'.");

            if (string.Equals(id, "TestData", StringComparison.OrdinalIgnoreCase))
            {
                foundPackage = true;
            }
        }

        Assert.True(foundPackage, $"Expected package 'TestData' was not returned for tag '{testTag}' on provider '{databaseType}'.");
    }

    [Theory]
    [MemberData(nameof(ProviderData))]
    public async Task SearchAuthorQueryFiltersResultsByAuthor(string databaseType, string connectionString, string connectionStringEnvVar)
    {
        if (!string.Equals(databaseType, "Sqlite", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(connectionString))
        {
            _output.WriteLine($"Skipping {databaseType}. Set {connectionStringEnvVar} to run this provider integration test.");
            return;
        }

        using var app = new BaGetterApplication(_output, inMemoryConfiguration: config =>
        {
            config["Database:Type"] = databaseType;
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                config["Database:ConnectionString"] = connectionString;
            }
        });
        using var client = app.CreateClient();

        var testAuthor = $"Þorvaldur Hafdal {Guid.NewGuid():N}";

        await using (var authorPackage = TestResources.GetPackageStreamWithVersionAndAuthors("1.2.3", [testAuthor]))
        {
            await app.AddPackageAsync(authorPackage);
        }

        var quotedAuthorQuery = Uri.EscapeDataString($"author:\"{testAuthor}\" TestData");
        using var response = await client.GetAsync($"v3/search?q={quotedAuthorQuery}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        Assert.True(document.RootElement.TryGetProperty("data", out var data));
        Assert.True(data.GetArrayLength() >= 1);

        var foundPackage = false;
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idProperty) ||
                !item.TryGetProperty("authors", out var authorsProperty))
            {
                continue;
            }

            var id = idProperty.GetString();
            var hasAuthor = false;
            foreach (var authorElement in authorsProperty.EnumerateArray())
            {
                var author = authorElement.GetString();
                if (!string.IsNullOrEmpty(author) &&
                    author.Contains(testAuthor, StringComparison.OrdinalIgnoreCase))
                {
                    hasAuthor = true;
                    break;
                }
            }

            Assert.True(hasAuthor, $"Package '{id}' did not include expected author '{testAuthor}'.");

            if (string.Equals(id, "TestData", StringComparison.OrdinalIgnoreCase))
            {
                foundPackage = true;
            }
        }

        Assert.True(foundPackage, $"Expected package 'TestData' was not returned for author '{testAuthor}' on provider '{databaseType}'.");
    }
}
