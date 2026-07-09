using System;

namespace BaGetter.Core.Authentication;

public record AuthResult(bool IsAuthenticated, Guid? UserId, string Username);
