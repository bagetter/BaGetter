using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NuGet.Versioning;
using Xunit;

namespace BaGetter.Core.Tests;

public class PackagePolicyEvaluatorTests
{
    [Fact]
    public void DisabledConfigAllowsPackage()
    {
        var target = CreateTarget(
            enabled: false,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "*" });

        var result = target.Evaluate(Upstream("Example.Package", "2.0.0"));

        Assert.False(target.IsFilteringEnabled);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void EmptyRulesAllowPackage()
    {
        var target = CreateTarget(enabled: true);

        var result = target.Evaluate(Upstream("Example.Package", "2.0.0"));

        Assert.False(target.IsFilteringEnabled);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void ExactPackageIdBlocksAllVersions()
    {
        var target = CreateTarget(
            enabled: true,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "*" });

        Assert.True(target.IsFilteringEnabled);
        Assert.True(target.Evaluate(Upstream("Example.Package", "1.0.0")).IsBlocked);
        Assert.True(target.Evaluate(Upstream("Example.Package", "2.0.0")).IsBlocked);
    }

    [Fact]
    public void VersionRangeBlocksMatchingVersionsOnly()
    {
        var target = CreateTarget(
            enabled: true,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "[2.0.0,)" });

        Assert.False(target.Evaluate(Upstream("Example.Package", "1.9.0")).IsBlocked);
        Assert.True(target.Evaluate(Upstream("Example.Package", "2.0.0")).IsBlocked);
        Assert.True(target.Evaluate(Upstream("Example.Package", "3.0.0")).IsBlocked);
    }

    [Fact]
    public void WildcardBlocksMatchingPackageIds()
    {
        var target = CreateTarget(
            enabled: true,
            new PackageFilterRuleOptions { PackageId = "Example.Tools.*", Versions = "*" });

        Assert.True(target.Evaluate(Upstream("Example.Tools.Build", "1.0.0")).IsBlocked);
        Assert.False(target.Evaluate(Upstream("Example.Tools", "1.0.0")).IsBlocked);
    }

    [Fact]
    public void PackageIdMatchingIsCaseInsensitive()
    {
        var target = CreateTarget(
            enabled: true,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "*" });

        Assert.True(target.Evaluate(Upstream("example.package", "2.0.0")).IsBlocked);
    }

    [Fact]
    public void CachedPackageIsAllowedWhenCacheBlockingIsDisabled()
    {
        var target = CreateTarget(
            enabled: true,
            blockCachedPackages: false,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "*" });

        var result = target.Evaluate(new PackageFilterContext(
            "Example.Package",
            NuGetVersion.Parse("2.0.0"),
            PackageFilterScope.CachedUpstream));

        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void LocalPackageIsAlwaysAllowed()
    {
        var target = CreateTarget(
            enabled: true,
            new PackageFilterRuleOptions { PackageId = "Example.Package", Versions = "*" });

        var result = target.Evaluate(new PackageFilterContext(
            "Example.Package",
            NuGetVersion.Parse("2.0.0"),
            PackageFilterScope.Local));

        Assert.False(result.IsBlocked);
    }

    private static PackagePolicyEvaluator CreateTarget(
        bool enabled,
        params PackageFilterRuleOptions[] rules)
        => CreateTarget(enabled, blockCachedPackages: true, rules);

    private static PackagePolicyEvaluator CreateTarget(
        bool enabled,
        bool blockCachedPackages,
        params PackageFilterRuleOptions[] rules)
    {
        var options = Options.Create(new PackageFilteringOptions
        {
            Enabled = enabled,
            BlockCachedPackages = blockCachedPackages,
            Rules = new List<PackageFilterRuleOptions>(rules)
        });

        return new PackagePolicyEvaluator(
            options,
            Mock.Of<ILogger<PackagePolicyEvaluator>>());
    }

    private static PackageFilterContext Upstream(string id, string version)
        => new(id, NuGetVersion.Parse(version), PackageFilterScope.Upstream);
}
