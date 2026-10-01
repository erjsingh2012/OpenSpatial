using OpenSpatial.Spatial;
using System.Reflection;

namespace OpenSpatial.Services;

public record PaymentRequest(string OrderId, decimal Amount, string Region, string Token);
public record PaymentResponse(bool Success, string ChargeId, decimal Tax, decimal Total, string Outcome);

// T6 — Action: orchestrates T7 + T8, owns the outcome
public class PaymentAction
{
    private readonly TaxTask   _tax    = new();
    private readonly StripeLeaf _stripe = new();

    [Spatial(
        Ecosystem  = "platform",
        Context    = "billing",
        Container  = "checkout",
        Component  = "payment",
        Workflow   = "order_flow",
        Action     = "process_payment",
        Task       = "execute",
        Capability = "STATE_MUTATE"
    )]
    public async Task<PaymentResponse> ProcessPayment(PaymentRequest req)
    {
        PrintCoordinate();

        // T7: pure calculation — no I/O
        var tax   = _tax.CalculateTax(req.Amount, req.Region);
        var total = req.Amount + tax;

        // T8: external I/O with hard cutoff
        var charge = await _stripe.ChargeStripe(total, req.Token);

        return charge.Success
            ? new PaymentResponse(true,  charge.ChargeId, tax, total, "SETTLED")
            : new PaymentResponse(false, "",              tax, total, charge.Outcome);
    }

    private void PrintCoordinate([System.Runtime.CompilerServices.CallerMemberName] string method = "")
    {
        var attr = GetType().GetMethod(method)?.GetCustomAttribute<SpatialAttribute>();
        if (attr is not null)
            Console.WriteLine($"  [T6 Action] {attr.Coordinate}.{method}");
    }
}
