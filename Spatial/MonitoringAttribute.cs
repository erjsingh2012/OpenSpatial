using System;

namespace OpenSpatial.Spatial;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MonitoringAttribute : Attribute
{
    // 16-bit path config (AI-generated default; overridable at runtime via MonitoringRuntime)
    // High byte = enable mask  [P1|P2|P3|S1|S2|S3|E_|Uk]
    // Low byte  = sample rates [Out:3bits|Sink:3bits|Eflag|Uflag]
    public ushort Path      { get; set; } = 0xFF6F;

    public string EventIn   { get; set; } = "";
    public string EventOut  { get; set; } = "";
    public string EventSink { get; set; } = "";
}
