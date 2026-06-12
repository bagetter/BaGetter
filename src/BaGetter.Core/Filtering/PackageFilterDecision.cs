namespace BaGetter.Core;

public class PackageFilterDecision
{
    private PackageFilterDecision(bool isBlocked, PackageFilterRuleOptions matchedRule)
    {
        IsBlocked = isBlocked;
        MatchedRule = matchedRule;
    }

    public bool IsBlocked { get; }

    public PackageFilterRuleOptions MatchedRule { get; }

    public static PackageFilterDecision Allow { get; } = new(false, null);

    public static PackageFilterDecision Block(PackageFilterRuleOptions matchedRule)
        => new(true, matchedRule);
}
