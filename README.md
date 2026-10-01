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
- **T8 Leaf** = the function name itself

---

## Structure

```
OpenSpatial/
  Spatial/
    SpatialAttribute.cs     ← [Spatial] schema definition
  Services/
    PaymentAction.cs        ← T6 Action: orchestrates the flow
    TaxTask.cs              ← T7 Task:   pure calculation, no I/O
    StripeLeaf.cs           ← T8 Leaf:   external call + 5s hard timeout
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

# 3. Run the demo
dotnet run

# 4. Run tests
dotnet test
```

**Demo output:**
```
=== OpenSpatial — 3-Tier Payment Flow ===

Input:  Order ORD-001 · $100.00 · Region CA
─────────────────────────────────────────
  [T6 Action] platform.billing.checkout.payment:order_flow.process_payment.execute.ProcessPayment
  [T7 Task]   platform.billing.checkout.payment:order_flow.process_payment.calculate_tax.CalculateTax
  [T8 Leaf]   platform.billing.checkout.payment:order_flow.process_payment.charge_vendor.ChargeStripe
  [T8 Leaf]   Stripe charged $108.75 → ch_e4b6c1cf
─────────────────────────────────────────
Tax:    $8.75
Total:  $108.75
Charge: ch_e4b6c1cf
Status: SETTLED

=== Spatial Manifest (.spatial/manifest.json) ===
✓ 3 nodes written to .spatial/manifest.json
```

**Try these variations:**
```bash
# Change region → different tax rate (T7 Task)
# Edit Program.cs: Region: "NY"  → $8.00 tax
#                  Region: "TX"  → $5.00 tax

# Trigger the timeout sink (T8 Leaf)
# Edit StripeLeaf.cs: Task.Delay(6000) → Status: TIMEOUT
```

---

## Tier Rules

| Tier | Role | Rule |
|------|------|------|
| T6 Action | Orchestrates | Calls T7 and T8, owns the outcome |
| T7 Task | Pure logic | No network or database calls |
| T8 Leaf | External I/O | Hard timeout required, explicit terminal outcomes only |

A T8 Leaf must always resolve to one of: `SETTLED`, `DECLINED`, or `TIMEOUT`. No silent hangs.

---

## Capability Tags

| Tag | Meaning |
|-----|---------|
| `DATA_ACCESS` | Read-only, no state mutation |
| `STATE_MUTATE` | Writes data, requires timeout + audit trail |
| `CRITICAL_DESTROY` | High blast radius, requires multi-sig approval |

---

## Roadmap

### Month 1 — The Shared Contract ✅
> One decorator. One coordinate. One source of truth.

- [x] `[Spatial]` attribute with full 8-tier schema
- [x] T6 Action → T7 Task → T8 Leaf call chain
- [x] Hard timeout on T8 Leaf (no silent hangs)
- [x] Capability tags: `DATA_ACCESS`, `STATE_MUTATE`, `CRITICAL_DESTROY`
- [x] Coordinate printed at runtime for every tier

---

### Month 2 — Manifest Generator
> Scan the assembly. Write the map. No manual documentation.

- [ ] Reflect over all methods tagged `[Spatial]` at build time
- [ ] Write `.spatial/manifest.json` with coordinate, capability, tier, and file location
- [ ] Add `.gitignore` for `bin/` and `obj/`, commit manifest to source control
- [ ] Validate: no two methods share the same coordinate

---

### Month 3 — OpenTelemetry Binding
> The coordinate becomes the OTel span name. Zero config.

- [ ] Add `OpenTelemetry` NuGet package
- [ ] `SpatialTracer.RunAsync()` wraps every `[Spatial]` method automatically
- [ ] Span name = full 8-tier coordinate string
- [ ] Span attributes: `spatial.capability`, `spatial.context`, `spatial.container`
- [ ] Span outcome tag: `OUTPUT` | `SINK:TIMEOUT` | `SINK:ERROR`
- [ ] Export to console exporter first, then OTLP

---

### Month 4 — Event Accounting (H.A.C.K. Baseline)
> Count what enters. Count what exits. Investigate the difference.

- [ ] Wire `I_k` (inputs), `O_k` (outputs), `S_k` (sinks), `B_k` (in-flight) OTel metrics
- [ ] Auto-derive from `EventIn`, `EventOut`, `EventSink` fields on `[Spatial]`
- [ ] Compute `U_k = I_k - (O_k + S_k + B_k)` as a gauge metric
- [ ] Alert when `U_k > 0` at any coordinate
- [ ] Grafana dashboard template: Health Funnel per coordinate

---

### Month 5 — `sptree` CLI
> Print C4 topology in the terminal. No diagram tool required.

- [ ] CLI that reads `.spatial/manifest.json`
- [ ] `sptree` → prints full system tree
- [ ] `sptree platform.billing` → prints subtree from that coordinate
- [ ] `sptree --capability STATE_MUTATE` → lists all state-mutating nodes
- [ ] GitHub Action: auto-generate Mermaid C4 diagram on every PR

---

### Month 6 — Spatial-MCP Server
> AI agents query by coordinate. No file traversal. No hallucination.

- [ ] MCP server exposing structured tools:
  - `get_node(coordinate)` → returns tier, capability, contract
  - `get_subnet(prefix)` → returns all nodes under a coordinate prefix
  - `get_callers(coordinate)` → returns all nodes that call this one
  - `evaluate_delta(diff)` → detects new cross-domain edges
- [ ] Plugs into Cursor, Windsurf, Claude Code via MCP protocol
- [ ] Agent sees the same structural map as the human — Architectural Asymmetry eliminated

---

## Author

**Jatinderpal Singh** — [Agentic-First Software Design](https://erjsingh2012.github.io/Agentic-First-Design/)
