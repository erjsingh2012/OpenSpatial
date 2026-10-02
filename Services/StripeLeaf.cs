using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ChargeResult(bool Success, string ChargeId, string Outcome);

// T8 — Leaf: external I/O, hard timeout, explicit outcomes only
public class StripeLeaf
{
    private static readonly SpatialAttribute     Attr    =
        typeof(StripeLeaf).GetMethod(nameof(ChargeStripe))!
                          .GetCustomAttribute<SpatialAttribute>()!;
    private static readonly MonitoringAttribute  Monitor =
        typeof(StripeLeaf).GetMethod(nameof(ChargeStripe))!
                          .GetCustomAttribute<MonitoringAttribute>()!;

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
    // 0xFF73: all paths on · output@10% · sink@25% · E/U@100%
    // Override at runtime: MonitoringRuntime.SetPath(coord, 0xFF7B) to crank sink to 100% during incident
    [Monitoring(
        Path      = 0xFF73,
        EventIn   = "payment.requested",
        EventOut  = "payment.settled  | P1:SETTLED_FULL, P2:SETTLED_PARTIAL, P3:SETTLED_DEFERRED",
        EventSink = "payment.failed   | S1:DECLINED, S2:TIMEOUT, S3:INSUFFICIENT_FUNDS"
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
