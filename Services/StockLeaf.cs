using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record StockLevel(string Sku, int Available, string WarehouseId);

// T8 — Leaf: fetches live stock from warehouse API, hard timeout
public class StockLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(StockLeaf).GetMethod(nameof(FetchStockLevel))!
                         .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "inventory",
        Container  = "warehouse",
        Component  = "stock",
        Workflow   = "order_flow",
        Action     = "pre_check",
        Task       = "fetch_stock",
        Capability = "DATA_ACCESS",
        TimeoutMs  = 2000
    )]
    public async Task<StockLevel> FetchStockLevel(string sku) =>
        await SpatialTracer.RunAsync(Attr, nameof(FetchStockLevel), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(40, cts.Token);
            return new StockLevel(sku, 47, "WH-SFO-01");
        });
}
