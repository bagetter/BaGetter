using System;

namespace BaGetter.Web.Pages.Admin;

/// <summary>
/// One feed-permission row posted by the bulk "Save all" form on the Groups page.
/// </summary>
public class FeedPermissionInput
{
    public Guid FeedId { get; set; }
    public bool CanPull { get; set; }
    public bool CanPush { get; set; }
    public bool CanDelete { get; set; }
}
