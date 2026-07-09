using System;

namespace BaGetter.Core.Entities;

public class FeedPermission
{
    public Guid Id { get; set; }
    public Guid FeedId { get; set; }
    public PrincipalType PrincipalType { get; set; }
    public Guid PrincipalId { get; set; }
    public bool CanPush { get; set; }
    public bool CanPull { get; set; }
    public bool CanDelete { get; set; }
    public PermissionSource Source { get; set; }

    public Feed Feed { get; set; }
}
