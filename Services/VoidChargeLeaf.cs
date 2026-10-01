using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record VoidResult(bool Success, string Outcome);

// T8 — Leaf: calls Stripe reversal API, CRITICAL_DESTROY — irreversible
public class VoidChargeLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(VoidChargeLeaf).GetMethod(nameof(ReverseCharge))!
                              .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "void_charge",
        Task       = "reverse_api",
        Capability = "CRITICAL_DESTROY",
        TimeoutMs  = 5000
    )]
    public async Task<VoidResult> ReverseCharge(string chargeId) =>
        await SpatialTracer.RunAsync(Attr, nameof(ReverseCharge), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(100, cts.Token);
            return new VoidResult(true, "VOIDED");
        });
}
