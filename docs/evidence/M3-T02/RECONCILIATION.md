# M3-T02 — Arrears and purchase eligibility reconciliation

**Status:** CLOSED — acceptance fully evidenced  
**Date:** 2026-10-04  
**Audited main:** `885b9ceb81e5face9849a9210c2d56b4d6285a02`  
**Regression source/test SHA:** `9e5d1e2b0d631cdc69154c4ddc8dd38094f50511`  
**Engine-free CI:** run #67, `37152011204` — success

## Implementation

Authoritative behavior is in `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs` with persisted arrears/ledger state in `Assets/StartupLife/Scripts/Core/GameState.cs` and structural nonnegative-cash validation in `Assets/StartupLife/Scripts/Core/StateValidation.cs`.

Obligations pay `min(cash, cost)`, create arrears for the remainder, and never drive cash negative. Income settles arrears in stable oldest-due order before adding spendable cash. Course purchase rejects while arrears remain or when cash is insufficient.

## Acceptance → evidence

| Criterion | Exact evidence |
|---|---|
| Zero-cash month | SimulationChecks: `oldest arrears settle before spending and employment recovers` plus the new repeated-shortfall regression starts from zero cash |
| Repeated shortfalls | SimulationChecks: `repeated zero-cash shortfalls accumulate arrears without negative cash`; 1M → 2M → 3M arrears while cash remains 0 |
| Income recovery / oldest arrears first | SimulationChecks: `oldest arrears settle before spending and employment recovers`; employment income clears arrears before cash becomes spendable |
| Insufficient course funds | SimulationChecks: `insufficient funds and stale revision reject without mutation`; rejection preserves checkpoint bytes |
| Interrupted/replayed transaction safety | Existing transactional/retry and H1 storage fault regressions; historical committed outcomes survive retry/restore without duplicate effects, H1/H2/R2 remains 19/19 |
| Cash never negative | New repeated-shortfall test asserts zero cash through repeated obligations; production uses bounded obligation payment and StateValidation rejects negative cash |
| Purchase blocked during arrears | `oldest arrears settle before spending and employment recovers` verifies `economy.arrears` and unchanged checkpoint before recovery |

## Save / migration impact

No schema change. `ArrearState`, ledger and cash are already persisted in SaveVersion 1. The reconciliation adds one deterministic test only; serializer, migration and storage behavior are unchanged.

## Runtime boundary

Engine-free evidence is sufficient for the financial rule itself. Mobile filesystem durability remains a separate lifecycle/platform concern and is not claimed here.

## Regression

CI #67: SimulationChecks 49/49; H1/H2/R2 19/19; M2 restore/provenance 35/35; all relevant .NET builds 0 warnings / 0 errors.
