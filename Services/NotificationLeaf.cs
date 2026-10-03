using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record EmailResult(bool Sent, string MessageId);

// T8 — Leaf: sends receipt email via external mail provider
public class NotificationLeaf
{
    private static readonly SpatialAttribute    Attr    =
        typeof(NotificationLeaf).GetMethod(nameof(SendReceiptEmail))!
                                .GetCustomAttribute<SpatialAttribute>()!;
    private static readonly MonitoringAttribute Monitor =
        typeof(NotificationLeaf).GetMethod(nameof(SendReceiptEmail))!
                                .GetCustomAttribute<MonitoringAttribute>()!;

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
    // 0xD96F: P1/P2/S1/S2/Uk on, P3/S3/E_ off · output@10% · sink@10% · U@100%
    // U_k always on — lost notification (I_k > O_k+S_k) = silent delivery failure
    [Monitoring(
        Path      = 0xD96F,
        EventIn   = "notification.email.requested",
        EventOut  = "notification.email.sent     | P1:DELIVERED, P2:QUEUED",
        EventSink = "notification.email.failed   | S1:PROVIDER_ERROR, S2:INVALID_ADDRESS"
    )]
    public async Task<EmailResult> SendReceiptEmail(string orderId, decimal total) =>
        await SpatialTracer.RunAsync(Attr, Monitor, nameof(SendReceiptEmail), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(80, cts.Token);
            var msgId = $"msg_{Guid.NewGuid().ToString()[..8]}";
            SpatialTracer.DeclareOutput("P1");   // P1:DELIVERED
            return new EmailResult(true, msgId);
        });
}
