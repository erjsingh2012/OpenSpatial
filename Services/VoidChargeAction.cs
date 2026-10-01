using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record VoidRequest(string ChargeId, string Reason);

// T6 — Action: orchestrates charge reversal, CRITICAL_DESTROY — no undo
public class VoidChargeAction
{
    private static readonly SpatialAttribute Attr =
        typeof(VoidChargeAction).GetMethod(nameof(VoidCharge))!
                                .GetCustomAttribute<SpatialAttribute>()!;

    private readonly VoidChargeLeaf _leaf = new();

    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "void_charge",
        Task       = "execute",
        Capability = "CRITICAL_DESTROY"
    )]
    public async Task<VoidResult> VoidCharge(VoidRequest req) =>
        await SpatialTracer.RunAsync(Attr, nameof(VoidCharge), async () =>
            await _leaf.ReverseCharge(req.ChargeId));
}
