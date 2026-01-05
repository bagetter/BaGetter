using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BaGetter.Protocol.Models;

namespace BaGetter.Core;

/// <summary>
/// Internal representation of a package deprecation request.
/// </summary>
public class PackageDeprecationInfo : IValidatableObject
{
    /// <summary>
    /// Deprecation reasons as case-insensitive values: Legacy, CriticalBugs, Other.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; init; }

    /// <summary>
    /// Optional custom message describing the deprecation.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Optional alternate package id that should be used instead.
    /// </summary>
    public string AlternatePackageId { get; init; }

    /// <summary>
    /// Optional alternate package version range (or exact version).
    /// </summary>
    public string AlternatePackageRange { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Reasons == null || Reasons.Count == 0)
        {
            yield return new ValidationResult("At least one deprecation reason is required", new[] { nameof(Reasons) });
            yield break;
        }

        var allowed = new HashSet<string>(new[] { "Legacy", "CriticalBugs", "Other" }, System.StringComparer.OrdinalIgnoreCase);
        foreach (var reason in Reasons)
        {
            if (!allowed.Contains(reason))
            {
                yield return new ValidationResult($"Unsupported deprecation reason '{reason}'. Allowed: Legacy, CriticalBugs, Other.", new[] { nameof(Reasons) });
            }
        }
    }

    public PackageDeprecation ToProtocolModel()
    {
        return new PackageDeprecation
        {
            Reasons = Reasons?.ToList(),
            Message = Message,
            AlternatePackage = string.IsNullOrWhiteSpace(AlternatePackageId)
                ? null
                : new AlternatePackage
                {
                    Id = AlternatePackageId,
                    Range = string.IsNullOrWhiteSpace(AlternatePackageRange) ? "*" : AlternatePackageRange
                }
        };
    }
}
