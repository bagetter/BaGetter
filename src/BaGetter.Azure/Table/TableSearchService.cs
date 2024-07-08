using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using BaGetter.Core;
using BaGetter.Protocol.Models;
using Microsoft.Extensions.Options;

namespace BaGetter.Azure
{
    public class TableSearchService : ISearchService
    {
        private readonly TableClient _table;
        private readonly ISearchResponseBuilder _responseBuilder;

        public TableSearchService(
            TableServiceClient client,
            ISearchResponseBuilder responseBuilder,
            IOptionsSnapshot<AzureTableOptions> options)
        {
            ArgumentNullException.ThrowIfNull(client, nameof(client));
            ArgumentNullException.ThrowIfNull(responseBuilder, nameof(responseBuilder));
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            _table = client.GetTableClient(options.Value.TableName);
            _responseBuilder = responseBuilder;
        }

        public async Task<SearchResponse> SearchAsync(
            SearchRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            return _responseBuilder.BuildSearch(results);
        }

        public async Task<AutocompleteResponse> AutocompleteAsync(
            AutocompleteRequest request,
            CancellationToken cancellationToken)
        {
            var results = await SearchAsync(
                request.Query,
                request.Skip,
                request.Take,
                request.IncludePrerelease,
                request.IncludeSemVer2,
                cancellationToken);

            var packageIds = results.Select(p => p.PackageId).ToList();

            return _responseBuilder.BuildAutocomplete(packageIds);
        }

        public Task<AutocompleteResponse> ListPackageVersionsAsync(
            VersionsRequest request,
            CancellationToken cancellationToken)
        {
            // TODO: Support versions autocomplete.
            // See: https://github.com/loic-sharma/BaGet/issues/291
            var response = _responseBuilder.BuildAutocomplete(new List<string>());

            return Task.FromResult(response);
        }

        public Task<DependentsResponse> FindDependentsAsync(string packageId, CancellationToken cancellationToken)
        {
            var response = _responseBuilder.BuildDependents(new List<PackageDependent>());

            return Task.FromResult(response);
        }

        private async Task<List<PackageRegistration>> SearchAsync(
            string searchText,
            int skip,
            int take,
            bool includePrerelease,
            bool includeSemVer2,
            CancellationToken cancellationToken)
        {
            var query = _table.QueryAsync<PackageEntity>(GenerateSearchFilter(searchText, includePrerelease, includeSemVer2), cancellationToken: cancellationToken);

            var results = await LoadPackagesAsync(query, maxPartitions: skip + take);

            return results
                .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => new PackageRegistration(group.Key, group.ToList()))
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        private static async Task<IReadOnlyList<Package>> LoadPackagesAsync(
            AsyncPageable<PackageEntity> query,
            int maxPartitions)
        {
            var results = new List<Package>();

            var partitions = 0;
            string lastPartitionKey = null;

            await foreach (var result in query)
            {
                if (lastPartitionKey != result.PartitionKey)
                {
                    lastPartitionKey = result.PartitionKey;
                    partitions++;

                    if (partitions > maxPartitions)
                    {
                        break;
                    }
                }

                results.Add(result.AsPackage());
            }

            return results;
        }

        private static string GenerateSearchFilter(string searchText, bool includePrerelease, bool includeSemVer2)
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var prefix = searchText.TrimEnd().Split(separator: null).Last();

                var prefixLower = prefix;
                var prefixUpper = prefix + "~";

                var partitionLowerFilter = $"PartitionKey ge '{prefixLower}'";
                var partitionUpperFilter = $"PartitionKey le '{prefixUpper}'";

                result = GenerateAnd(partitionLowerFilter, partitionUpperFilter);
            }

            result = GenerateAnd(result, "Listed eq true");

            if (!includePrerelease)
            {
                result = GenerateAnd(result, "IsPrerelease eq false");
            }

            if (!includeSemVer2)
            {
                result = GenerateAnd(result, "SemVerLevel eq 0");
            }

            return result;

            string GenerateAnd(string left, string right)
            {
                if (string.IsNullOrEmpty(left)) return right;

                return $"({left}) and ({right})";
            }
        }
    }
}
