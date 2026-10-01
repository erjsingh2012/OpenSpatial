using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ChargeResult(bool Success, string ChargeId, string Outcome);

// T8 — Leaf: external I/O, hard timeout, explicit outcomes only
public class StripeLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(StripeLeaf).GetMethod(nameof(ChargeStripe))!
                          .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "process_payment",
        Task       = "charge_vendor",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 5000
    )]
    public async Task<ChargeResult> ChargeStripe(decimal total, string token) =>
        await SpatialTracer.RunAsync(Attr, nameof(ChargeStripe), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);

            await Task.Delay(200, cts.Token);

            var chargeId = $"ch_{Guid.NewGuid().ToString()[..8]}";
            return new ChargeResult(true, chargeId, "SETTLED");
        });
}
