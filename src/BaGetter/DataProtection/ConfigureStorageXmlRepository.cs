using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace BaGetter.DataProtection;

/// <summary>
/// Points Data Protection at the <see cref="StorageServiceXmlRepository"/>. Configured via options
/// (rather than a <c>PersistKeysTo*</c> call) so the repository can resolve <c>IStorageService</c>
/// from the application's service provider.
/// </summary>
internal sealed class ConfigureStorageXmlRepository : IConfigureOptions<KeyManagementOptions>
{
    private readonly IServiceProvider _services;

    public ConfigureStorageXmlRepository(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public void Configure(KeyManagementOptions options)
    {
        options.XmlRepository = new StorageServiceXmlRepository(_services);
    }
}
