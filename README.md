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

## Run

```bash
dotnet run
```

Output:
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

- [ ] Manifest generator — scan assembly, write `.spatial/manifest.json`
- [ ] OpenTelemetry binding — coordinate becomes the OTel span name automatically
- [ ] `sptree` CLI — print C4 topology from manifest
- [ ] Spatial-MCP server — AI agents query by coordinate, not keyword search

---

## Author

**Jatinderpal Singh** — [Agentic-First Software Design](https://erjsingh2012.github.io/Agentic-First-Design/)
