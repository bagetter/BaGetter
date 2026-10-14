using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core;

public interface ISearchReindexService
{
    Task<int> ReindexAsync(CancellationToken cancellationToken);
}
