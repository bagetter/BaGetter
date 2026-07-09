using System;
using System.Collections.Generic;

namespace BaGetter.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public AuthProvider AuthProvider { get; set; }
    public string EntraObjectId { get; set; }
    public string PasswordHash { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsAdmin { get; set; }
    public bool CanLoginToUI { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; }
    public List<PersonalAccessToken> PersonalAccessTokens { get; set; }
    public List<UserGroup> UserGroups { get; set; }
}
