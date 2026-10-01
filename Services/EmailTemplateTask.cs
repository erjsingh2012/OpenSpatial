using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure template rendering, no I/O
public class EmailTemplateTask
{
    private static readonly SpatialAttribute Attr =
        typeof(EmailTemplateTask).GetMethod(nameof(RenderEmailTemplate))!
                                 .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "notifications",
        Container  = "email",
        Component  = "templater",
        Workflow   = "order_flow",
        Action     = "notify",
        Task       = "render",
        Capability = "DATA_ACCESS"
    )]
    public string RenderEmailTemplate(string templateName, string orderId) =>
        SpatialTracer.Run(Attr, nameof(RenderEmailTemplate), () =>
            $"[{templateName}] Order {orderId} — rendered at {DateTime.UtcNow:HH:mm:ss}Z");
}
