namespace BaGetter.Core;

public interface IPackagePolicyEvaluator
{
    bool IsFilteringEnabled { get; }

    PackageFilterDecision Evaluate(PackageFilterContext context);
}
