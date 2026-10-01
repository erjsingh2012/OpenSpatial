using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record FraudScore(bool IsRisky, double Score, string Reason);

// T8 — Leaf: calls external ML scorer, hard timeout
public class FraudScoreLeaf
{
    private static readonly SpatialAttribute Attr =
        typeof(FraudScoreLeaf).GetMethod(nameof(FetchFraudScore))!
                              .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "fraud",
        Container  = "screening",
        Component  = "scorer",
        Workflow   = "detection_flow",
        Action     = "pre_auth",
        Task       = "score_api",
        Capability = "DATA_ACCESS",
        TimeoutMs  = 3000
    )]
    public async Task<FraudScore> FetchFraudScore(string token, decimal amount) =>
        await SpatialTracer.RunAsync(Attr, nameof(FetchFraudScore), async () =>
        {
            using var cts = new CancellationTokenSource(Attr.TimeoutMs);
            await Task.Delay(50, cts.Token);

            // Realistic threshold: flag orders over $10,000 or known bad tokens
            var score   = (amount > 10_000m || token.StartsWith("tok_fraud")) ? 0.82 : 0.12;
            var isRisky = score > 0.65;
            return new FraudScore(isRisky, score, isRisky ? "AMOUNT_THRESHOLD" : "CLEAR");
        });
}
