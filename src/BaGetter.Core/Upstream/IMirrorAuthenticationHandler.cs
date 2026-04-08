using System.Net.Http;

namespace BaGetter.Core;

/// <summary>
/// Provides an HTTP message handler that wraps outgoing mirror requests
/// with authentication (e.g., Azure AD token injection).
/// </summary>
public interface IMirrorAuthenticationHandler
{
    HttpMessageHandler CreateHandler(HttpMessageHandler innerHandler);
}
