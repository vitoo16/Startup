# M7-T01 provisional presentation adapter

Date: 2026-10-03  
Owner model: GPT-5.6 Sol  
PR: #17 — `feat: add M7 first playable presentation adapter`

Status: **provisional engine-free sub-slice only**. M7-T01 remains incomplete until the Unity composition root, Character Creation UI, Life screen, button bindings, PlayMode flow, and visual/runtime evidence are implemented and verified in a live Unity Editor.

## Why this slice is next

The post-freeze implementation audit found that M1–M6 domain behavior already exists and is covered by engine-free tests, while the first genuinely missing first-playable layer is M7-T01 Presentation integration.

The audit specifically identified M7-T01 as the first safe next Sol slice because the frozen M1 public API already supplies the required `GameSnapshot`, `SimulationOutcome`, `ActiveCourseSnapshot`, and `CourseChange` contracts.

This PR implements only the part that can be proven without a live Unity Editor/MCP session.

## Skills used

Project skills:

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`

Pinned community skills reviewed from revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`:

- `unity-game-director`
- `unity-gameplay-systems`
- `unity-qa-release`

Unity/MCP/UI skills remain required for the later scene/prefab/view-binding continuation. They were not claimed as runtime-executed in this engine-free slice.

## Acceptance criteria for this sub-slice

- Presentation dispatches through frozen `IGameCommands` only.
- Presentation never reads or mutates `GameState`, save envelopes, receipts, or persistence internals.
- Every command envelope uses the latest public snapshot `RunId` and `Revision`.
- Command IDs are supplied by an injectable source so tests can be deterministic while runtime may use GUID retry tokens.
- Character Creation versus Life-screen routing is derived from the immutable snapshot.
- Course state is consumed through frozen `ActiveCourseSnapshot`.
- Playback acknowledgement binds the frozen `CurrentActivityId`.
- Day/month advancement remains Application-owned through `IGameCommands`.
- Presentation-projected history and skill collections remain immutable.
- The frozen M1 API is unchanged.
- SaveVersion remains 1.
- No scene/prefab/runtime evidence is claimed.

## Implementation

### FirstPlayableFlow

New file:

`Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs`

The engine-independent adapter provides typed Presentation actions:

- `CreateCharacter`
- `AcceptJob`
- `AdvanceBoundary`
- `PurchaseCourse`
- `Study`
- `Resign`
- `AcknowledgePlayback`
- `AdvanceDay`
- `AdvanceMonth`

Every action obtains a fresh `GameSnapshot`, constructs a `CommandEnvelope` using its current `RunId` and `Revision`, dispatches through `IGameCommands`, then republishes a detached `FirstPlayableState`.

The adapter does not reference Simulation, Infrastructure internals, mutable Core state, Unity views, scenes, or save files.

### Command ID source

`ICommandIdSource` is injected.

`GuidCommandIdSource` is the runtime-safe default implementation for unique local retry tokens.

Tests inject a deterministic sequential implementation.

Simulation randomness remains untouched; command IDs are presentation/application retry identifiers, not gameplay RNG.

### FirstPlayableState

The immutable read model publishes the first-playable fields needed by later view bindings:

- screen mode: Character Creation or Life;
- name;
- simulated instant and revision;
- cash and arrears;
- career ID/XP/rank;
- cue;
- current activity ID;
- playback cursor;
- active course;
- skill levels;
- history;
- convenience state for career/study/playback availability.

Collections are copied into read-only wrappers before publication.

## Engine-free verification

A dedicated `tools/PresentationChecks` harness compiles `FirstPlayableFlow.cs` as a plain netstandard2.1 source file, proving that the adapter itself has no Unity dependency.

The 7 focused cases verify:

1. initial snapshot selects Character Creation and publishes immutable collections;
2. character creation binds current RunId, revision, injected command ID, and exact command payload;
3. job/course/study/resign/boundary actions use the latest revision and correct frozen command type;
4. playback acknowledgement uses the current public activity ID;
5. day/month advancement uses `AdvanceBoundary` intent and republishes the reached state;
6. `ActiveCourseSnapshot` is preserved through the adapter;
7. an invalid empty command-ID source fails before command dispatch.

## Verified evidence

Functional head `600ca45a0af08f3241e084932ea31d73a0ee4415` passed Engine-free CI run
`37137937289` (#57):

- authoritative documentation: PASS;
- static Unity foundation: **31/31**;
- frozen M1 API guard: **31/31**;
- Unity runner contract static gate: **30/30**;
- repository security/release hygiene: **28/28**;
- SimulationChecks: **45/45**;
- H1/H2/R2: **19/19**;
- M2 restore/provenance: **35/35**;
- PresentationChecks: **7/7**;
- all .NET builds: **0 warnings / 0 errors**;
- clean-worktree verification: PASS;
- artifact `engine-free-foundation-reports`: ID `11279027230`;
- artifact SHA-256: `59ec8faaecdce1f8962117450834401dc751765eab2c8e83762cb3b3a560e603`.

Two earlier CI attempts failed only because two .NET projects in the new harness shared the same MSBuild intermediate directory. The final harness isolates project extension/intermediate paths through `Directory.Build.props`. No production-domain correction was needed.

## Visual evidence

None.

No scene, prefab, hierarchy, uGUI layout, TMP binding, animation, or screenshot was changed in this sub-slice.

## Save impact

None.

- SaveVersion remains 1.
- no DTO persistence shape changes;
- no migration;
- no new save field;
- no Application/Infrastructure persistence change.

## Known limitations / next continuation

M7-T01 is not complete.

Still required with live Unity tooling:

- composition root that creates/restores `GameSession`;
- Character Creation view/controller binding;
- Life screen HUD binding;
- work fast-forward/playback controller;
- evening study controls;
- day-summary modal;
- Vietnamese localization keys for the new UI;
- PlayMode first-playable journey;
- safe-area/portrait visual evidence;
- Unity Editor import/compile evidence.

Scene/prefab assets must be edited through Unity Editor/MCP or generated Editor scripts, not raw YAML.

Android/iOS/device gates remain separate.

## Files changed

Production:

- `Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs`
- `Assets/StartupLife/Scripts/Presentation/FirstPlayableFlow.cs.meta`

Verification:

- `tools/PresentationChecks/StartupLife.PresentationModel.csproj`
- `tools/PresentationChecks/PresentationChecks.csproj`
- `tools/PresentationChecks/Directory.Build.props`
- `tools/PresentationChecks/Program.cs`
- `scripts/Test-Simulation.ps1`

Documentation:

- this closeout.

## Task ledger boundary

No checkbox is changed.

The engine-free adapter is meaningful M7-T01 progress, but it is not a substitute for the required Unity PlayMode and visual acceptance evidence.
