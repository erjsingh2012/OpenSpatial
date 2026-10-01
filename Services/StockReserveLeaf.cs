using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ReservationResult(bool Reserved, string ReservationId);

// T8 — Leaf: decrements warehouse stock, writes to inventory DB
public class StockReserveLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(StockReserveLeaf).GetMethod(nameof(DecrementStock))!
                                .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "inventory",
        Container  = "warehouse",
        Component  = "stock",
        Workflow   = "order_flow",
        Action     = "reserve",
        Task       = "decrement_stock",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 3000
    )]
    public async Task<ReservationResult> DecrementStock(string sku, int qty) =>
        await SpatialTracer.RunAsync(Attr, nameof(DecrementStock), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(60, cts.Token);
            var rsvId = $"rsv_{Guid.NewGuid().ToString()[..8]}";
            return new ReservationResult(true, rsvId);
        });
}
