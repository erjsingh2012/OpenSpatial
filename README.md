# OpenSpatial

A C# implementation of the `[Spatial]` decorator — the core mechanism of the **Agentic-First Software Design** framework.

The `[Spatial]` attribute gives every method an 8-tier architectural coordinate that humans, AI agents, compilers, and runtime engines all read identically. This eliminates **Architectural Asymmetry**: the problem where four actors hold four different, out-of-sync mental models of the same system.

---

## The Coordinate

```
platform.billing.checkout.payment : order_flow.process_payment.charge_vendor.ChargeStripe
│        │       │         │         │           │               │             │
T1       T2      T3        T4        T5          T6              T7            T8
Ecosystem Context Container Component Workflow   Action          Task          Leaf
```

- **T1–T4** (Macro): where the code lives in the system
- **T5–T8** (Micro): what the code does and what tier it operates at
- **T8 Leaf** = the function name itself — the coordinate is decoupled from the filesystem

---

## Real-World Demo: E-Commerce Order Fulfillment

Seven microservices. Four execution phases. One `[Spatial]` attribute per method.

```
── Phase 1: Pre-flight checks ─────────────────────────────────
  Catalog:    iPhone 15 Pro · $999.50/unit · IN_STOCK
  Inventory:  47 units available · requested 2 → AVAILABLE
  Blocklist:  tok_test_visa → CLEAR
  Fraud:      score 0.12 → APPROVED

── Phase 2: Payment ────────────────────────────────────────────
  Tax:        $174.91 (CA rate)
  Total:      $2173.91
  Charge:     ch_19e22c61 → SETTLED

── Phase 3: Fulfillment ────────────────────────────────────────
  Order:      ORD-2026-001-20261001 → CREATED
  Reserved:   2x SKU-A1234 → rsv_f4a616e4
  Carrier:    UPS Ground · $0.00 · ETA 3 days
  Shipment:   SHIP-CB842DE2 → BOOKED

── Phase 4: Notifications ──────────────────────────────────────
  Template:   [order_confirmation] Order ORD-2026-001 — rendered
  Email:      msg_e50b2f0d → SENT
  SMS:        sms_df56f7a7 → SENT

╔══════════════════════════════════════════════════════╗
║  Status: FULFILLED ✓                                 ║
╚══════════════════════════════════════════════════════╝
```

---

## Architecture — 7 Domains, 22 Nodes

```
platform.catalog        → search.indexer     → SearchIndex          [DATA_ACCESS]
                          search.ranker      → RankByRelevance       [DATA_ACCESS]

platform.inventory      → warehouse.stock    → FetchStockLevel       [DATA_ACCESS]
                                             → ValidateQuantity       [DATA_ACCESS]
                                             → DecrementStock         [STATE_MUTATE]

platform.fraud          → screening.scorer   → EvaluateRisk          [DATA_ACCESS]
                                             → FetchFraudScore        [DATA_ACCESS]
                          screening.blocklist→ CheckBlocklist         [DATA_ACCESS]

platform.billing        → checkout.payment   → ProcessPayment        [STATE_MUTATE]
                                             → ChargeStripe           [STATE_MUTATE]
                                             → CalculateTax           [DATA_ACCESS]
                                             → VoidCharge             [CRITICAL_DESTROY]
                                             → ReverseCharge          [CRITICAL_DESTROY]

platform.orders         → management.writer  → AssignOrderId         [DATA_ACCESS]
                                             → PersistOrder           [STATE_MUTATE]
                          management.validator→ ValidateOrderItems    [DATA_ACCESS]

platform.fulfillment    → shipping.dispatcher→ AssignCarrier         [STATE_MUTATE]
                                             → SelectShippingRate     [DATA_ACCESS]
                                             → BookShipment           [STATE_MUTATE]

platform.notifications  → email.sender       → SendReceiptEmail      [STATE_MUTATE]
                          email.templater    → RenderEmailTemplate    [DATA_ACCESS]
                          sms.sender         → SendOrderSMS           [STATE_MUTATE]
```

---

## Structure

```
OpenSpatial/
  Spatial/
    SpatialAttribute.cs     ← [Spatial] schema — the single declarative contract
    SpatialTracer.cs        ← OTel span wrapper — coordinate = span name
    ManifestGenerator.cs    ← reflects assembly → .spatial/manifest.json
    SpatialTree.cs          ← sptree CLI — C1/C2/C3/C4 + Mermaid output
  Services/
    SearchLeaf.cs           ← platform.catalog    · T8
    RankerTask.cs           ← platform.catalog    · T7
    StockLeaf.cs            ← platform.inventory  · T8
    StockValidationTask.cs  ← platform.inventory  · T7
    StockReserveLeaf.cs     ← platform.inventory  · T8
    BlocklistLeaf.cs        ← platform.fraud      · T8
    FraudAction.cs          ← platform.fraud      · T6
    FraudScoreLeaf.cs       ← platform.fraud      · T8
    PaymentAction.cs        ← platform.billing    · T6
    TaxTask.cs              ← platform.billing    · T7
    StripeLeaf.cs           ← platform.billing    · T8
    VoidChargeAction.cs     ← platform.billing    · T6 [CRITICAL_DESTROY]
    VoidChargeLeaf.cs       ← platform.billing    · T8 [CRITICAL_DESTROY]
    OrderValidatorTask.cs   ← platform.orders     · T7
    OrderIdTask.cs          ← platform.orders     · T7
    OrderPersistLeaf.cs     ← platform.orders     · T8
    CarrierAction.cs        ← platform.fulfillment· T6
    ShippingRateTask.cs     ← platform.fulfillment· T7
    ShipmentLeaf.cs         ← platform.fulfillment· T8
    EmailTemplateTask.cs    ← platform.notifications· T7
    NotificationLeaf.cs     ← platform.notifications· T8
    SmsLeaf.cs              ← platform.notifications· T8
  Program.cs
```

---

## Build, Run & Test

**Requirements:** .NET 8 SDK — [download](https://dotnet.microsoft.com/download)

```bash
# 1. Clone
git clone https://github.com/erjsingh2012/OpenSpatial.git
cd OpenSpatial

# 2. Build
dotnet build

# 3. Run the full order fulfillment demo
dotnet run

# 4. sptree — C4 architecture map
dotnet run -- tree              # full C4 code diagram (22 nodes)
dotnet run -- tree --c1         # 7 system contexts at a glance
dotnet run -- tree --c2         # containers per context
dotnet run -- tree --c3         # components per container
dotnet run -- tree --c4         # full code-level map

# 5. Filter views
dotnet run -- tree platform.billing             # billing domain only
dotnet run -- tree --cap CRITICAL_DESTROY       # all high-blast-radius nodes
dotnet run -- tree --cap STATE_MUTATE           # all write operations

# 6. Export to Markdown / Mermaid
dotnet run -- tree --md                         # Mermaid graph TD
dotnet run -- tree --md --c1                    # C1 as Mermaid
dotnet run -- tree --md > ARCHITECTURE.md       # save to file
```

---

## sptree Output

**C1 — System Context (`--c1`)**
```
[C1] platform.billing        (5 nodes)
[C1] platform.catalog        (2 nodes)
[C1] platform.fraud          (3 nodes)
[C1] platform.fulfillment    (3 nodes)
[C1] platform.inventory      (3 nodes)
[C1] platform.notifications  (3 nodes)
[C1] platform.orders         (3 nodes)

22 node(s) across 7 context(s)
```

**C4 — Full Code Map (`--c4`)**
```
[C1] platform.fraud
  └─ [C2] screening
       ├─ [C3] blocklist
      │    └── [C4] [DATA_ACCESS     ] detection_flow › blocklist › lookup › CheckBlocklist
       └─ [C3] scorer
           ├── [C4] [DATA_ACCESS     ] detection_flow › pre_auth › evaluate › EvaluateRisk
           └── [C4] [DATA_ACCESS     ] detection_flow › pre_auth › score_api › FetchFraudScore

[C1] platform.notifications
  ├─ [C2] email
  │    ├─ [C3] sender
  │   │    └── [C4] [STATE_MUTATE    ] order_flow › notify › send_api › SendReceiptEmail
  │    └─ [C3] templater
  │        └── [C4] [DATA_ACCESS     ] order_flow › notify › render › RenderEmailTemplate
  └─ [C2] sms
       └─ [C3] sender
           └── [C4] [STATE_MUTATE    ] order_flow › notify › send_sms › SendOrderSMS

22 node(s)   STATE_MUTATE: 8   DATA_ACCESS: 12   CRITICAL_DESTROY: 2
```

---

## Tier Rules

| Tier | Role | Rule |
|------|------|------|
| T6 Action | Orchestrates | Calls T7 and T8, owns the outcome |
| T7 Task | Pure logic | No network or database calls |
| T8 Leaf | External I/O | Hard timeout required, explicit terminal outcomes only |

A T8 Leaf must always resolve to one of: `SETTLED`, `DECLINED`, `VOIDED`, or `TIMEOUT`. No silent hangs.

---

## Capability Tags

| Tag | Color | Meaning |
|-----|-------|---------|
| `DATA_ACCESS` | 🔵 Blue | Read-only, no state mutation |
| `STATE_MUTATE` | 🔴 Red | Writes data, requires timeout + audit trail |
| `CRITICAL_DESTROY` | 🟣 Purple | High blast radius, irreversible operation |

---

## OTel Binding

Every `[Spatial]` method automatically becomes an OTel span. The coordinate is the span name:

```
Activity.DisplayName:
  platform.billing.checkout.payment:order_flow.process_payment.charge_vendor.ChargeStripe

Activity.Tags:
  spatial.coordinate  → full 8-tier coordinate
  spatial.capability  → STATE_MUTATE
  spatial.outcome     → OUTPUT | SINK:TIMEOUT | SINK:ERROR
  spatial.context     → billing
  spatial.container   → checkout
```

Swap the console exporter for OTLP to send spans to Jaeger / Grafana Tempo with zero code change.

---

## Roadmap

### Phase 1 — Foundation ✅

#### Month 1 — The Shared Contract ✅
- [x] `[Spatial]` attribute with full 8-tier schema
- [x] T6 Action → T7 Task → T8 Leaf call chain
- [x] Hard timeout on T8 Leaf (no silent hangs)
- [x] Capability tags: `DATA_ACCESS`, `STATE_MUTATE`, `CRITICAL_DESTROY`

#### Month 2 — Manifest Generator ✅
- [x] Reflect over assembly, write `.spatial/manifest.json`
- [x] Validate no duplicate coordinates
- [x] Auto-generated — no manual documentation

#### Month 3 — OpenTelemetry Binding ✅
- [x] `SpatialTracer.RunAsync()` — coordinate = OTel span name
- [x] Span tags: capability, context, container, outcome
- [x] Console exporter — swap for OTLP to reach Jaeger / Tempo

#### Month 4 — sptree CLI ✅
- [x] `dotnet run -- tree --c1/--c2/--c3/--c4` — zoom levels
- [x] Filter by coordinate prefix or capability tag
- [x] `--md` flag — Mermaid output for GitHub / README

#### Month 5 — Real-World Demo ✅
- [x] 7 microservice domains (catalog, inventory, fraud, billing, orders, fulfillment, notifications)
- [x] 22 `[Spatial]` nodes — 8 STATE_MUTATE, 12 DATA_ACCESS, 2 CRITICAL_DESTROY
- [x] Full order fulfillment flow: pre-flight → payment → fulfillment → notifications

---

### Phase 2 — Observability

#### Month 6 — Event Accounting (H.A.C.K. Baseline)
> Count what enters. Count what exits. Investigate the difference.

- [ ] Wire `I_k` (inputs), `O_k` (outputs), `S_k` (sinks), `B_k` (in-flight) as OTel metrics
- [ ] Auto-derive from `EventIn`, `EventOut`, `EventSink` fields on `[Spatial]`
- [ ] Compute `U_k = I_k - (O_k + S_k + B_k)` as a gauge metric
- [ ] Alert when `U_k > 0` at any coordinate — unaccounted events detected
- [ ] Grafana dashboard template: Health Funnel per coordinate

#### Month 7 — Spatial-MCP Server
> AI agents query by coordinate. No file traversal. No hallucination.

- [ ] MCP server exposing structured tools:
  - `get_node(coordinate)` → tier, capability, contract
  - `get_subnet(prefix)` → all nodes under a coordinate prefix
  - `get_callers(coordinate)` → call graph from manifest
  - `evaluate_delta(diff)` → detect new cross-domain edges
- [ ] Plugs into Cursor, Windsurf, Claude Code via MCP protocol
- [ ] Human and AI agent see the same structural map — Architectural Asymmetry eliminated

---

## Author

**Jatinderpal Singh** — [Agentic-First Software Design](https://erjsingh2012.github.io/Agentic-First-Design/)
