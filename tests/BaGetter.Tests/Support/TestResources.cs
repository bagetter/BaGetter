using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace BaGetter.Tests;

public static class TestResources
{
    private const string ResourcePrefix = "BaGetter.Tests.TestData.";
    private static readonly Regex VersionRegex = new(@"<version>.*?</version>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex TagsRegex = new(@"<tags>.*?</tags>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex AuthorsRegex = new(@"<authors>.*?</authors>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Test package created with the following properties:
    ///
    ///     <Authors>Test author</Authors>
    ///     <PackageDescription>Test description</PackageDescription>
    ///     <PackageVersion>1.2.3</PackageVersion>
    ///     <IncludeSymbols>true</IncludeSymbols>
    ///     <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    /// </summary>
    public const string Package = "TestData.1.2.3.nupkg";
    public const string SymbolPackage = "TestData.1.2.3.snupkg";

    /// <summary>
    /// Buffer the resource stream into memory so the caller doesn't have to dispose.
    /// </summary>
    public static MemoryStream GetResourceStream(string resourceName)
    {
        using var resourceStream = typeof(TestResources)
            .Assembly
            .GetManifestResourceStream(ResourcePrefix + resourceName);

        if (resourceStream == null)
        {
            return null;
        }

        var bufferedStream = new MemoryStream();
        using (resourceStream)
        {
            resourceStream.CopyTo(bufferedStream);
        }

        bufferedStream.Position = 0;
        return bufferedStream;
    }

    public static MemoryStream GetPackageStreamWithVersion(string version)
        => GetPackageStreamWithVersionAndTags(version, tags: null);

    public static MemoryStream GetPackageStreamWithVersionAndTags(string version, string[] tags)
        => GetPackageStreamWithVersionAndMetadata(version, tags, authors: null);

    public static MemoryStream GetPackageStreamWithVersionAndAuthors(string version, string[] authors)
        => GetPackageStreamWithVersionAndMetadata(version, tags: null, authors);

    public static MemoryStream GetPackageStreamWithVersionAndMetadata(string version, string[] tags, string[] authors)
    {
        using var originalPackageStream = GetResourceStream(Package);
        using var sourceArchive = new ZipArchive(originalPackageStream, ZipArchiveMode.Read, leaveOpen: false);

        var output = new MemoryStream();
        using (var targetArchive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries)
            {
                var targetEntry = targetArchive.CreateEntry(entry.FullName);

                using var sourceEntryStream = entry.Open();
                using var targetEntryStream = targetEntry.Open();

                if (entry.FullName.EndsWith(".nuspec", System.StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(sourceEntryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                    var nuspec = reader.ReadToEnd();
                    var updatedNuspec = VersionRegex.Replace(nuspec, $"<version>{version}</version>", 1);
                    updatedNuspec = UpdateTags(updatedNuspec, tags);
                    updatedNuspec = UpdateAuthors(updatedNuspec, authors);

                    using var writer = new StreamWriter(targetEntryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), bufferSize: 1024, leaveOpen: true);
                    writer.Write(updatedNuspec);
                    writer.Flush();
                }
                else
                {
                    sourceEntryStream.CopyTo(targetEntryStream);
                }
            }
        }

        output.Position = 0;
        return output;
    }

    private static string UpdateTags(string nuspec, string[] tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return nuspec;
        }

        var normalizedTags = string.Join(' ', tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
        if (string.IsNullOrEmpty(normalizedTags))
        {
            return nuspec;
        }

        if (TagsRegex.IsMatch(nuspec))
        {
            return TagsRegex.Replace(nuspec, $"<tags>{normalizedTags}</tags>", 1);
        }

        return Regex.Replace(
            nuspec,
            "</metadata>",
            $"<tags>{normalizedTags}</tags></metadata>",
            RegexOptions.IgnoreCase);
    }

    private static string UpdateAuthors(string nuspec, string[] authors)
    {
        if (authors == null || authors.Length == 0)
        {
            return nuspec;
        }

        var normalizedAuthors = string.Join(", ", authors.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()));
        if (string.IsNullOrEmpty(normalizedAuthors))
        {
            return nuspec;
        }

        if (AuthorsRegex.IsMatch(nuspec))
        {
            return AuthorsRegex.Replace(nuspec, $"<authors>{normalizedAuthors}</authors>", 1);
        }

        return Regex.Replace(
            nuspec,
            "</metadata>",
            $"<authors>{normalizedAuthors}</authors></metadata>",
            RegexOptions.IgnoreCase);
    }
}
