using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using BaGetter.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Azure
{
    /// <summary>
    /// Stores the metadata of packages using Azure Table Storage.
    /// </summary>
    public class TablePackageDatabase : IPackageDatabase
    {
        private const int MaxPreconditionFailures = 5;

        private readonly TableClient _table;
        private readonly ILogger<TablePackageDatabase> _logger;

        public TablePackageDatabase(
            TableServiceClient client,
            ILogger<TablePackageDatabase> logger,
            IOptions<AzureTableOptions> options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(logger);

            _table = client.GetTableClient(options.Value.TableName);
            _logger = logger;
        }

        public async Task<PackageAddResult> AddAsync(Package package, CancellationToken cancellationToken)
        {
            try
            {
                var entity = TableOperationBuilder.GetEntity(package);

                await _table.AddEntityAsync(entity, cancellationToken);
            }
            catch (RequestFailedException e) when (e.IsAlreadyExistsException())
            {
                return PackageAddResult.PackageAlreadyExists;
            }

            return PackageAddResult.Success;
        }

        public async Task AddDownloadAsync(
            string id,
            NuGetVersion version,
            CancellationToken cancellationToken)
        {
            var attempt = 0;

            while (true)
            {
                try
                {
                    var result = await _table.GetEntityIfExistsAsync<PackageDownloadsEntity>(id, version.ToNormalizedString().ToLowerInvariant(), cancellationToken: cancellationToken);

                    if (!result.HasValue)
                    {
                        return;
                    }

                    var entity = result.Value;

                    entity.Downloads += 1;

                    var updateResponse = await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge, cancellationToken);

                    // Not sure if there's gonna be an exception here so check both ways just in case
                    if(updateResponse.Status == (int?)HttpStatusCode.PreconditionFailed && attempt < MaxPreconditionFailures)
                    {
                        attempt++;
                        _logger.LogWarning(
                            "Retrying due to precondition failure, attempt {Attempt} of {MaxPreconditionFailures}",
                            attempt, MaxPreconditionFailures);
                        continue;
                    }

                    return;
                }
                catch (RequestFailedException e)
                    when (attempt < MaxPreconditionFailures && e.IsPreconditionFailedException())
                {
                    attempt++;
                    _logger.LogWarning(
                        e,
                        "Retrying due to precondition failure, attempt {Attempt} of {MaxPreconditionFailures}",
                        attempt, MaxPreconditionFailures);
                }
            }
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
        {
            var query = _table.QueryAsync<PackageEntity>(p =>
                            p.PartitionKey.Equals(id, StringComparison.InvariantCultureIgnoreCase),
                            1,
                            MinimalColumnSet,
                            cancellationToken);

            await foreach(var _ in query)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> ExistsAsync(
            string id,
            NuGetVersion version,
            CancellationToken cancellationToken)
        {
            var query = _table.QueryAsync<PackageEntity>(p =>
                                p.PartitionKey.Equals(id, StringComparison.InvariantCultureIgnoreCase) &&
                                p.RowKey.Equals(version.ToNormalizedString(), StringComparison.InvariantCultureIgnoreCase),
                                1,
                                MinimalColumnSet,
                                cancellationToken);

            await foreach (var _ in query)
            {
                return true;
            }

            return false;
        }

        public async Task<IReadOnlyList<Package>> FindAsync(string id, bool includeUnlisted, CancellationToken cancellationToken)
        {
            const int maxPerPage = 500;
            var query =
                includeUnlisted
                ? _table.QueryAsync<PackageEntity>(p => p.PartitionKey.Equals(id, StringComparison.InvariantCultureIgnoreCase), maxPerPage, cancellationToken: cancellationToken)
                : _table.QueryAsync<PackageEntity>(p => p.PartitionKey.Equals(id, StringComparison.InvariantCultureIgnoreCase) && p.Listed, maxPerPage, cancellationToken: cancellationToken);

            var results = new List<Package>();
            await foreach (var entity in query)
            {
                results.Add(entity.AsPackage());
            }

            return results.OrderBy(p => p.Version).ToList();
        }

        public async Task<Package> FindOrNullAsync(
            string id,
            NuGetVersion version,
            bool includeUnlisted,
            CancellationToken cancellationToken)
        {
            var result = await _table.GetEntityIfExistsAsync<PackageEntity>(id.ToLowerInvariant(), version.ToNormalizedString(), cancellationToken: cancellationToken);

            if (!result.HasValue)
            {
                return null;
            }

            var entity = result.Value;

            // Filter out the package if it's unlisted.
            if (!includeUnlisted && !entity.Listed)
            {
                return null;
            }

            return entity.AsPackage();
        }

        public async Task<bool> HardDeletePackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            var result = await _table.DeleteEntityAsync(id, version.ToNormalizedString().ToLowerInvariant(), cancellationToken: cancellationToken);
            return !result.IsError;
        }

        public async Task<bool> RelistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            var result = await _table.GetEntityIfExistsAsync<PackageListingEntity>(id, version.ToNormalizedString().ToLowerInvariant(), cancellationToken: cancellationToken);

            if (!result.HasValue)
            {
                return false;
            }

            var entity = result.Value;

            entity.Listed = true;

            await _table.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Merge, cancellationToken);

            return true;
        }

        public async Task<bool> UnlistPackageAsync(string id, NuGetVersion version, CancellationToken cancellationToken)
        {
            var result = await _table.GetEntityIfExistsAsync<PackageListingEntity>(id, version.ToNormalizedString().ToLowerInvariant(), cancellationToken: cancellationToken);

            if (!result.HasValue)
            {
                return false;
            }

            var entity = result.Value;

            entity.Listed = false;

            await _table.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Merge, cancellationToken);

            return true;
        }

        private static List<string> MinimalColumnSet => ["PartitionKey"];
    }
}
