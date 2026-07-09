using System;

namespace BaGetter.Core.Entities;

public class UserGroup
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }

    public User User { get; set; }
    public Group Group { get; set; }
}
