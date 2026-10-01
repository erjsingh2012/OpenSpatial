using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure quantity check, no I/O
public class StockValidationTask
{
    private static readonly SpatialAttribute Attr =
        typeof(StockValidationTask).GetMethod(nameof(ValidateQuantity))!
                                   .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "inventory",
        Container  = "warehouse",
        Component  = "stock",
        Workflow   = "order_flow",
        Action     = "pre_check",
        Task       = "validate_qty",
        Capability = "DATA_ACCESS"
    )]
    public bool ValidateQuantity(int available, int requested) =>
        SpatialTracer.Run(Attr, nameof(ValidateQuantity), () => available >= requested);
}
