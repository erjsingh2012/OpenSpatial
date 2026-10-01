using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure relevance scoring, no I/O
public class RankerTask
{
    private static readonly SpatialAttribute Attr =
        typeof(RankerTask).GetMethod(nameof(RankByRelevance))!
                          .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "catalog",
        Container  = "search",
        Component  = "ranker",
        Workflow   = "catalog_flow",
        Action     = "product_search",
        Task       = "rank_results",
        Capability = "DATA_ACCESS"
    )]
    public IReadOnlyList<ProductResult> RankByRelevance(IReadOnlyList<ProductResult> results, string query) =>
        SpatialTracer.Run(Attr, nameof(RankByRelevance), () =>
            results.OrderByDescending(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                   .ToList());
}
