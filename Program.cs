using OpenSpatial.Services;
using OpenSpatial.Spatial;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Reflection;

// ── sptree CLI ─────────────────────────────────────────────────────────────
// Usage:
//   dotnet run -- tree                          → full C4 map
//   dotnet run -- tree platform.billing         → filter by coordinate prefix
//   dotnet run -- tree --cap STATE_MUTATE       → filter by capability
// ──────────────────────────────────────────────────────────────────────────
if (args.Length > 0 && args[0] == "tree")
{
    string? filter = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
    string? cap    = null;

    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--cap") cap = args[i + 1];

    SpatialTree.Print(filter: filter, capability: cap);
    return;
}

// ── Payment demo ───────────────────────────────────────────────────────────
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("openspatial")
    .AddConsoleExporter()
    .Build();

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

ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
