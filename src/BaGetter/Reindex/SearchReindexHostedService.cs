using System;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter;

public class SearchReindexHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ILogger<SearchReindexHostedService> _logger;

    public SearchReindexHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BaGetterOptions> options,
        ILogger<SearchReindexHostedService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reindexOptions = _options.Value.Reindex;
        if (reindexOptions == null || !reindexOptions.Enabled)
        {
            return;
        }

        if (reindexOptions.RunOnStartup)
        {
            await RunOnceAsync(stoppingToken);
        }

        if (reindexOptions.IntervalMinutes <= 0)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(reindexOptions.IntervalMinutes), stoppingToken);
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISearchReindexService>();
            var count = await service.ReindexAsync(cancellationToken);
            _logger.LogInformation("Background search reindex completed. Indexed {PackageCount} packages.", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background search reindex failed.");
        }
    }
}
