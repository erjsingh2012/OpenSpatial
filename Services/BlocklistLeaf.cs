using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record BlocklistResult(bool Blocked, string Reason);

// T8 — Leaf: checks token against fraud blocklist API
public class BlocklistLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(BlocklistLeaf).GetMethod(nameof(CheckBlocklist))!
                             .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fraud",
        Container  = "screening",
        Component  = "blocklist",
        Workflow   = "detection_flow",
        Action     = "blocklist",
        Task       = "lookup",
        Capability = "DATA_ACCESS",
        TimeoutMs  = 1500
    )]
    public async Task<BlocklistResult> CheckBlocklist(string token) =>
        await SpatialTracer.RunAsync(Attr, nameof(CheckBlocklist), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(20, cts.Token);
            return new BlocklistResult(false, "CLEAR");
        });
}
