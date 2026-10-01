using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

// T7 — Task: pure calculation, no I/O
public class TaxTask
{
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
    public decimal CalculateTax(decimal amount, string region)
    {
        PrintCoordinate();

        var rate = region switch
        {
            "CA" => 0.0875m,
            "NY" => 0.08m,
            _    => 0.05m
        };

        return Math.Round(amount * rate, 2);
    }

    private void PrintCoordinate([System.Runtime.CompilerServices.CallerMemberName] string method = "")
    {
        var attr = GetType().GetMethod(method)?.GetCustomAttribute<SpatialAttribute>();
        if (attr is not null)
            Console.WriteLine($"  [T7 Task]  {attr.Coordinate}.{method}");
    }
}
