namespace BaGetter.Core
{
    /// <summary>
    /// The NuGet V3 search request.
    /// </summary>
    /// <remarks>See: <see href="https://docs.microsoft.com/en-us/nuget/api/search-query-service-resource#request-parameters"/></remarks>
    public class SearchRequest
    {
        /// <summary>
        /// The number of results to skip, for pagination.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// The number of results to return, for pagination.
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// Whether to include pre-release packages.
        /// </summary>
        public bool IncludePrerelease { get; set; }

        /// <summary>
        /// Whether to include SemVer 2.0.0 compatible packages.
        /// </summary>
        public bool IncludeSemVer2 { get; set; }

        /// <summary>
        /// Filter results to a package type. If null, no filter is applied.
        /// </summary>
        public string PackageType { get; set; }

        /// <summary>
        /// Filters results to a target framework. If null, no filter is applied.
        /// </summary>
        public string Framework { get; set; }

        /// <summary>
        /// The search query.
        /// </summary>
        public string Query { get; set; }
    }
}
