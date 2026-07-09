using System;
using System.Collections.Generic;

namespace BaGetter.Core.Entities;

public class Group
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string AppRoleValue { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<UserGroup> UserGroups { get; set; }
}
