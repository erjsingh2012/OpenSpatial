# OpenSpatial

Two decorators. Every public boundary method carries both.

```csharp
[Spatial(Ecosystem="platform", Context="billing", Container="checkout",
         Component="payment", Workflow="order_flow", Action="process_payment",
         Task="charge_vendor", Capability="STATE_MUTATE", TimeoutMs=5000)]
[Monitoring(
    Path      = 0xFF73,   // all paths · output@10% · sink@25% · E/U@100%
    EventIn   = "payment.requested",
    EventOut  = "payment.settled  | P1:SETTLED_FULL, P2:SETTLED_PARTIAL, P3:SETTLED_DEFERRED",
    EventSink = "payment.failed   | S1:DECLINED, S2:TIMEOUT, S3:INSUFFICIENT_FUNDS"
)]
public async Task<ChargeResult> ChargeStripe(decimal total, string token) { }
```

Both are **AI-generated once** during code review. Developers approve, never write them by hand.

---

## The Problem — Architectural Asymmetry

Four actors hold four different, out-of-sync mental models of the same system:

| Actor | How they see the code |
|---|---|
| Human developer | file tree + tribal knowledge |
| AI agent | token context window |
| Compiler | type graph |
| Runtime / OTel | span names + metric labels |

**OpenSpatial gives all four the same map** — an 8-tier coordinate embedded directly in the code.

---

## Two Decorators — Two Jobs

### `[Spatial]` — Design-time · For Humans + AI Agents

Answers *where is this code and what is it?*

- Humans: C4 diagrams, code review, architecture decisions
- AI agents: deterministic code navigation, correct code generation
- Output: `manifest.json`, `sptree` CLI, Mermaid export, Spatial-MCP server

### `[Monitoring]` — Runtime · For Production + Dashboards

Answers *is this code healthy right now?*

- On-call engineers: U_k alert → exact coordinate → exact flow view
- SRE: auto-generated Grafana dashboard per coordinate
- Output: OTel counters (I_k, O_k, S_k), U_k gauge, named path breakdown

**The shared key:** the `[Spatial]` coordinate labels every `[Monitoring]` metric — a Grafana spike links back to the exact C4 node.

---

## What Gets Tracked

Only **public boundary methods**. Private helpers and utilities are invisible to the system.

```csharp
// ✅ TRACKED — public, crosses a system boundary
[Spatial(...)][Monitoring(...)]
public async Task<ChargeResult> ChargeStripe(decimal total, string token) { }

// ✅ TRACKED — public T7, meaningful testable unit of work
[Spatial(...)]
public decimal CalculateTax(decimal amount, string region) { }

// ❌ NOT TRACKED — private, trivial, no boundary
private decimal _applyRate(decimal amount, decimal rate) => amount * rate;

// ❌ NOT TRACKED — internal utility
private string _buildChargeId() => $"ch_{Guid.NewGuid().ToString()[..8]}";
```

**Rule:** public + crosses a domain / service / I/O boundary = track. Everything else = skip.

---

## The `[Spatial]` Coordinate — 8 Tiers

```
platform.billing.checkout.payment : order_flow.process_payment.charge_vendor.ChargeStripe
│        │       │         │         │           │               │             │
T1       T2      T3        T4        T5          T6              T7            T8
Ecosystem Context Container Component Workflow   Action          Task          Leaf
```

- **T1–T4 Macro** — where the code lives in the system (filesystem-independent)
- **T5–T8 Micro** — what the code does and at what tier
- **T8 Leaf** — the function name itself

Moving a file does not change the coordinate. The architecture lives in the decorator, not the filesystem.

---

## The `[Monitoring]` Schema — Event Accounting + Runtime Config

### Counters

| Counter | Increments | Meaning |
|---|---|---|
| `I_k` | on every call | event entered this coordinate |
| `O_k` | on OUTPUT | event exited successfully |
| `S_k` | on SINK:* | event terminated intentionally |
| `U_k` | derived | `I_k − (O_k + S_k)` |

`U_k > 0` = events entered and never exited through any known path. **This is the investigation signal.**

### `Path` — 16-bit monitoring config (AI default, runtime overridable)

```
High byte — enable mask      Low byte — sample rates
[P1|P2|P3|S1|S2|S3|E_|Uk]   [Out:3bits | Sink:3bits | E-flag | U-flag]
```

**Sample rate codes (3 bits):** `000`=0% · `001`=2% · `010`=5% · `011`=10% · `100`=25% · `101`=50% · `110`=100%

| Hex | Enabled | Output | Sink | Use |
|---|---|---|---|---|
| `0xFF73` | all 8 | 10% | 25% | payment — default production |
| `0xFF4F` | all 8 | 5%  | 10% | fraud — high-volume, reduce noise |
| `0xFF7B` | all 8 | 10% | 100% | incident — see every sink event |
| `0xFF D7` | all 8 | 100% | 50% | full incident mode |
| `0x0000` | all off | — | — | monitoring disabled |

**Runtime override** — three layers, resolved in order:

```csharp
// 1. In-process (feature flag, admin endpoint, incident responder)
MonitoringRuntime.SetPath(coordinate, 0xFF7B);   // crank sink to 100% during incident
MonitoringRuntime.ClearPath(coordinate);          // restore to decorator default

// 2. Environment variable (deployment config, per-pod override)
// SPATIAL_PATH_PLATFORM_BILLING_CHECKOUT_PAYMENT_ORDER_FLOW_PROCESS_PAYMENT_CHARGE_VENDOR=0xFF7B

// 3. Decorator default — AI-generated at code review time, never hand-written
[Monitoring(Path=0xFF73, ...)]
```

`MonitoringRuntime.Inspect(coordinate, defaultPath)` prints source + full decoded breakdown.

### Named Paths

`O_k` and `S_k` break into named sub-paths — each gets its own OTel counter:

```
EventOut  = "payment.settled | P1:SETTLED_FULL, P2:SETTLED_PARTIAL, P3:SETTLED_DEFERRED"
EventSink = "payment.failed  | S1:DECLINED, S2:TIMEOUT, S3:INSUFFICIENT_FUNDS"
```

`U_k > 0` shows *something is wrong*. Named paths show *which exit is failing* — S2 (TIMEOUT) rising while S3 (INSUFFICIENT_FUNDS) = 0 points to Stripe latency, not a code bug.

### Flow Logic View

Each method's manifest node includes a structured flow — steps and all possible exits:

```json
"Flow": {
  "steps": [
    { "seq": 1, "op": "guard",   "desc": "CancellationTokenSource(5000ms)" },
    { "seq": 2, "op": "io",      "desc": "Stripe API call — external network" },
    { "seq": 3, "op": "compute", "desc": "generate charge ID" }
  ],
  "exits": [
    { "outcome": "OUTPUT",       "condition": "charge.Success == true",  "counter": "O_k → P1" },
    { "outcome": "SINK:TIMEOUT", "condition": "cts.Token cancelled",     "counter": "S_k → S2" },
    { "outcome": "SINK:ERROR",   "condition": "unhandled exception",     "counter": "S_k → S3" }
  ]
}
```

When `U_k > 0` fires, the flow view shows which exit has no counter mapped — that is where the leak is.

---

## Real-World Demo — E-Commerce Order Fulfillment

Seven microservices. Four execution phases. 22 `[Spatial]` nodes.

```
── Phase 1: Pre-flight ────────────────────────────────────────
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
  Template:   order_confirmation rendered
  Email:      msg_e50b2f0d → SENT
  SMS:        sms_df56f7a7 → SENT

Status: FULFILLED ✓
```

### Architecture — 7 Domains, 22 Nodes

```
platform.catalog        → search.indexer      → SearchIndex           [DATA_ACCESS]
                          search.ranker       → RankByRelevance        [DATA_ACCESS]

platform.inventory      → warehouse.stock     → FetchStockLevel        [DATA_ACCESS]
                                              → ValidateQuantity        [DATA_ACCESS]
                                              → DecrementStock          [STATE_MUTATE]

platform.fraud          → screening.scorer    → EvaluateRisk           [DATA_ACCESS]
                                              → FetchFraudScore         [DATA_ACCESS]
                          screening.blocklist → CheckBlocklist          [DATA_ACCESS]

platform.billing        → checkout.payment    → ProcessPayment         [STATE_MUTATE]
                                              → ChargeStripe            [STATE_MUTATE]
                                              → CalculateTax            [DATA_ACCESS]
                                              → VoidCharge              [CRITICAL_DESTROY]
                                              → ReverseCharge           [CRITICAL_DESTROY]

platform.orders         → management.writer   → AssignOrderId          [DATA_ACCESS]
                                              → PersistOrder            [STATE_MUTATE]
                          management.validator→ ValidateOrderItems      [DATA_ACCESS]

platform.fulfillment    → shipping.dispatcher → AssignCarrier          [STATE_MUTATE]
                                              → SelectShippingRate      [DATA_ACCESS]
                                              → BookShipment            [STATE_MUTATE]

platform.notifications  → email.sender        → SendReceiptEmail       [STATE_MUTATE]
                          email.templater     → RenderEmailTemplate     [DATA_ACCESS]
                          sms.sender          → SendOrderSMS            [STATE_MUTATE]
```

---

## Build, Run & Test

**Requirements:** .NET 8 SDK — [download](https://dotnet.microsoft.com/download)

```bash
# Clone and build
git clone https://github.com/erjsingh2012/OpenSpatial.git
cd OpenSpatial
dotnet build

# Run the full order fulfillment demo
dotnet run

# sptree — C4 architecture map
dotnet run -- tree --c1         # 7 system contexts at a glance
dotnet run -- tree --c2         # containers per context
dotnet run -- tree --c3         # components per container
dotnet run -- tree --c4         # full 22-node code map

# Filter views
dotnet run -- tree platform.billing          # one domain only
dotnet run -- tree --cap CRITICAL_DESTROY    # high blast-radius nodes
dotnet run -- tree --cap STATE_MUTATE        # all write operations

# Export to Markdown / Mermaid
dotnet run -- tree --md                      # Mermaid graph TD
dotnet run -- tree --md --c1                 # C1 as Mermaid
dotnet run -- tree --md > ARCHITECTURE.md   # save to file
```

---

## Tier Rules

| Tier | Role | Rule |
|---|---|---|
| T6 Action | Orchestrates | Calls T7 and T8, owns the outcome |
| T7 Task | Pure logic | No network, no database — pure function |
| T8 Leaf | External I/O | Hard timeout required, explicit terminal outcomes only |

**T8 Leaf must always resolve to one of:** `SETTLED`, `DECLINED`, `VOIDED`, or `TIMEOUT`. No silent hangs.

---

## Capability Tags

| Tag | Color | Meaning |
|---|---|---|
| `DATA_ACCESS` | 🔵 | Read-only, no state mutation |
| `STATE_MUTATE` | 🔴 | Writes data — requires timeout + audit trail |
| `CRITICAL_DESTROY` | 🟣 | Irreversible, high blast radius |

---

## OTel Binding

`[Spatial]` drives traces. `[Monitoring]` drives metrics and logs. `SpatialTracer` bridges both.

```
On entry  → I_k counter  + span.AddEvent(EventIn)
On OUTPUT → O_k counter  + span.AddEvent(EventOut) + latency histogram → P1/P2/P3
On SINK   → S_k counter  + span.AddEvent(EventSink)                   → S1/S2/S3
U_k gauge → I_k − (O_k + S_k)  →  alert when > 0
```

Span name = full 8-tier coordinate. Every metric carries the coordinate as a label.
Swap the console exporter for OTLP to send to Jaeger / Grafana Tempo with zero code change.

---

## Roadmap

### Phase 1 — Foundation ✅

| Month | Milestone | Status |
|---|---|---|
| 1 | `[Spatial]` attribute + 3-tier payment flow | ✅ |
| 2 | Manifest generator (`.spatial/manifest.json`) | ✅ |
| 3 | OpenTelemetry binding — coordinate = span name | ✅ |
| 4 | `sptree` CLI — C1/C2/C3/C4 + Mermaid `--md` | ✅ |
| 5 | Real-world demo — 7 domains, 22 nodes | ✅ |

### Phase 2 — Observability

| Month | Milestone | Status |
|---|---|---|
| 6 | `[Monitoring]` attribute — I_k/O_k/S_k/U_k + named paths + flow view | ⬜ |
| 7 | Grafana dashboard auto-generation from `[Monitoring]` schema | ⬜ |
| 8 | Spatial-MCP Server — AI agents query by coordinate | ⬜ |

### Phase 3 — Enforcement

| Month | Milestone | Status |
|---|---|---|
| 9 | `[SpatialModule]` — T1-T4 defined once at class level | ⬜ |
| 10 | Roslyn analyzer — duplicate coords, missing timeouts caught at build | ⬜ |
| 11 | GitHub Action — C4 Mermaid diff on every PR | ⬜ |
| 12 | AI code review agent — validates coordinates, flags missing exits | ⬜ |

---

## Author

**Jatinderpal Singh** — [Agentic-First Software Design](https://erjsingh2012.github.io/Agentic-First-Design/)
