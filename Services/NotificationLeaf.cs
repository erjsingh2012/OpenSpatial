using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record EmailResult(bool Sent, string MessageId);

// T8 — Leaf: sends receipt email via external mail provider
public class NotificationLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(NotificationLeaf).GetMethod(nameof(SendReceiptEmail))!
                                .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "notifications",
        Container  = "email",
        Component  = "sender",
        Workflow   = "order_flow",
        Action     = "notify",
        Task       = "send_api",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 3000
    )]
    public async Task<EmailResult> SendReceiptEmail(string orderId, decimal total) =>
        await SpatialTracer.RunAsync(Attr, nameof(SendReceiptEmail), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(80, cts.Token);
            var msgId = $"msg_{Guid.NewGuid().ToString()[..8]}";
            return new EmailResult(true, msgId);
        });
}
