using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ShipmentResult(string ShipmentId, string Carrier, decimal Cost, int EstimatedDays);

// T8 — Leaf: books shipment via carrier API, hard timeout
public class ShipmentLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(ShipmentLeaf).GetMethod(nameof(BookShipment))!
                            .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fulfillment",
        Container  = "shipping",
        Component  = "dispatcher",
        Workflow   = "order_flow",
        Action     = "dispatch",
        Task       = "book_shipment",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 5000
    )]
    public async Task<ShipmentResult> BookShipment(string orderId, CarrierRate rate) =>
        await SpatialTracer.RunAsync(Attr, nameof(BookShipment), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(150, cts.Token);
            var shipId = $"SHIP-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            return new ShipmentResult(shipId, rate.Carrier, rate.Cost, rate.EstimatedDays);
        });
}
