using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// Stores package deprecation metadata alongside packages using the configured <see cref="IStorageService"/>.
/// This keeps database schemas untouched while still exposing deprecation information via the V3 registration API.
/// </summary>
public class StoragePackageDeprecationService : IPackageDeprecationService
{
    private const string DeprecationsPrefix = "deprecations";
    private const string JsonContentType = "application/json";

    private readonly IStorageService _storage;
    private readonly ILogger<StoragePackageDeprecationService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public StoragePackageDeprecationService(IStorageService storage, ILogger<StoragePackageDeprecationService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> DeprecateAsync(string id, NuGetVersion version, PackageDeprecationInfo deprecation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deprecation);

        var results = deprecation.Validate(new ValidationContext(deprecation));
        if (results?.Any() == true)
        {
            var message = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Invalid deprecation payload: {message}");
        }

        var normalizedId = id.ToLowerInvariant();
        var normalizedVersion = version.ToNormalizedString().ToLowerInvariant();
        var path = BuildPath(normalizedId, normalizedVersion);

        var payload = JsonSerializer.Serialize(deprecation.ToProtocolModel(), SerializerOptions);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        _logger.LogInformation("Writing deprecation for {PackageId} {PackageVersion} to {Path}", normalizedId, normalizedVersion, path);
        var result = await _storage.PutAsync(path, stream, JsonContentType, cancellationToken);

        // If the entry already exists with different content, overwrite it to allow updating deprecations.
        if (result == StoragePutResult.Conflict)
        {
            _logger.LogInformation("Existing deprecation found for {PackageId} {PackageVersion}, overwriting", normalizedId, normalizedVersion);
            await _storage.DeleteAsync(path, cancellationToken);

            await using var retry = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            await _storage.PutAsync(path, retry, JsonContentType, cancellationToken);
        }

        return true;
    }

    public async Task<PackageDeprecation> GetOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var normalizedId = id.ToLowerInvariant();
        var normalizedVersion = version.ToNormalizedString().ToLowerInvariant();
        var path = BuildPath(normalizedId, normalizedVersion);

        try
        {
            await using var stream = await _storage.GetAsync(path, cancellationToken);
            if (stream == null) return null;

            return await JsonSerializer.DeserializeAsync<PackageDeprecation>(stream, SerializerOptions, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
    {
        var normalizedId = id.ToLowerInvariant();
        var normalizedVersion = version.ToNormalizedString().ToLowerInvariant();
        var path = BuildPath(normalizedId, normalizedVersion);
        await _storage.DeleteAsync(path, cancellationToken);
    }

    public async Task AttachAsync(IEnumerable<Package> packages, CancellationToken cancellationToken)
    {
        if (packages == null) return;

        foreach (var package in packages)
        {
            try
            {
                package.Deprecation = await GetOrNullAsync(package.Id, package.Version, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to attach deprecation info for {PackageId} {PackageVersion}", package.Id, package.Version);
                package.Deprecation = null;
            }
        }
    }

    private static string BuildPath(string normalizedId, string normalizedVersion)
    {
        return Path.Combine(DeprecationsPrefix, normalizedId, $"{normalizedVersion}.json");
    }
}
