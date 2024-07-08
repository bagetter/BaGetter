using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaGetter.Core.Statistics;

public interface IStatisticsService
{
    Task<int> GetPackagesTotalAmount();
    Task<int> GetVersionsTotalAmount();
    IEnumerable<string> GetKnownServices();
}
