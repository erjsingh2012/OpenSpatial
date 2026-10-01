using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T6 — Action: orchestrates fraud scoring, owns the risk decision
public class FraudAction
{
    private static readonly SpatialAttribute Attr =
        typeof(FraudAction).GetMethod(nameof(EvaluateRisk))!
                           .GetCustomAttribute<SpatialAttribute>()!;

    private readonly FraudScoreLeaf _scorer = new();

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fraud",
        Container  = "screening",
        Component  = "scorer",
        Workflow   = "detection_flow",
        Action     = "pre_auth",
        Task       = "evaluate",
        Capability = "DATA_ACCESS"
    )]
    public async Task<FraudScore> EvaluateRisk(string token, decimal amount) =>
        await SpatialTracer.RunAsync(Attr, nameof(EvaluateRisk), async () =>
            await _scorer.FetchFraudScore(token, amount));
}
