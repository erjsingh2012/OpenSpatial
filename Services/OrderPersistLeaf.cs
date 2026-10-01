using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record OrderRecord(string OrderId, decimal Total, string Status);

// T8 — Leaf: writes order record to database
public class OrderPersistLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(OrderPersistLeaf).GetMethod(nameof(PersistOrder))!
                                .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "orders",
        Container  = "management",
        Component  = "writer",
        Workflow   = "order_flow",
        Action     = "create",
        Task       = "persist",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 3000
    )]
    public async Task<OrderRecord> PersistOrder(string orderId, decimal total) =>
        await SpatialTracer.RunAsync(Attr, nameof(PersistOrder), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(70, cts.Token);
            return new OrderRecord(orderId, total, "CREATED");
        });
}
