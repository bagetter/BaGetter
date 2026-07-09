using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.ServiceIndex;
using BaGetter.Protocol.Models;
using Microsoft.AspNetCore.Mvc;

namespace BaGetter.Web.Controllers;

/// <summary>
/// The NuGet Service Index. This aids NuGet client to discover this server's services.
/// This is an unauthenticated endpoint, as it is used by clients to discover the endpoint URLs for this server.
/// </summary>

public class ServiceIndexController : Controller
{
    private readonly IServiceIndexService _serviceIndex;

    public ServiceIndexController(IServiceIndexService serviceIndex)
    {
        _serviceIndex = serviceIndex ?? throw new ArgumentNullException(nameof(serviceIndex));
    }

    // GET v3/index
    [HttpGet]
    public async Task<ServiceIndexResponse> GetAsync(CancellationToken cancellationToken)
    {
        return await _serviceIndex.GetAsync(cancellationToken);
    }
}
