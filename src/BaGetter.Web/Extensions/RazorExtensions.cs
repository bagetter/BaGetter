using Humanizer;

namespace BaGetter.Web.Extensions;

public static class RazorExtensions
{
    public static string ToMetric(this long value)
    {
        return ((double) value).ToMetric();
    }
}
