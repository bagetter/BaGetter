using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class MirrorOptions : IValidatableObject
{
    /// <summary>
    /// If true, packages that aren't found locally will be indexed
    /// using the upstream source.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The v3 index that will be mirrored.
    /// </summary>
    public Uri PackageSource { get; set; }

    /// <summary>
    /// Whether or not the package source is a v2 package source feed.
    /// </summary>
    public bool Legacy { get; set; }

    /// <summary>
    /// The time before a download from the package source times out.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PackageDownloadTimeoutSeconds { get; set; } = 600;

    public MirrorAuthenticationOptions Authentication { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (PackageSource == null)
        {
            yield return new ValidationResult(
                $"The {nameof(PackageSource)} configuration is required if mirroring is enabled",
                [nameof(PackageSource)]);
        }

        if (Authentication != null)
        {
            if (Legacy && Authentication.Type is not (MirrorAuthenticationType.None or MirrorAuthenticationType.Basic))
            {
                yield return new ValidationResult(
                    "Legacy v2 feeds only support basic authentication",
                    [nameof(Legacy), nameof(Authentication)]);
            }

            switch (Authentication.Type)
            {
                case MirrorAuthenticationType.Basic:
                    if (string.IsNullOrEmpty(Authentication.Username))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Username)} configuration is required for basic authentication",
                            [nameof(Authentication.Username)]);
                    }

                    if (string.IsNullOrEmpty(Authentication.Password))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Password)} configuration is required for basic authentication",
                            [nameof(Authentication.Password)]);
                    }
                    break;

                case MirrorAuthenticationType.Bearer:
                    if (string.IsNullOrEmpty(Authentication.Token))
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.Token)} configuration is required for bearer authentication",
                            [nameof(Authentication.Token)]);
                    }
                    break;

                case MirrorAuthenticationType.Custom:
                    if (Authentication.CustomHeaders == null)
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.CustomHeaders)} configuration is required for custom authentication",
                            [nameof(Authentication.CustomHeaders)]);
                        break;
                    }

                    if (Authentication.CustomHeaders.Count == 0)
                    {
                        yield return new ValidationResult(
                            $"The {nameof(Authentication.CustomHeaders)} configuration has no headers defined." +
                            $" Use \"{nameof(Authentication.Type)}\": \"{nameof(MirrorAuthenticationType.None)}\" instead if you intend you use no authentication.",
                            [nameof(Authentication.CustomHeaders)]);
                    }
                    break;
            }
        }
    }
}
