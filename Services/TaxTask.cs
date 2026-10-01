using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure calculation, no I/O
public class TaxTask
{
    private static readonly SpatialAttribute Attr =
        typeof(TaxTask).GetMethod(nameof(CalculateTax))!
                       .GetCustomAttribute<SpatialAttribute>()!;

    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "process_payment",
        Task       = "calculate_tax",
        Capability = "DATA_ACCESS"
    )]
    public decimal CalculateTax(decimal amount, string region) =>
        SpatialTracer.Run(Attr, nameof(CalculateTax), () =>
        {
            var rate = region switch
            {
                "CA" => 0.0875m,
                "NY" => 0.08m,
                _    => 0.05m
            };
            return Math.Round(amount * rate, 2);
        });
}
