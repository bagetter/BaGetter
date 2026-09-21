using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

public interface INugetCredentialValidator
{
    Task<NugetCredentialValidationResult> ValidateAsync(
        string username,
        string password,
        CancellationToken cancellationToken);
}
