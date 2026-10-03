# M1 outcome/playback contract — closure candidate

Date: 2026-10-03  
Owner model: GPT-5.6 Sol  
PR #12 merged the initial candidate; follow-up PR #13 — `fix: close M1 playback provenance and course read model`

Status: **engine-free follow-up closure candidate only**. Astra found one HIGH and one MEDIUM after PR #12; PR #13 addresses both. This document does not mark M1-T01, M1-T02, or any milestone complete. Independent Astra re-verification and the separately tracked Unity/runtime dependency evidence are still required.

## Scope

This slice implements the presentation-facing outcome/playback contract described by ADR-003 and the first-playable architecture:

- immutable committed outcomes for direct commands and batch boundaries;
- deterministic reconstruction of the same outcome for an old retry;
- public current-activity/playback state in snapshots;
- authoritative reached instant for multi-boundary advancement;
- playback acknowledgement separated from rewards;
- receipt cue provenance bound to production replay.

No gameplay rule, content definition, save field, SaveVersion, migration, scene, prefab, or UI is changed.

## Skills used

Project-specific skills:

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`

Pinned community Unity skills from `tea-x-random/unity-game-skills` revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`:

- `unity-game-director`
- `unity-gameplay-systems`
- `unity-game-economy`
- `unity-debug-profiler`
- `unity-qa-release`
- `unity-mcp-bridge`

The MCP skill contract was reviewed, but this slice changes only scene-independent C# and uses the engine-free CI path. No live Unity Editor/MCP action is claimed.

## Public contract

### SimulationOutcome

`CommandResult.Outcome` is non-null for a successfully `Committed` command and for an `AlreadyCommitted` retry.

The immutable outcome publishes Core-only values:

- operation and command identity;
- activity identity;
- prior/new revision;
- simulated start/end instants and minutes consumed;
- committed presentation cue and playback cursor;
- cash delta and immutable ledger entries;
- employment identity, career-XP delta and prior/new rank;
- immutable skill exposure/effective-level/granted-floor deltas;
- nullable typed `CourseChange` with identity, definition, skill, prior/new progress, target, change kind, and explicit completion reason;
- newly committed grant IDs;
- newly committed history entries.

All list inputs are copied into read-only arrays before publication.

### Snapshot and batch result

`GameSnapshot` now publishes:

- `CurrentActivityId`
- `PlaybackCursor`
- nullable immutable `ActiveCourse`, including instance/definition/skill IDs, current progress, target units, and target level

`AdvanceResult` now publishes:

- committed boundary results, each with an outcome;
- `ReachedInstant`;
- the existing stop reason.

Presentation therefore does not need mutable `GameState`, save-envelope internals, receipts, ledger traversal, or reward reconstruction to render a committed result.

## Commit publication

For a new command, Application captures a detached before-state baseline, evaluates an isolated candidate with the production `SimulationEngine`, validates it, and constructs the outcome from before/candidate differences.

The outcome is not returned to the caller until:

1. the candidate has passed state validation;
2. the save store reports a committed write;
3. storage readback confirms the committed generation and receipt;
4. the live state is swapped to the committed candidate.

Definitive write failure still publishes no outcome and leaves the live state unchanged. Ambiguous write still enters RecoveryRequired.

## Old retry and reload behavior

No outcome DTO is persisted.

For an `AlreadyCommitted` retry, Application reconstructs a detached state from the authoritative receipt origin and replays the retained receipt sequence through the production `SimulationEngine` until the requested receipt is reached.

The reconstruction verifies/reuses:

- canonical command payload;
- production operation allocation;
- production simulation rules;
- committed duration;
- committed receipt order/revision;
- existing restore/state invariants.

It performs no storage write and never mutates the live session.

This permits a retry after later commits or after a fresh restore to publish the same semantic outcome without a SaveVersion change.

## Cue and playback provenance

The restore verifier requires every committed receipt cue—not only `AdvanceBoundary` career cues—to equal the cue produced by deterministic production replay. PR #13 additionally requires checkpoint-level `CurrentCue` and `PlaybackCursor` to equal replay before a session can be published when required content is available.

Checksum-valid saves with a forged receipt cue, forged root cue, or rolled-back root playback cursor are classified as `Corrupt` before `TryRestore` can return a session. When required content is unavailable, compatibility classification retains precedence and returns `UnsupportedContent`.

Candidate serialization now occurs inside the controlled validation path, so serializer/invariant validation failure returns `state.invalid` instead of escaping from the command gate.

## Playback safety

`AcknowledgePlayback` continues to run through the normal transactional command gate.

Its committed outcome may publish the new playback cursor but has zero:

- cash delta;
- career-XP delta;
- ledger entries;
- skill deltas;
- grant IDs;
- history entries.

The activity ID and cue remain the already committed playback target. Animation/playback consumption therefore cannot grant or replay economy/progression rewards.

## Tests

Functional PR #13 head `f04b6241397b717403f9b0600c59834dbdf1064d` passed Engine-free CI run `37130997830` (#45).

Evidence:

- authoritative documentation validation: PASS;
- static Unity foundation: **31/31**;
- Unity runner contract static gate: **30/30**;
- repository security/release hygiene: **28/28**;
- SimulationChecks: **45/45**;
- H1/H2/R2 focused regressions: **19/19**;
- M2 restore/provenance regressions: **35/35**;
- all three .NET harness builds: **0 warnings / 0 errors**;
- clean-worktree verification: PASS;
- artifact `engine-free-foundation-reports`: ID `11277310271`;
- artifact SHA-256: `272444eefe0a69370ca305935f162902c80c6554ea0ac4e31e8f105e9dfe7ec9`.

New M1-focused checks cover:

1. immutable transaction outcome fields from course purchase;
2. retry after later commits returns the same semantic outcome;
3. retry from a fresh restored session returns the same semantic outcome;
4. career-scene activity interval, cue, XP and skill exposure deltas;
5. snapshot current activity/playback state;
6. batch reached instant and non-null boundary outcomes;
7. playback acknowledgement produces no reward/progression/history deltas;
8. checksum-valid forged non-boundary receipt cue is rejected as corrupt;
9. forged root cue and playback-cursor rollback are rejected before restore publication;
10. unsupported-content precedence is retained despite forged playback fields;
11. candidate serialization validation is contained as `state.invalid`;
12. active course identity/definition/progress/target survive cold restore through public snapshot only;
13. course purchase/progress/study completion publish typed changes;
14. career-grant completion is explicitly marked `SkillTargetAlreadyMet`.

Existing H1/H2/R2 and M2 suites remain green.

## Visual evidence

None.

This slice contains no user-visible Unity screen, scene, prefab, animation, art, or runtime UI change. No Editor screenshot or PlayMode recording is claimed.

## Save impact

- SaveVersion remains **1**.
- Envelope/DTO persistence shape remains unchanged.
- Synthetic-v0 migration remains unchanged.
- No outcome fields are serialized.
- Receipt retention remains required for deterministic retry reconstruction.
- No migration is required by this slice.

## Known limitations

- Outcome reconstruction for an old retry is O(committed receipt history). This is correctness-first for MVP; scaling/storage cost remains a later runtime/mobile performance measurement.
- Receipt compaction is still prohibited until a separate design proves exactly-once retries and outcome reconstruction remain safe.
- The public API has engine-free evidence only; Unity compiler/import, EditMode, PlayMode, IL2CPP/AOT, Android/iOS, device lifecycle, physical filesystem durability, and performance remain separate gates.
- Future deferred business/event systems may extend outcome delta families; such extensions must preserve immutable Core-only publication and save-compatibility rules.
- PR #13 is not independently architecture-approved until Astra re-verifies F1/F2 and the final API semantics.

## Files changed

Production:

- `Assets/StartupLife/Scripts/Core/Contracts.cs`
- `Assets/StartupLife/Scripts/Core/GameState.cs`
- `Assets/StartupLife/Scripts/Application/GameSession.cs`
- `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs`

Verification:

- `tools/SimulationChecks/Program.cs`

Documentation:

- `docs/adr/ADR-003-transactional-outcomes.md`
- this evidence file.

## Closeout gate

Do not freeze this API, start Gemini content/schema scale-out, or mark M1 complete until an independent Astra review confirms that the outcome fields are sufficient for Presentation, retry reconstruction is semantically correct, playback remains reward-independent, and H1/H2/R2/M2 stay clean.
