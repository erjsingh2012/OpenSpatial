using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record ChargeResult(bool Success, string ChargeId, string Outcome);

// T8 — Leaf: external I/O, hard timeout, explicit outcomes only
public class StripeLeaf
{
    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "process_payment",
        Task       = "charge_vendor",
        Capability = "STATE_MUTATE"
    )]
    public async Task<ChargeResult> ChargeStripe(decimal total, string token)
    {
        PrintCoordinate();

        // Hard cutoff — T8 Leaf must never hang
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            // Simulated Stripe call (replace with real Stripe SDK later)
            await Task.Delay(200, cts.Token);

            var chargeId = $"ch_{Guid.NewGuid().ToString()[..8]}";
            Console.WriteLine($"  [T8 Leaf]  Stripe charged ${total} → {chargeId}");

            return new ChargeResult(true, chargeId, "SETTLED");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  [T8 Leaf]  SINK: STRIPE_GATEWAY_TIMEOUT");
            return new ChargeResult(false, "", "TIMEOUT");
        }
    }

    private void PrintCoordinate([System.Runtime.CompilerServices.CallerMemberName] string method = "")
    {
        var attr = GetType().GetMethod(method)?.GetCustomAttribute<SpatialAttribute>();
        if (attr is not null)
            Console.WriteLine($"  [T8 Leaf]  {attr.Coordinate}.{method}");
    }
}
