using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure order validation rules, no I/O
public class OrderValidatorTask
{
    private static readonly SpatialAttribute Attr =
        typeof(OrderValidatorTask).GetMethod(nameof(ValidateOrderItems))!
                                  .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "orders",
        Container  = "management",
        Component  = "validator",
        Workflow   = "order_flow",
        Action     = "create",
        Task       = "validate",
        Capability = "DATA_ACCESS"
    )]
    public bool ValidateOrderItems(string sku, int qty, decimal unitPrice) =>
        SpatialTracer.Run(Attr, nameof(ValidateOrderItems), () =>
            !string.IsNullOrEmpty(sku) && qty > 0 && unitPrice > 0);
}
