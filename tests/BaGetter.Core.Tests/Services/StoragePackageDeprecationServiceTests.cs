using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Tests.Support;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests.Services;

public class StoragePackageDeprecationServiceTests
{
    private readonly FakeStorageService _storage = new();
    private readonly StoragePackageDeprecationService _target;
    private readonly CancellationToken _token = CancellationToken.None;

    public StoragePackageDeprecationServiceTests()
    {
        _target = new StoragePackageDeprecationService(_storage, NullLogger<StoragePackageDeprecationService>.Instance);
    }

    [Fact]
    public async Task CreatesNewDeprecation()
    {
        var info = new PackageDeprecationInfo
        {
            Reasons = new[] { "Legacy" },
            Message = "Use newer package"
        };

        await _target.DeprecateAsync("Core", NuGetVersion.Parse("1.0.0"), info, _token);

        var result = await _target.GetOrNullAsync("Core", NuGetVersion.Parse("1.0.0"), _token);
        Assert.NotNull(result);
        Assert.Equal("Legacy", Assert.Single(result.Reasons));
        Assert.Equal("Use newer package", result.Message);
    }

    [Fact]
    public async Task OverwritesExistingDeprecationOnConflict()
    {
        var v1 = new PackageDeprecationInfo { Reasons = new[] { "Legacy" }, Message = "Old" };
        var v2 = new PackageDeprecationInfo { Reasons = new[] { "CriticalBugs" }, Message = "New message" };

        var id = "Core";
        var version = NuGetVersion.Parse("2.0.0");

        await _target.DeprecateAsync(id, version, v1, _token);
        await _target.DeprecateAsync(id, version, v2, _token);

        var result = await _target.GetOrNullAsync(id, version, _token);
        Assert.NotNull(result);
        Assert.Equal("CriticalBugs", Assert.Single(result.Reasons));
        Assert.Equal("New message", result.Message);
    }

    [Fact]
    public async Task AttachAsyncPopulatesDeprecation()
    {
        var id = "Core";
        var version = NuGetVersion.Parse("3.0.0");
        var info = new PackageDeprecationInfo { Reasons = new[] { "Other" }, Message = "Policy" };

        await _target.DeprecateAsync(id, version, info, _token);

        var packages = new List<Package>
        {
            new Package { Id = id, Version = version }
        };

        await _target.AttachAsync(packages, _token);

        Assert.NotNull(packages[0].Deprecation);
        Assert.Equal("Other", Assert.Single(packages[0].Deprecation.Reasons));
    }

    private class FakeStorageService : IStorageService
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!_store.TryGetValue(path, out var bytes))
            {
                throw new FileNotFoundException();
            }

            return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
        }

        public Task<Uri> GetDownloadUriAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Uri>(null);
        }

        public Task<StoragePutResult> PutAsync(string path, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var newBytes = ms.ToArray();

            if (_store.TryGetValue(path, out var existing))
            {
                if (AreEqual(existing, newBytes))
                {
                    return Task.FromResult(StoragePutResult.AlreadyExists);
                }

                return Task.FromResult(StoragePutResult.Conflict);
            }

            _store[path] = newBytes;
            return Task.FromResult(StoragePutResult.Success);
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            _store.Remove(path);
            return Task.CompletedTask;
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }
    }
}
