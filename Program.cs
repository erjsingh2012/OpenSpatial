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

// ── E-Commerce Order Fulfillment Demo ──────────────────────────────────────
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("openspatial")
    .AddConsoleExporter()
    .Build();

// ── Order parameters ────────────────────────────────────────────────────────
const string orderId  = "ORD-2026-001";
const string sku      = "SKU-A1234";
const int    qty      = 2;
const string region   = "CA";
const string token    = "tok_test_visa";
const string phone    = "+14155550199";

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        OpenSpatial — E-Commerce Order Fulfillment           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

// ── Phase 1: Pre-flight ─────────────────────────────────────────────────────
Console.WriteLine("── Phase 1: Pre-flight checks ─────────────────────────────────");

var searcher = new SearchLeaf();
var product  = await searcher.SearchIndex(sku);
Console.WriteLine($"  Catalog:    {product.Name} · ${product.UnitPrice}/unit · {(product.Available ? "IN_STOCK" : "OUT_OF_STOCK")}");

var stockLeaf      = new StockLeaf();
var stockValidator = new StockValidationTask();
var stock          = await stockLeaf.FetchStockLevel(sku);
var isAvailable    = stockValidator.ValidateQuantity(stock.Available, qty);
Console.WriteLine($"  Inventory:  {stock.Available} units available · requested {qty} → {(isAvailable ? "AVAILABLE" : "INSUFFICIENT")}");

if (!isAvailable)
{
    Console.WriteLine("\n  Status: REJECTED — insufficient stock");
    return;
}

var blocklist    = new BlocklistLeaf();
var blockResult  = await blocklist.CheckBlocklist(token);
Console.WriteLine($"  Blocklist:  {token} → {blockResult.Reason}");

var fraud       = new FraudAction();
var fraudResult = await fraud.EvaluateRisk(token, product.UnitPrice * qty);
Console.WriteLine($"  Fraud:      score {fraudResult.Score:F2} → {(fraudResult.IsRisky ? "DECLINED" : "APPROVED")}");

if (fraudResult.IsRisky || blockResult.Blocked)
{
    Console.WriteLine("\n  Status: REJECTED — fraud risk");
    ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
    return;
}

// ── Phase 2: Payment ────────────────────────────────────────────────────────
Console.WriteLine("\n── Phase 2: Payment ────────────────────────────────────────────");

var payment = new PaymentAction();
var charge  = await payment.ProcessPayment(
    new PaymentRequest(orderId, product.UnitPrice * qty, region, token));

Console.WriteLine($"  Tax:        ${charge.Tax} ({region} rate)");
Console.WriteLine($"  Total:      ${charge.Total}");
Console.WriteLine($"  Charge:     {charge.ChargeId} → {charge.Outcome}");

if (!charge.Success)
{
    Console.WriteLine("\n  Status: FAILED — payment declined");
    ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
    return;
}

// ── Phase 3: Fulfillment ────────────────────────────────────────────────────
Console.WriteLine("\n── Phase 3: Fulfillment ────────────────────────────────────────");

var orderValidator = new OrderValidatorTask();
orderValidator.ValidateOrderItems(sku, qty, product.UnitPrice);

var orderIdTask = new OrderIdTask();
var fullOrderId = orderIdTask.AssignOrderId(orderId);

var orderPersist = new OrderPersistLeaf();
var order        = await orderPersist.PersistOrder(fullOrderId, charge.Total);
Console.WriteLine($"  Order:      {order.OrderId} → {order.Status}");

var stockReserve = new StockReserveLeaf();
var reservation  = await stockReserve.DecrementStock(sku, qty);
Console.WriteLine($"  Reserved:   {qty}x {sku} → {reservation.ReservationId}");

var carrier  = new CarrierAction();
var shipment = await carrier.AssignCarrier(orderId, region, charge.Total);
Console.WriteLine($"  Carrier:    {shipment.Carrier} · ${shipment.Cost} · ETA {shipment.EstimatedDays} days");
Console.WriteLine($"  Shipment:   {shipment.ShipmentId} → BOOKED");

// ── Phase 4: Notifications ──────────────────────────────────────────────────
Console.WriteLine("\n── Phase 4: Notifications ──────────────────────────────────────");

var emailTemplate = new EmailTemplateTask();
var rendered      = emailTemplate.RenderEmailTemplate("order_confirmation", fullOrderId);
Console.WriteLine($"  Template:   {rendered}");

var notif = new NotificationLeaf();
var email = await notif.SendReceiptEmail(fullOrderId, charge.Total);
Console.WriteLine($"  Email:      {email.MessageId} → SENT");

var sms    = new SmsLeaf();
var smsOut = await sms.SendOrderSMS(fullOrderId, phone);
Console.WriteLine($"  SMS:        {smsOut.SmsId} → SENT");

// ── Summary ─────────────────────────────────────────────────────────────────
Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine($"║  Status: FULFILLED ✓                                        ║");
Console.WriteLine($"║  Order:  {fullOrderId,-50}║");
Console.WriteLine($"║  Total:  ${charge.Total,-49}║");
Console.WriteLine($"║  Ship:   {shipment.ShipmentId,-50}║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

ManifestGenerator.Generate(Assembly.GetExecutingAssembly());
