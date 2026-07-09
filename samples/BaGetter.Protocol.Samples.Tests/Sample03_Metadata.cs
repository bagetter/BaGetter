using System;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Protocol.Samples.Tests;

public class Sample03Metadata
{
    [Fact]
    public async Task GetAllPackageMetadata()
    {
        // Find the metadata for all versions of a package.
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");

        var items = await client.GetPackageMetadataAsync("Newtonsoft.Json");
        if (!items.Any())
        {
            Console.WriteLine($"Package 'Newtonsoft.Json' does not exist");
            return;
        }

        // There is an item for each version of the package.
        foreach (var metadata in items)
        {
            Console.WriteLine($"Version: {metadata.Version}");
            Console.WriteLine($"Listed: {metadata.Listed}");
            Console.WriteLine($"Tags: {metadata.Tags}");
            Console.WriteLine($"Description: {metadata.Description}");
        }
    }

    [Fact]
    public async Task GetPackageMetadata()
    {
        // Find the metadata for a single version of a package.
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");

        var packageId = "Newtonsoft.Json";
        var packageVersion = new NuGetVersion("12.0.1");

        var metadata = await client.GetPackageMetadataAsync(packageId, packageVersion);

        Console.WriteLine($"Listed: {metadata.Listed}");
        Console.WriteLine($"Tags: {metadata.Tags}");
        Console.WriteLine($"Description: {metadata.Description}");
    }

    [Fact]
    public async Task ListVersions()
    {
        // Find all versions of a package (including unlisted versions).
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");

        var packageVersions = await client.ListPackageVersionsAsync("Newtonsoft.Json", includeUnlisted: true);
        if (!packageVersions.Any())
        {
            Console.WriteLine($"Package 'Newtonsoft.Json' does not exist");
            return;
        }

        Console.WriteLine($"Found {packageVersions.Count} versions");
    }
}
