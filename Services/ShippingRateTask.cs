using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record CarrierRate(string Carrier, decimal Cost, int EstimatedDays);

// T7 — Task: pure rate selection logic, no I/O
public class ShippingRateTask
{
    private static readonly SpatialAttribute Attr =
        typeof(ShippingRateTask).GetMethod(nameof(SelectShippingRate))!
                                .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fulfillment",
        Container  = "shipping",
        Component  = "dispatcher",
        Workflow   = "order_flow",
        Action     = "dispatch",
        Task       = "select_rate",
        Capability = "DATA_ACCESS"
    )]
    public CarrierRate SelectShippingRate(string region, decimal orderTotal) =>
        SpatialTracer.Run(Attr, nameof(SelectShippingRate), () =>
        {
            // Free shipping over $500, else UPS Ground
            return orderTotal >= 500m
                ? new CarrierRate("UPS Ground", 0.00m,  3)
                : new CarrierRate("USPS First", 12.50m, 5);
        });
}
