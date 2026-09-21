namespace BaGetter.Web.Admin;

public sealed record AdminPackageOperationResult(bool Succeeded, string Message)
{
    public static AdminPackageOperationResult Success(string message) => new(true, message);
    public static AdminPackageOperationResult Failure(string message) => new(false, message);
}
