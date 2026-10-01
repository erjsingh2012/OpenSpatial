using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ProductResult(string Sku, string Name, decimal UnitPrice, bool Available);

// T8 — Leaf: queries product search index, hard timeout
public class SearchLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(SearchLeaf).GetMethod(nameof(SearchIndex))!
                          .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "catalog",
        Container  = "search",
        Component  = "indexer",
        Workflow   = "catalog_flow",
        Action     = "product_search",
        Task       = "query_index",
        Capability = "DATA_ACCESS",
        TimeoutMs  = 2000
    )]
    public async Task<ProductResult> SearchIndex(string sku) =>
        await SpatialTracer.RunAsync(Attr, nameof(SearchIndex), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(30, cts.Token);

            return sku switch
            {
                "SKU-A1234" => new ProductResult("SKU-A1234", "iPhone 15 Pro", 999.50m, true),
                "SKU-B5678" => new ProductResult("SKU-B5678", "AirPods Pro",   249.00m, true),
                _            => new ProductResult(sku,         "Unknown",        0.00m,  false)
            };
        });
}
