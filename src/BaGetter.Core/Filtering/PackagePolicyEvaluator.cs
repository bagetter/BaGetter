using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Versioning;

namespace BaGetter.Core;

public class PackagePolicyEvaluator : IPackagePolicyEvaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly PackageFilteringOptions _options;
    private readonly ILogger<PackagePolicyEvaluator> _logger;
    private readonly List<CompiledRule> _rules;

    public PackagePolicyEvaluator(
        IOptions<PackageFilteringOptions> options,
        ILogger<PackagePolicyEvaluator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _rules = CompileRules(_options.Rules);
    }

    public bool IsFilteringEnabled => _options.Enabled && _rules.Count > 0;

    public PackageFilterDecision Evaluate(PackageFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!IsFilteringEnabled ||
            context.Scope == PackageFilterScope.Local ||
            (context.Scope == PackageFilterScope.CachedUpstream && !_options.BlockCachedPackages))
        {
            return PackageFilterDecision.Allow;
        }

        foreach (var rule in _rules)
        {
            if (!rule.PackageIdPattern.IsMatch(context.PackageId))
            {
                continue;
            }

            if (!rule.AllVersions &&
                (context.Version == null || !rule.VersionRange.Satisfies(context.Version)))
            {
                continue;
            }

            _logger.LogWarning(
                "Blocked package {PackageId} {PackageVersion} by rule {RulePackageId} {RuleVersions} in scope {PackageFilterScope}",
                context.PackageId,
                context.Version,
                rule.Options.PackageId,
                rule.Options.Versions,
                context.Scope);

            return PackageFilterDecision.Block(rule.Options);
        }

        return PackageFilterDecision.Allow;
    }

    private List<CompiledRule> CompileRules(IReadOnlyList<PackageFilterRuleOptions> rules)
    {
        var result = new List<CompiledRule>();
        if (rules == null)
        {
            return result;
        }

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule?.PackageId))
            {
                continue;
            }

            var versions = string.IsNullOrWhiteSpace(rule.Versions) ? "*" : rule.Versions.Trim();
            var allVersions = versions == "*";
            VersionRange versionRange = null;

            if (!allVersions && !VersionRange.TryParse(versions, out versionRange))
            {
                _logger.LogWarning(
                    "Ignoring package filtering rule {RulePackageId} because version range {RuleVersions} is invalid",
                    rule.PackageId,
                    rule.Versions);
                continue;
            }

            var pattern = "^" + Regex.Escape(rule.PackageId.Trim()).Replace(@"\*", ".*") + "$";
            result.Add(new CompiledRule(
                rule,
                new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout),
                allVersions,
                versionRange));
        }

        return result;
    }

    private sealed record CompiledRule(
        PackageFilterRuleOptions Options,
        Regex PackageIdPattern,
        bool AllVersions,
        VersionRange VersionRange);
}
