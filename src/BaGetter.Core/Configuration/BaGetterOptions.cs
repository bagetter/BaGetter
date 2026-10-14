using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BaGetter.Core.Configuration;

namespace BaGetter.Core;

public class BaGetterOptions : IValidatableObject
{
    /// <summary>
    /// The API Key required to authenticate package
    /// operations. If <see cref="ApiKeys"/> and  <see cref="ApiKey"/> are not set, package operations do not require authentication.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Optional hash for the legacy <see cref="ApiKey"/> in format:
    /// PBKDF2$&lt;iterations&gt;$&lt;base64Salt&gt;$&lt;base64Hash&gt;.
    /// If set, this is used in addition to <see cref="ApiKey"/>.
    /// </summary>
    public string ApiKeyHash { get; set; }

    /// <summary>
    /// The application root URL for usage in reverse proxy scenarios.
    /// </summary>
    public string PathBase { get; set; }

    /// <summary>
    /// If enabled, the database will be updated at app startup by running
    /// Entity Framework migrations. This is not recommended in production.
    /// </summary>
    public bool RunMigrationsAtStartup { get; set; } = true;

    /// <summary>
    /// How BaGetter should interpret package deletion requests.
    /// </summary>
    public PackageDeletionBehavior PackageDeletionBehavior { get; set; } = PackageDeletionBehavior.Unlist;

    /// <summary>
    /// If enabled, pushing a package that already exists will replace the
    /// existing package.
    /// </summary>
    public PackageOverwriteAllowed AllowPackageOverwrites { get; set; } = PackageOverwriteAllowed.False;

    /// <summary>
    /// If true, disables package pushing, deleting, and re-listing.
    /// </summary>
    public bool IsReadOnlyMode { get; set; } = false;

    /// <summary>
    /// The URLs the BaGetter server will use.
    /// As per documentation <a href="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-3.1#server-urls">here (Server URLs)</a>.
    /// </summary>
    public string Urls { get; set; }

    /// <summary>
    /// The maximum package size in GB.
    /// Attempted uploads of packages larger than this will be rejected with an internal server error carrying one <see cref="System.IO.InvalidDataException"/>.
    /// </summary>
    public uint MaxPackageSizeGiB { get; set; } = 8;

    /// <summary>
    /// The number of package versions to include in a single registration page.
    /// If a package has more versions than this value, the registration index will return paged entries.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int RegistrationPageSize { get; set; } = 64;

    /// <summary>
    /// If this is set to a value, it will limit the number of versions that can be pushed for a package.
    /// the older versions will be deleted.
    /// This setting is not used anymore and is deprecated.
    /// </summary>
    [Obsolete("MaxVersionsPerPackage is deprecated. Please configure RetentionOptions parameters instead.")]
    public uint? MaxVersionsPerPackage { get; set; } = null;

    public RetentionOptions Retention { get; set; }

    public DatabaseOptions Database { get; set; }

    public StorageOptions Storage { get; set; }

    public SearchOptions Search { get; set; }

    public MirrorOptions Mirror { get; set; }

    /// <summary>
    /// Multiple mirrors to use for read-through caching. When this list is set and non-empty,
    /// it takes precedence over <see cref="Mirror"/>.
    /// </summary>
    public IList<MirrorOptions> Mirrors { get; set; }

    public HealthCheckOptions HealthCheck { get; set; }

    public StatisticsOptions Statistics { get; set; }

    public RequestRateLimitOptions RequestRateLimit { get; set; } = new();

    public CorsPolicyOptions Cors { get; set; } = new();

    public SecurityHeadersOptions SecurityHeaders { get; set; } = new();

    public LocalizationOptions Localization { get; set; } = new();

    public SearchReindexOptions Reindex { get; set; } = new();

    public NugetAuthenticationOptions Authentication { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxPackageSizeGiB == 0)
        {
            yield return new ValidationResult(
                $"{nameof(MaxPackageSizeGiB)} must be greater than 0.",
                new[] { nameof(MaxPackageSizeGiB) });
        }

        if (!string.IsNullOrEmpty(PathBase))
        {
            if (!PathBase.StartsWith('/'))
            {
                yield return new ValidationResult(
                    $"{nameof(PathBase)} must start with '/'.",
                    new[] { nameof(PathBase) });
            }

            if (PathBase.Length > 1 && PathBase.EndsWith('/'))
            {
                yield return new ValidationResult(
                    $"{nameof(PathBase)} must not end with '/' unless it is '/'.",
                    new[] { nameof(PathBase) });
            }
        }

        if (Cors != null)
        {
            if (Cors.AllowAnyOrigin && Cors.AllowCredentials)
            {
                yield return new ValidationResult(
                    $"{nameof(Cors)}.{nameof(CorsPolicyOptions.AllowCredentials)} cannot be true when {nameof(Cors)}.{nameof(CorsPolicyOptions.AllowAnyOrigin)} is true.",
                    new[] { nameof(Cors) });
            }

            if (!Cors.AllowAnyOrigin && (Cors.AllowedOrigins == null || Cors.AllowedOrigins.Length == 0))
            {
                yield return new ValidationResult(
                    $"{nameof(Cors)}.{nameof(CorsPolicyOptions.AllowedOrigins)} must contain at least one origin when {nameof(Cors)}.{nameof(CorsPolicyOptions.AllowAnyOrigin)} is false.",
                    new[] { nameof(Cors) });
            }
        }

        if (SecurityHeaders is { EnableHsts: true, Enabled: false })
        {
            yield return new ValidationResult(
                $"{nameof(SecurityHeaders)}.{nameof(SecurityHeadersOptions.EnableHsts)} requires {nameof(SecurityHeaders)}.{nameof(SecurityHeadersOptions.Enabled)} to be true.",
                new[] { nameof(SecurityHeaders) });
        }

        if (Authentication?.Ldap?.Enabled == true)
        {
            if (string.IsNullOrWhiteSpace(Authentication.Ldap.Server))
            {
                yield return new ValidationResult(
                    $"{nameof(Authentication)}.{nameof(NugetAuthenticationOptions.Ldap)}.{nameof(LdapAuthenticationOptions.Server)} is required when LDAP authentication is enabled.",
                    [nameof(Authentication)]);
            }

            if (string.IsNullOrWhiteSpace(Authentication.Ldap.BaseDn))
            {
                yield return new ValidationResult(
                    $"{nameof(Authentication)}.{nameof(NugetAuthenticationOptions.Ldap)}.{nameof(LdapAuthenticationOptions.BaseDn)} is required when LDAP authentication is enabled.",
                    [nameof(Authentication)]);
            }

            var hasBindDn = !string.IsNullOrWhiteSpace(Authentication.Ldap.BindDn);
            var hasBindPassword = !string.IsNullOrWhiteSpace(Authentication.Ldap.BindPassword);
            if (hasBindDn != hasBindPassword)
            {
                yield return new ValidationResult(
                    $"{nameof(Authentication)}.{nameof(NugetAuthenticationOptions.Ldap)} requires both {nameof(LdapAuthenticationOptions.BindDn)} and {nameof(LdapAuthenticationOptions.BindPassword)} when either is configured.",
                    [nameof(Authentication)]);
            }

            if (Authentication.Ldap.Port is <= 0 or > 65535)
            {
                yield return new ValidationResult(
                    $"{nameof(Authentication)}.{nameof(NugetAuthenticationOptions.Ldap)}.{nameof(LdapAuthenticationOptions.Port)} must be between 1 and 65535 when set.",
                    [nameof(Authentication)]);
            }
        }

        var mirrors = GetConfiguredMirrors();
        var useMirrorList = Mirrors is { Count: > 0 };
        for (var i = 0; i < mirrors.Count; i++)
        {
            var mirror = mirrors[i];
            var prefix = useMirrorList ? $"{nameof(Mirrors)}[{i}]" : nameof(Mirror);

            if (mirror == null)
            {
                yield return new ValidationResult(
                    $"{prefix} must not be null.",
                    [prefix]);
                continue;
            }

            var mirrorValidationResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                mirror,
                new ValidationContext(mirror),
                mirrorValidationResults,
                validateAllProperties: true);

            foreach (var validationResult in mirrorValidationResults)
            {
                var members = validationResult.MemberNames?.Any() == true
                    ? validationResult.MemberNames.Select(m => $"{prefix}.{m}")
                    : [prefix];

                yield return new ValidationResult(
                    $"{prefix}: {validationResult.ErrorMessage}",
                    members);
            }
        }
    }

    public IReadOnlyList<MirrorOptions> GetConfiguredMirrors()
    {
        if (Mirrors is { Count: > 0 })
        {
            return Mirrors.ToList();
        }

        return Mirror == null ? Array.Empty<MirrorOptions>() : [Mirror];
    }
}
