# M6-T02 — Courses and study-time consumption reconciliation

**Status:** CLOSED — acceptance fully evidenced  
**Date:** 2026-10-04  
**Audited main:** `885b9ceb81e5face9849a9210c2d56b4d6285a02`  
**Regression source/test SHA:** `9e5d1e2b0d631cdc69154c4ddc8dd38094f50511`  
**Engine-free CI:** run #67, `37152011204` — success

## Implementation

Course purchase/study/completion is authoritative in `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs`. Persisted active/completed course state is in `Assets/StartupLife/Scripts/Core/GameState.cs`; course validation is in `Assets/StartupLife/Scripts/Core/Definitions.cs`.

Purchase validates one active course, content, prerequisite level, target-not-already-met, arrears and cash before charging. Study consumes only available free minutes and only the minutes needed to reach the target; progress is fixed-point `minutes * LearningSpeed`. External attainment of the target completes the active course without refund or a second grant.

## Acceptance → evidence

| Criterion | Exact evidence |
|---|---|
| One-time charge / retry | SimulationChecks: `course charge once, study time and completion floor`; same command retry is AlreadyCommitted and cash is charged once |
| Study consumes eligible time only | Same test plus `LearningSpeed changes duration and study never overlaps work/sleep`; work/sleep blocks reject without mutation |
| LearningSpeed affects future duration | `LearningSpeed changes duration and study never overlaps work/sleep`; doubled speed reduces required study minutes while respecting schedule boundaries |
| Partial-course save/restore with real progress | `Vietnamese name and course save round trip` restores exactly 600000 progress units; `mid-cycle/course restore and next outcomes deterministic` proves continuation equivalence |
| Target reached elsewhere | `course target met by career completes without refund/second grant`; cash stays charged, course completes once, no course grant is added |
| Insufficient funds preserve state | `insufficient funds and stale revision reject without mutation`; checkpoint bytes remain identical |
| Prerequisite gate | New SimulationChecks: `course prerequisites and single-active-course gate reject without mutation`; prerequisite rejection is byte-identical |
| Only one active course | Same new regression; second course rejects `course.already_active` with byte-identical checkpoint |
| Course read model / cold restore | `active course snapshot survives cold restore without checkpoint inspection` and PresentationChecks historical course retry |
| Completion semantics | Typed course-change regressions distinguish progress, study completion and skill-target-already-met completion |

## Save / migration impact

SaveVersion remains 1. `CourseState` progress/completion and completed-course history are already persisted. No DTO, serializer, migration, pricing or gameplay rule changes were made in this reconciliation.

## Regression

CI #67 passed SimulationChecks 49/49, PresentationChecks 11/11, M2 restore/provenance 35/35 and H1/H2/R2 19/19 with zero build warnings/errors.
