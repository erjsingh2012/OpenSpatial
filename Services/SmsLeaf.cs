using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record SmsResult(bool Sent, string SmsId);

// T8 — Leaf: sends SMS via Twilio gateway, hard timeout
public class SmsLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(SmsLeaf).GetMethod(nameof(SendOrderSMS))!
                       .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "notifications",
        Container  = "sms",
        Component  = "sender",
        Workflow   = "order_flow",
        Action     = "notify",
        Task       = "send_sms",
        Capability = "STATE_MUTATE",
        TimeoutMs  = 3000
    )]
    public async Task<SmsResult> SendOrderSMS(string orderId, string phone) =>
        await SpatialTracer.RunAsync(Attr, nameof(SendOrderSMS), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(60, cts.Token);
            var smsId = $"sms_{Guid.NewGuid().ToString()[..8]}";
            return new SmsResult(true, smsId);
        });
}
