using System;
using System.Collections.Generic;
using BaGetter.Core.Configuration;
using Microsoft.Extensions.Options;

namespace BaGetter;

/// <summary>
/// BaGetter's options configuration, specific to the default BaGetter application.
/// Don't use this if you are embedding BaGetter into your own custom ASP.NET Core application.
/// </summary>
public class ValidateBaGetterOptions
    : IValidateOptions<BaGetterOptions>
{
    private static readonly HashSet<string> _validDatabaseTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AzureTable",
            "MySql",
            "PostgreSql",
            "Sqlite",
            "SqlServer",
        };

    private static readonly HashSet<string> _validStorageTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AliyunOss",
            "AwsS3",
            "AzureBlobStorage",
            "Filesystem",
            "GoogleCloud",
            "TencentCos",
            "Null"
        };

    private static readonly HashSet<string> _validSearchTypes
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

        if (!_validDatabaseTypes.Contains(options.Database?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Database)}:{nameof(DatabaseOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", _validDatabaseTypes)}");
        }

        if (!_validStorageTypes.Contains(options.Storage?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Storage)}:{nameof(StorageOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", _validStorageTypes)}");
        }

        if (!_validSearchTypes.Contains(options.Search?.Type))
        {
            failures.Add(
                $"The '{nameof(BaGetterOptions.Search)}:{nameof(SearchOptions.Type)}' config is invalid. " +
                $"Allowed values: {string.Join(", ", _validSearchTypes)}");
        }

        ValidateAuthentication(options, failures);

        if (failures.Count != 0) return ValidateOptionsResult.Fail(failures);

        return ValidateOptionsResult.Success;
    }

    private static void ValidateAuthentication(BaGetterOptions options, List<string> failures)
    {
        var auth = options.Authentication;
        if (auth == null)
            return;

        var mode = auth.Mode;

        if (mode == AuthenticationMode.Config)
            return;

        if (mode is AuthenticationMode.Entra or AuthenticationMode.Hybrid)
        {
            if (auth.Entra == null)
            {
                failures.Add($"The '{nameof(NugetAuthenticationOptions.Entra)}' config is required when Authentication Mode is '{mode}'");
            }
            else
            {
                if (string.IsNullOrEmpty(auth.Entra.TenantId))
                    failures.Add($"The '{nameof(EntraOptions.TenantId)}' config is required for Entra authentication");

                if (string.IsNullOrEmpty(auth.Entra.ClientId))
                    failures.Add($"The '{nameof(EntraOptions.ClientId)}' config is required for Entra authentication");

                if (string.IsNullOrEmpty(auth.Entra.Instance))
                    failures.Add($"The '{nameof(EntraOptions.Instance)}' config is required for Entra authentication");
            }
        }

        if (auth.MaxTokenExpiryDays < 1)
            failures.Add($"The '{nameof(NugetAuthenticationOptions.MaxTokenExpiryDays)}' config must be at least 1");

        if (auth.MaxFailedAttempts < 1)
            failures.Add($"The '{nameof(NugetAuthenticationOptions.MaxFailedAttempts)}' config must be at least 1");

        if (auth.LockoutMinutes < 1)
            failures.Add($"The '{nameof(NugetAuthenticationOptions.LockoutMinutes)}' config must be at least 1");
    }
}
