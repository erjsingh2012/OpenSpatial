using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: deterministic order ID assignment, no I/O
public class OrderIdTask
{
    private static readonly SpatialAttribute Attr =
        typeof(OrderIdTask).GetMethod(nameof(AssignOrderId))!
                           .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "orders",
        Container  = "management",
        Component  = "writer",
        Workflow   = "order_flow",
        Action     = "create",
        Task       = "assign_id",
        Capability = "DATA_ACCESS"
    )]
    public string AssignOrderId(string orderId) =>
        SpatialTracer.Run(Attr, nameof(AssignOrderId), () =>
            $"{orderId}-{DateTime.UtcNow:yyyyMMdd}");
}
