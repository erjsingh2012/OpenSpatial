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
// Usage:
//   dotnet run -- tree              → full C4 diagram
//   dotnet run -- tree --c1         → C1 System Context only
//   dotnet run -- tree --c2         → C1 + C2 Containers
//   dotnet run -- tree --c3         → C1 + C2 + C3 Components
//   dotnet run -- tree --c4         → full code diagram (same as default)
//   dotnet run -- tree --cap STATE_MUTATE   → filter by capability
//   dotnet run -- tree platform.billing     → filter by coordinate prefix
if (args.Length > 0 && args[0] == "tree")
{
    string? filter = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
    string? cap    = null;
    int     level  = 4;

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--cap" && i + 1 < args.Length) cap   = args[i + 1];
        if (args[i] == "--c1") level = 1;
        if (args[i] == "--c2") level = 2;
        if (args[i] == "--c3") level = 3;
        if (args[i] == "--c4") level = 4;
    }

    SpatialTree.Print(filter: filter, capability: cap, level: level);
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
