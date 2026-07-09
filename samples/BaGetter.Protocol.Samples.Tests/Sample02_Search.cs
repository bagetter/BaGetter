using System;
using System.Threading.Tasks;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Protocol.Samples.Tests;

public class Sample02Search
{
    [Fact]
    public async Task Exists()
    {
        // Check if a package exists.
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");

        if (!await client.ExistsAsync("newtonsoft.json"))
        {
            Console.WriteLine("Package 'newtonsoft.json' does not exist!");
        }

        var packageVersion = NuGetVersion.Parse("12.0.1");
        if (!await client.ExistsAsync("newtonsoft.json", packageVersion))
        {
            Console.WriteLine("Package 'newtonsoft.json' version '12.0.1' does not exist!");
        }
    }

    [Fact]
    public async Task Search()
    {
        // Search for packages that are relevant to "json".
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");
        var results = await client.SearchAsync("json");

        var index = 1;
        foreach (var result in results)
        {
            Console.WriteLine($"Result #{index}");
            Console.WriteLine($"Package id: {result.PackageId}");
            Console.WriteLine($"Package version: {result.Version}");
            Console.WriteLine($"Package downloads: {result.TotalDownloads}");
            Console.WriteLine($"Package versions: {result.Versions.Count}");
            Console.WriteLine();

            index++;
        }
    }

    [Fact]
    public async Task Autocomplete()
    {
        // Search for packages whose names' start with "Newt".
        var client = new NuGetClient("https://api.nuget.org/v3/index.json");
        var packageIds = await client.AutocompleteAsync("Newt");

        foreach (var packageId in packageIds)
        {
            Console.WriteLine($"Found package ID '{packageId}'");
        }
    }
}
