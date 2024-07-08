using System;
using System.Text;
using BaGetter.Web.Helper;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BaGetter.Web.Extensions;

public static class HtmlHelperExtensions
{
    /// <remarks>Based on: <see href="https://github.com/NuGet/NuGetGallery/blob/main/src/NuGetGallery/Views/Shared/Gallery/Layout.cshtml"/></remarks>
    public static IHtmlContent AddReleaseMetaTags(this IHtmlHelper htmlHelper)
    {
        var builder = new StringBuilder();

        var ver = ApplicationVersionHelper.GetVersion();
        if (ver.Present)
        {
            WriteMetaTag(builder, "branch", ver.Branch);
            WriteMetaTag(builder, "commit", ver.ShortCommit);
            WriteMetaTag(builder, "time", ver.BuildDateUtc == DateTime.MinValue ? null : ver.BuildDateUtc.ToString("O"));
        }

        return new HtmlString(builder.ToString());
    }

    public static IHtmlContent AddReleaseInformationAsComment(this IHtmlHelper htmlHelper)
    {
        var builder = new StringBuilder();

        var ver = ApplicationVersionHelper.GetVersion();
        if (ver.Present)
        {
            builder.AppendLine("<!--")
                .AppendLine($"This is BaGetter version {ver.Version}.");

            if (!string.IsNullOrEmpty(ver.ShortCommit))
            {
                var commitUri = ver.CommitUri != null ? ver.CommitUri.AbsoluteUri.Replace("git://github.com", "https://github.com") : string.Empty;
                builder.AppendLine($"Deployed from {ver.ShortCommit} Link: {commitUri}");
            }

            if (!string.IsNullOrEmpty(ver.Branch))
            {
                var branchUri = ver.BranchUri != null ? ver.BranchUri.AbsoluteUri : string.Empty;
                builder.AppendLine($"Built on {ver.Branch} Link: {branchUri}");
            }

            if (ver.BuildDateUtc != DateTime.MinValue)
            {
                builder.AppendLine($"Built on {ver.BuildDateUtc.ToString("O")}");
            }

            builder.AppendLine("-->");
        }

        return new HtmlString(builder.ToString());
    }

    private static void WriteMetaTag(StringBuilder builder, string name, string val)
    {
        if (!string.IsNullOrEmpty(val))
        {
            builder.AppendLine($"<meta name=\"deployment-{name}\" content=\"{val}\" />");
        }
    }
}
