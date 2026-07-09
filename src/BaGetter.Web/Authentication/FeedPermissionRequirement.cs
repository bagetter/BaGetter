using Microsoft.AspNetCore.Authorization;

namespace BaGetter.Web.Authentication;

public class FeedPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public FeedPermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public const string Pull = "Pull";
    public const string Push = "Push";
    public const string Delete = "Delete";
    public const string Admin = "Admin";
}
