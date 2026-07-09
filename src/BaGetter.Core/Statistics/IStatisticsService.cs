using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaGetter.Core.Statistics;

public interface IStatisticsService
{
    Task<int> GetPackagesTotalAmount(Guid feedId);
    Task<int> GetVersionsTotalAmount(Guid feedId);
    IEnumerable<string> GetKnownServices();
}
