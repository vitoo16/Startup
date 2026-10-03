# M2-T02 — Calendar and schedule planner reconciliation

**Status:** CLOSED — acceptance fully evidenced  
**Date:** 2026-10-04  
**Audited main:** `885b9ceb81e5face9849a9210c2d56b4d6285a02`  
**Regression source/test SHA:** `9e5d1e2b0d631cdc69154c4ddc8dd38094f50511`  
**Engine-free CI:** run #67, `37152011204` — success

## Implementation

Authoritative calendar/schedule behavior is implemented in:

- `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs`
- `Assets/StartupLife/Scripts/Core/GameState.cs`
- `Assets/StartupLife/Scripts/Core/Definitions.cs`

The locked contract in `docs/architecture/FIRST_PLAYABLE_CONTRACTS.md` and ADR-004 defines unemployed/founder days as having no employment reservation. The current first-playable scheduler represents that rule generically: `WorkingCareer` returns no reservation whenever there is no active eligible employment. Future business/founder business allocation belongs to the separate business planner contract and is not silently substituted for this calendar acceptance criterion.

## Acceptance → evidence

| Criterion | Exact evidence |
|---|---|
| Leap years, month ends, office weekends, hospitality/F&B weekend shifts | SimulationChecks: `leap years, month end and hospitality weekends` |
| Unemployment and founder-day availability | SimulationChecks: `employment-free planner covers founder-equivalent availability and clamps allocated minutes`; verifies a no-employment day exposes the full wake→sleep window and that resignation releases the remaining employment reservation |
| Allocated minutes never exceed available time | Same regression requests 5,000 study minutes with only 840 available and proves exactly 840 are consumed, ending at sleep with zero free minutes |
| Suspension / zero offline advancement | SimulationChecks: `suspension/real elapsed time never advances simulation`; checkpoint bytes remain identical across restore and repeated snapshots |
| Activity boundaries / deterministic advancement | Existing boundary, scheduler, retry, restore and M2 provenance suites; RestoreInvariantChecks remains 35/35 |
| Birth date behavior required by the current model | Character creation persists `BirthDateIso`; restore provenance binds it to the committed creation command. The current first-playable contract defines no separate birthday reward/event side effect, so none is invented for closure. |

## Save / migration impact

No schema change in this reconciliation. Calendar/activity fields are already part of SaveVersion 1 and the existing restore/migration framework. The added test changes no persisted DTO, serializer, migration, or production rule.

## Runtime boundary

This task is deterministic simulation/calendar work; no visual or device claim is required. CI #67 passed SimulationChecks 49/49, H1/H2/R2 19/19 and M2 restore/provenance 35/35 with clean builds.

## Skills / review boundary

The reconciliation used the repository session orchestrator and gameplay guardian plus the locked calendar/gameplay contracts. It adds verification only; no gameplay implementation or architecture change is attributed to this pass.
