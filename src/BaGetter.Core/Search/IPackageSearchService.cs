namespace BaGetter.Core;

/// <summary>
/// Search service exposed to NuGet clients and the web UI. It wraps the configured
/// local search provider with optional upstream search and full-proxy URL rewriting.
/// </summary>
public interface IPackageSearchService : ISearchService
{
}
