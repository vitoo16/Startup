# M7-T02 — First playable persistence and device gate

## Current local execution addendum — 2026-10-04

The [runtime acceptance report](RUNTIME-2026-10-04/REPORT.md) supersedes the historical
NOT RUN entries below for Windows validation, Unity import/tests, portrait captures,
and the dedicated Android emulator. It records two minimal verified fixes and actual
checksummed lifecycle checkpoints. Physical Android acceptance and current iOS smoke
remain NOT RUN. M7-T02 remains unchecked; M8 remains NOT READY.

The original GitHub implementation session below is retained as historical evidence.

## Session identity

- Owner model: GPT-5.6 Sol
- Starting main: `10510f0096c013fbc606f51afa4b2939eff094b9`
- Branch: `feat/m7-t02-lifecycle-device-gate`
- PR: #21
- Unity pin: `6000.3.25f1`
- Scope: M7-T02 only. M8 is untouched and remains blocked.
- SaveVersion: 1

## Skills used

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- pinned `unity-game-director`
- pinned `unity-gameplay-systems`
- pinned `unity-mcp-bridge`
- pinned `unity-qa-release`
- pinned `unity-debug-profiler`
- official `ui`
- official `ui-ugui`
- pinned `unity-ui-designer`
- `vietnam-art-direction`
- official `unity-cli`
- pinned `unity-localization`

Community skills were read from the repository-locked revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`. Official Unity skill provenance remains the
existing lock record in `docs/tooling/skills.lock.json`.

## Preflight LOW fix

The documentation validator's broken accessibility link was reproduced from source:
`.agents/skills/accessibility/SKILL.md` referenced missing
`../web-quality-audit/SKILL.md`.

Only that broken reference was removed. Validator behavior and scan scope were not weakened.

Commit:

`docs: fix broken accessibility skill reference`

## Lifecycle architecture

`StartupLifeBootstrapper` remains the Unity composition root and owns the Unity callbacks.

A small Presentation-only `FirstPlayableLifecycle` receives:

- the canonical `FirstPlayableFlow`;
- `WorkShiftPlaybackController`;
- the existing Presentation refresh callback.

It does not receive, inspect, serialize, or mutate `GameState`, `ISaveStore`,
`JsonSaveSerializer`, or `AtomicFileSaveStore`.

Callbacks used:

- `OnApplicationPause(bool)`;
- `OnApplicationQuit()`.

`OnApplicationFocus` is deliberately not used because mobile focus churn can overlap pause
transitions and would create duplicate lifecycle operations.

## Checkpoint semantics

There is no second lifecycle save format and no second save manager.

Authoritative gameplay already autosaves transactionally on every committed command through:

`FirstPlayableFlow → IGameCommands/GameSession → JsonSaveSerializer → AtomicFileSaveStore`

Therefore the lifecycle checkpoint is the last durably committed GameSession revision. Pausing does
not manufacture a new gameplay revision merely to say "paused".

When a work boundary has committed its reward but its presentation cue is not acknowledged:

- `CurrentActivityId` is already persisted;
- authoritative XP/skills/salary/history are already persisted;
- `PlaybackCursor == 0` remains persisted;
- pause stops the Presentation coroutine without acknowledging the unseen cue;
- restore may replay that cue visually;
- the replay path issues only `AcknowledgePlayback`;
- the replay path never calls `AdvanceBoundary`.

An already acknowledged cue has a non-zero playback cursor and is not replayed on boot.

## Course, day, recovery, and wall-clock coverage added

PlayMode source coverage now exercises:

- no-save fresh launch;
- pause during a committed work cue before acknowledgement;
- restart after an acknowledged work cue;
- active WorkShift coroutine interruption and UI recovery;
- exact mid-course instance/progress/cash restore and eventual one-time completion;
- next-day state restore without recommitting the day boundary;
- corrupt primary with valid backup, followed by the next normal write;
- unreadable primary fail-closed behavior even with a valid backup;
- both primary and backup invalid producing fatal bootstrap rather than a new run;
- real elapsed pause time leaving simulation instant/revision/economy unchanged.

The tests consume `FirstPlayableState` / frozen public outcome contracts. They do not inspect raw
mutable `GameState` from Presentation.

## Save/schema decision

No missing lifecycle persistence field was found.

Existing SaveVersion 1 already persists the required authoritative state, including:

- current activity;
- current cue;
- playback cursor;
- active course instance/progress;
- simulation instant;
- economy/career/skill/history/RNG/scheduler state.

Result:

- SaveVersion: **1**
- migration change: **no**
- persisted field change: **no**

## Evidence boundary for this session

The GitHub connector can edit/read the repository and observe automatic PR CI. It does not expose
the user's Windows machine, a licensed live Unity Editor, an attached Android device, a Mac/iOS
host, or manual `workflow_dispatch` for the provisioned self-hosted runtime workflow.

Accordingly, on this M7-T02 head:

- changed-head Unity import/compile: **NOT RUN**
- changed-head EditMode: **NOT RUN**
- changed-head PlayMode: **NOT RUN**
- Android fresh launch: **NOT RUN**
- Android pause during work: **NOT RUN**
- Android mid-course restart: **NOT RUN**
- Android next-day continuation: **NOT RUN**
- Android backup recovery: **NOT RUN**
- iOS smoke: **NOT RUN — external Mac/iOS route unavailable**
- Windows-local documentation validator: **NOT RUN**
- portrait before/after restore captures: **NOT RUN**
- full-loop recording: **NOT RUN**

The prior M7-T01 Windows/Unity 6000.3.25f1 evidence remains a regression baseline only. It is not
relabelled as M7-T02 lifecycle/device evidence because this change modifies Presentation lifecycle
code and tests.

Automatic Engine-free CI remains valid for source/documentation/.NET/static gates only.

## Acceptance status

M7-T02 remains unchecked.

Current classification:

`M7-T02 NOT CLOSED — changed-head Unity runtime, Android device gate, visual evidence, Windows validator, and iOS smoke remain unexecuted`

Downstream:

`M8 NOT READY — blocked by M7-T02`

Do not merge the PR and do not begin M8.

## Session closeout

Owner model: GPT-5.6 Sol

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release;
unity-debug-profiler; ui; ui-ugui; unity-ui-designer; vietnam-art-direction; unity-cli;
unity-localization.

Acceptance criteria: implementation and deterministic/static coverage are prepared; platform/runtime
acceptance remains explicit and unwaived.

Visual evidence: not claimed.

Save impact: lifecycle integration only; SaveVersion 1 unchanged; no migration or persisted-field
change.

Known limitations: no direct Windows/Unity/Android/Mac/iOS execution surface in this connector
session.

Files changed by the implementation slice: Presentation lifecycle/bootstrap/work playback, lifecycle
PlayMode tests, M7 static source gate, this evidence record, and the isolated accessibility link fix.
