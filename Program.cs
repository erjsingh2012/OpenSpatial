using OpenSpatial.Services;
using OpenSpatial.Spatial;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Reflection;

// ── sptree CLI ─────────────────────────────────────────────────────────────
// Usage:
//   dotnet run -- tree                          → full C4 ASCII map
//   dotnet run -- tree --c1                     → C1 System Context only
//   dotnet run -- tree --c2                     → C1 + C2 Containers
//   dotnet run -- tree --c3                     → C1 + C2 + C3 Components
//   dotnet run -- tree --c4                     → full code diagram (default)
//   dotnet run -- tree --md                     → Mermaid diagram (any level)
//   dotnet run -- tree --cap STATE_MUTATE       → filter by capability
//   dotnet run -- tree platform.billing         → filter by coordinate prefix
// ──────────────────────────────────────────────────────────────────────────
if (args.Length > 0 && args[0] == "tree")
{
    string? filter   = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
    string? cap      = null;
    int     level    = 4;
    bool    markdown = false;

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--cap" && i + 1 < args.Length) cap      = args[i + 1];
        if (args[i] == "--c1") level = 1;
        if (args[i] == "--c2") level = 2;
        if (args[i] == "--c3") level = 3;
        if (args[i] == "--c4") level = 4;
        if (args[i] == "--md") markdown = true;
    }

    if (markdown)
        SpatialTree.PrintMermaid(filter: filter, capability: cap, level: level);
    else
        SpatialTree.Print(filter: filter, capability: cap, level: level);

    return;
}

// ── Multi-domain payment demo ───────────────────────────────────────────────
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("openspatial")
    .AddConsoleExporter()
    .Build();

Console.WriteLine("=== OpenSpatial — Multi-Domain Payment Flow ===\n");

var request = new PaymentRequest(
    OrderId: "ORD-001",
    Amount:  100.00m,
    Region:  "CA",
    Token:   "tok_test_visa"
);

Console.WriteLine($"Input:  Order {request.OrderId} · ${request.Amount} · Region {request.Region}");
Console.WriteLine("─────────────────────────────────────────");

// ── platform.fraud: pre-auth screening ────────────────────────────────────
var fraud       = new FraudAction();
var fraudResult = await fraud.EvaluateRisk(request.Token, request.Amount);
Console.WriteLine($"Fraud:  Score {fraudResult.Score:F2} · {fraudResult.Reason}");

if (fraudResult.IsRisky)
{
    Console.WriteLine("─────────────────────────────────────────");
    Console.WriteLine("Status: DECLINED (fraud threshold exceeded)");
    ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
    return;
}

// ── platform.billing: tax + charge ────────────────────────────────────────
var payment = new PaymentAction();
var result  = await payment.ProcessPayment(request);

// ── platform.notifications: receipt ───────────────────────────────────────
var notif = new NotificationLeaf();
var email = await notif.SendReceiptEmail(request.OrderId, result.Total);

Console.WriteLine("─────────────────────────────────────────");
Console.WriteLine($"Tax:    ${result.Tax}");
Console.WriteLine($"Total:  ${result.Total}");
Console.WriteLine($"Charge: {result.ChargeId}");
Console.WriteLine($"Email:  {email.MessageId}");
Console.WriteLine($"Status: {result.Outcome}");

ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
