using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core.Authentication;

public interface IFeedAuthenticationService
{
    Task<AuthResult> AuthenticateByTokenAsync(string token, CancellationToken cancellationToken);
    Task<AuthResult> AuthenticateByCredentialsAsync(string username, string password, CancellationToken cancellationToken);
}
