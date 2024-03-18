using System;

namespace BaGetter.Web.Helper;

internal class ApplicationVersion
{
    public static readonly ApplicationVersion Empty = new();

    /// <summary>
    /// Indicates if all version information was given during build process.
    /// </summary>
    public bool Present { get; private set; }

    /// <summary>
    /// The version e.g. <c>7.8.9</c>
    /// </summary>
    public string Version { get; private set; }
    /// <summary>
    /// The version with additional information e.g. <c>7.8.9+commitHash</c>
    /// </summary>
    public string InformationalVersion { get; private set; }
    public string Branch { get; private set; }
    public string Commit { get; private set; }
    public DateTime BuildDateUtc { get; private set; }
    public string Authors { get; private set; }

    // Calculated
    public string ShortCommit { get; private set; }
    public Uri BranchUri { get; private set; }
    public Uri CommitUri { get; private set; }

    private ApplicationVersion()
    {
        Present = false;
        Version = "1.0";
        Branch = string.Empty;
        Commit = string.Empty;
        BuildDateUtc = DateTime.UtcNow;
    }

    public ApplicationVersion(Uri repositoryBase, string informationalVersion, string version, string branch, string commit, DateTime buildDateUtc, string authors)
    {
        Present = true;
        InformationalVersion = informationalVersion;
        Version = version;
        Branch = branch;
        Commit = commit;
        BuildDateUtc = buildDateUtc;
        Authors = authors;

        ShortCommit = string.IsNullOrEmpty(Commit) ? string.Empty : Commit.Substring(0, Math.Min(10, Commit.Length));

        if (repositoryBase != null)
        {
            BranchUri = CombineUri(repositoryBase, "tree/" + branch);
            CommitUri = CombineUri(repositoryBase, "commit/" + ShortCommit);
        }
    }

    private static Uri CombineUri(Uri repositoryBase, string relative)
    {
        var builder = new UriBuilder(repositoryBase);
        if (string.IsNullOrEmpty(builder.Path))
        {
            builder.Path = relative;
        }
        else
        {
            if (!builder.Path.EndsWith('/'))
            {
                builder.Path += "/";
            }

            builder.Path += relative;
        }

        return builder.Uri;
    }
}
