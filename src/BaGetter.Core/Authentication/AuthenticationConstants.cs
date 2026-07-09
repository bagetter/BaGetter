namespace BaGetter.Core.Authentication;
public static class AuthenticationConstants
{
    public const string NugetBasicAuthenticationScheme = "NugetBasicAuthentication";
    public const string NugetUserPolicy = "NuGetUserPolicy";
    public const string EntraOidcScheme = "EntraOidc";
    public const string CookieScheme = "BaGetterCookie";
    public const string CookieName = "BaGetter.Auth";

    /// <summary>Claim stamped into the cookie that indicates whether the user is an admin.</summary>
    public const string IsAdminClaim = "bagetter:is_admin";
}
