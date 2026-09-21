using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaGetter.Core;

public class SearchReindexService : ISearchReindexService
{
    private readonly IContext _context;
    private readonly ISearchIndexer _indexer;
    private readonly IOptions<BaGetterOptions> _options;
    private readonly ILogger<SearchReindexService> _logger;

    public SearchReindexService(
        IContext context,
        ISearchIndexer indexer,
        IOptions<BaGetterOptions> options,
        ILogger<SearchReindexService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> ReindexAsync(CancellationToken cancellationToken)
    {
        var batchSize = _options.Value.Reindex?.BatchSize ?? 100;
        var indexedCount = 0;
        var lastSeenKey = 0;

        _logger.LogInformation("Starting search reindex (batch size: {BatchSize})", batchSize);

        while (true)
        {
            var batch = await _context.Packages
                .AsNoTracking()
                .Where(p => p.Key > lastSeenKey)
                .OrderBy(p => p.Key)
                .Include(p => p.Dependencies)
                .Include(p => p.PackageTypes)
                .Include(p => p.TargetFrameworks)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var package in batch)
            {
                await _indexer.IndexAsync(package, cancellationToken);
                indexedCount++;
            }

            lastSeenKey = batch[^1].Key;
        }

        _logger.LogInformation("Finished search reindex. Indexed {PackageCount} packages.", indexedCount);
        return indexedCount;
    }
}
