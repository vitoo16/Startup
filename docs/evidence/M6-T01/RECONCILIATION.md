# M6-T01 — Six transferable skills reconciliation

**Status:** CLOSED — acceptance fully evidenced  
**Date:** 2026-10-04  
**Audited main:** `885b9ceb81e5face9849a9210c2d56b4d6285a02`  
**Regression source/test SHA:** `9e5d1e2b0d631cdc69154c4ddc8dd38094f50511`  
**Engine-free CI:** run #67, `37152011204` — success

## Production skill catalog

`Assets/StartupLife/Scripts/Content/FirstPlayableContentTemplate.cs` contains exactly the approved six transferable skills:

1. `communication`
2. `negotiation`
3. `time-management`
4. `problem-solving`
5. `networking`
6. `leadership`

No Creativity skill is present in the MVP catalog.

## Implementation

- `SkillDefinition` requires exactly five strictly increasing exposure thresholds, yielding effective levels 0–5.
- `SkillState` stores exposure separately from `GrantedLevel`.
- `GameSnapshot.Level` is `max(granted floor, exposure-earned level)`.
- Work scenes add career XP to employment and skill exposure to the selected skill as separate state.
- `SimulationEngine.Grant` is idempotent by stable grant ID and never lowers an existing granted floor.

## Acceptance → evidence

| Criterion | Exact evidence |
|---|---|
| Exact six production skills | FirstPlayableContentTemplate exact ID list; ContentCatalogChecks also verifies six skills in the built catalog |
| Level 0–5 boundaries | SimulationChecks: `skill levels cover 0-5 boundaries and weaker grants cannot lower earned strength`; covers all threshold edges 0 through 5 |
| Stronger skill remains stronger | Same regression proves a weaker granted floor cannot lower an exposure-earned level and a stronger floor remains effective |
| Duplicate milestone grants are idempotent | SimulationChecks: `career XP remains separate from skills; promotion milestone idempotent`; later advancement does not increase grant count |
| Scene exposure progresses skill | SimulationChecks: `career boundary outcome exposes activity interval progression and reached instant`; committed career scene reports communication exposure delta independently of career XP |
| Career progression remains separate from Skill | `career XP remains separate from skills; promotion milestone idempotent` and production state split between EmploymentState.Xp/Rank and SkillState.Exposure/GrantedLevel |

## Save / migration impact

No schema change. Skill exposure, granted floors and stable grant IDs already persist in SaveVersion 1. No migration is introduced by this reconciliation.

## Regression

CI #67 passed SimulationChecks 49/49, ContentCatalogChecks 8/8, frozen M1 API 31/31 and the existing restore/provenance suites with clean builds.
