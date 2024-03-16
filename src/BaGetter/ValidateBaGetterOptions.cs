using System;
using System.Collections.Generic;
using System.Linq;
using BaGetter.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace BaGetter;

/// <summary>
/// BaGetter's options configuration, specific to the default BaGetter application.
/// Don't use this if you are embedding BaGetter into your own custom ASP.NET Core application.
/// </summary>
public class ValidateBaGetterOptions
    : IValidateOptions<BaGetterOptions>
{
    private static readonly HashSet<string> ValidDatabaseTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AzureTable",
            "MySql",
            "PostgreSql",
            "Sqlite",
            "SqlServer",
        };

    private static readonly HashSet<string> ValidStorageTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AliyunOss",
            "AwsS3",
            "AzureBlobStorage",
            "Filesystem",
            "GoogleCloud",
            "Null",
        };

    private static readonly HashSet<string> ValidSearchTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AzureSearch",
            "Database",
            "Null",
        };

    public ValidateOptionsResult Validate(string name, BaGetterOptions options)
    {
        var failures = new List<string>();

        if (options.Database == null) failures.Add($"The '{nameof(BaGetterOptions.Database)}' config is required");
        if (options.Mirror == null) failures.Add($"The '{nameof(BaGetterOptions.Mirror)}' config is required");
        if (options.Search == null) failures.Add($"The '{nameof(BaGetterOptions.Search)}' config is required");
        if (options.Storage == null) failures.Add($"The '{nameof(BaGetterOptions.Storage)}' config is required");

        if (!ValidDatabaseTypes.Contains(options.Database?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Database)}:{nameof(DatabaseOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", ValidDatabaseTypes)}");
        }

        if (!ValidStorageTypes.Contains(options.Storage?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Storage)}:{nameof(StorageOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", ValidStorageTypes)}");
        }

        if (!ValidSearchTypes.Contains(options.Search?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Search)}:{nameof(SearchOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", ValidSearchTypes)}");
        }

        if (failures.Count != 0) return ValidateOptionsResult.Fail(failures);

        return ValidateOptionsResult.Success;
    }
}
