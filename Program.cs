using OpenSpatial.Services;
using OpenSpatial.Spatial;
using System.Reflection;

Console.WriteLine("=== OpenSpatial — 3-Tier Payment Flow ===\n");

var payment = new PaymentAction();

var request = new PaymentRequest(
    OrderId: "ORD-001",
    Amount:  100.00m,
    Region:  "CA",
    Token:   "tok_test_visa"
);

Console.WriteLine($"Input:  Order {request.OrderId} · ${request.Amount} · Region {request.Region}");
Console.WriteLine("─────────────────────────────────────────");

var result = await payment.ProcessPayment(request);

Console.WriteLine("─────────────────────────────────────────");
Console.WriteLine($"Tax:    ${result.Tax}");
Console.WriteLine($"Total:  ${result.Total}");
Console.WriteLine($"Charge: {result.ChargeId}");
Console.WriteLine($"Status: {result.Outcome}");

// Month 2: generate .spatial/manifest.json from all [Spatial] methods
ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
