using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T6 — Action: selects carrier rate (T7) then books shipment (T8), owns dispatch outcome
public class CarrierAction
{
    private static readonly SpatialAttribute Attr =
        typeof(CarrierAction).GetMethod(nameof(AssignCarrier))!
                             .GetCustomAttribute<SpatialAttribute>()!;

    private readonly ShippingRateTask _rates    = new();
    private readonly ShipmentLeaf     _shipment = new();

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fulfillment",
        Container  = "shipping",
        Component  = "dispatcher",
        Workflow   = "order_flow",
        Action     = "dispatch",
        Task       = "assign_carrier",
        Capability = "STATE_MUTATE"
    )]
    public async Task<ShipmentResult> AssignCarrier(string orderId, string region, decimal orderTotal) =>
        await SpatialTracer.RunAsync(Attr, nameof(AssignCarrier), async () =>
        {
            var rate     = _rates.SelectShippingRate(region, orderTotal);
            var shipment = await _shipment.BookShipment(orderId, rate);
            return shipment;
        });
}
