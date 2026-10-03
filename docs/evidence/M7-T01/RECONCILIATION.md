# M7-T01 — End-to-end first playable reconciliation

**Status:** CLOSED — acceptance fully evidenced  
**Date:** 2026-10-04  
**Latest audited main:** `885b9ceb81e5face9849a9210c2d56b4d6285a02`  
**M7 functional source head:** `b2bc2ccebdb63d3a578f9db2d401a6b2ab931ed7`  
**PR #18 merge:** `f4e2030186bd8f809cdf78584934c0a7195f63ea`  
**PR #18 final head:** `7fa21cf87245360e85d73cc776b1b935512cb511`  
**PR #18 final Engine-free CI:** run #63, `37146129069` — success  
**Reconciliation regression SHA:** `9e5d1e2b0d631cdc69154c4ddc8dd38094f50511`  
**Reconciliation Engine-free CI:** run #67, `37152011204` — success

This record supersedes only the old prerequisite-status statement in the provisional M7 evidence. It does not rewrite the historical runtime results.

## Prerequisite reconciliation

The necessary first-playable behavior in the requested chain is now independently reconciled and closed:

- M2-T02 — calendar/schedule
- M3-T02 — arrears/purchase eligibility
- M6-T01 — transferable skills
- M6-T02 — courses/study

Historical unchecked tasks outside this reconciliation are not inferred complete merely to make the chain green.

## Production/UI authority

M7 Presentation consumes the frozen `IGameCommands` / `FirstPlayableFlow` seam. Bootstrap, Character Creation, Life, Work playback, Study and Day Summary render immutable snapshots/outcomes and dispatch commands; they do not own or mutate authoritative `GameState`, checkpoint bytes, salary, XP, skills or course progress.

Relevant implementation includes:

- `Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs`
- `StartupLifeBootstrapper.cs`
- `CharacterCreationViewController.cs`
- `LifeScreenViewController.cs`
- `WorkShiftPlaybackController.cs`
- `StudyActionController.cs`
- `DaySummaryModal.cs`
- generated FirstPlayable scene/content/localization assets created through Unity Editor tooling

## Runtime evidence reused from PR #18

Committed evidence under `docs/evidence/PROVISIONAL-FIRST-PLAYABLE/M7-RUNTIME-2026-10-04/` records:

- Unity 6000.3.25f1 import/compile PASS, zero Startup Life compiler warnings
- builders/reopened bindings PASS
- EditMode 8/8 PASS
- PlayMode 2/2 PASS
- actual 1080×1920 portrait journey through Character Creation → Developer → Work → evening Study → Day Summary
- Vietnamese localization/glyph assertions and simulated notch/safe-area capture
- normal Application save/reload PASS, schema 1, generation 17
- observed save state: cash 1,800,000 VND; career XP 40; course progress 600000 units; exact salary claim 5000000/11
- six actual Unity captures and a rendered work/evening frame recording

`CreateWorkStudyAndAdvanceDayThroughViews` verifies state transitions through actual views/buttons, authoritative work XP/time/playback, reward-independent acknowledgement, course purchase/study, next-day summary agreement, save creation and scene-reload equality for name/time/cash/XP/course progress.

## Evidence-reuse integrity

A commit comparison from M7 functional head `b2bc2cc...` through latest audited main `885b9ce...` contains no changes under `Assets/StartupLife/`, `Assets/Scenes/`, `Packages/`, `ProjectSettings/` or `tools/`. The two post-PR18 main commits add only `.agents/**`.

This reconciliation adds only deterministic SimulationChecks plus documentation/ledger changes. No M7 runtime-affecting source or serialized Unity asset is modified, so the committed PR #18 Unity evidence remains applicable.

## Save / schema impact

None. SaveVersion remains 1; the normal `GameSession → JsonSaveSerializer → AtomicFileSaveStore` path is retained. No migration or save DTO change is introduced.

## Boundary

M7-T01 closes the first-playable journey only. It does **not** claim M7-T02 pause/interruption, Android lifecycle/device backup recovery, iOS smoke, IL2CPP/device filesystem behavior, or physical-device QA.

**Downstream status:** M7-T02 READY.  
**M8 status:** M8 NOT READY — blocked by M7-T02.
