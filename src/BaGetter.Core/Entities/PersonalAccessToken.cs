using System;

namespace BaGetter.Core.Entities;

public class PersonalAccessToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string TokenHash { get; set; }
    public string TokenPrefix { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsRevoked { get; set; }

    public User User { get; set; }
}
