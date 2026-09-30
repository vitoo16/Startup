# Provisional first playable architecture review

Date: 2026-09-30. Reviewer: delegated architecture reviewer. Runtime/source owner: primary Codex, following explicit takeover when Sol agents became unavailable. Scope: read-only review of Core, Simulation, Application, Infrastructure, and the .NET scenario harness against the [frozen contracts](../../architecture/FIRST_PLAYABLE_CONTRACTS.md).

Verdict: useful provisional engine-free implementation; **three actionable findings remain in the reviewed revision**. This is not M1 completion or Unity acceptance. Primary owner is responsible for fixes and new regression evidence. Findings below refer to the source snapshot read during this review; resolve explicitly with follow-up evidence rather than removing the original observations.

## Findings

### AR-01 — P2: a writer that loses the file lock can delete another writer's temporary save

Location: `Assets/StartupLife/Scripts/Infrastructure/AtomicFileSaveStore.cs:45` and `:75`.

Every instance uses `primary + ".tmp"`. The unconditional finally block deletes this path even when opening the `.lock` stream failed and this attempt never created a temporary file. Reproduction interleaving: writer A acquires the lock, writes/closes its temp and pauses before replacement; writer B attempts the same path, fails the lock, then deletes A's temp in finally; A's replacement now fails. Per-instance `gate` does not protect two store instances/processes. The previous primary remains intact, but legitimate checkpoints fail under contention and B modifies files without owning the write lock.

Fix: use unique per-attempt temp paths and only delete the path created by that attempt, or restrict cleanup to the attempt that both acquired the cross-process lock and created the shared temp. Keep lock ownership through validation, promotion, and cleanup. Regression: second store attempts a commit while the first is paused after temp validation; first commit must still succeed and second must leave its files untouched.

Evidence: static control-flow review; no concurrent filesystem fault-injection test was run by this reviewer.

### AR-02 — P1: transient read failures are treated as corruption and allow rollback

Location: `Assets/StartupLife/Scripts/Infrastructure/AtomicFileSaveStore.cs:18` and `:30`; `Assets/StartupLife/Scripts/Application/GameSession.cs:72`.

`ReadFile` maps IOException and UnauthorizedAccessException to Corrupt. `Read` therefore accepts an older backup when the valid latest primary is merely temporarily unreadable, and `GameSession.Recover` publishes that older state. For example, a file-sharing lock on the primary triggers rollback even though its checksum/content is still valid. Once access returns, subsequent commits can fail their revision compare because the primary is newer than the recovered state. This is a storage-access problem, not evidence authorizing a previous-generation rollback.

Fix: return a distinct read/I/O failure status and preserve the current state, blocking/retrying recovery without fallback in that case. Fall back only when a primary is confirmed missing or its bytes fail content/integrity validation. `File.Exists` can also report false for access failures; prefer opening the file and distinguishing missing-file exceptions explicitly. Regression: make the primary temporarily unreadable while leaving a valid older backup readable; no rollback or publication of the backup occurs, and removing the fault restores the latest generation.

Evidence: static exception/fallback path review; device filesystem behavior remains untested.

### AR-03 — P2: restore validation accepts inconsistent activity/deck state

Location: `Assets/StartupLife/Scripts/Core/StateValidation.cs:29` and `:34`; `Assets/StartupLife/Scripts/Simulation/SimulationEngine.cs:135`.

The validator checks minute range, scene count range, and that every deck ID exists in some career. It does not verify active shift boundary alignment, WorkDateIso/ScenesToday coherence, or that a stored active deck matches the active career when its signature claims to be current. A checksummed/migrated snapshot at Developer minute 600 is accepted with the harness's 540-start/120-minute scene slots, but the next advancement rejects `activity.invalid_boundary`. With multiple careers, an ID from another career can pass validation and later make `career.Scenes.Single(...)` throw outside GameSession's existing expected-failure catches.

Fix: validate semantic relationships on load: active-career deck membership and signature, cursor/last-scene consistency, work date and completed slot count, and current time relative to the saved shift/activity. Preserve the documented allowed stale-signature transition after promotion until the implementation rebuilds the suffix atomically. Reject inconsistent fixtures before accepting them as loadable saves; do not “repair” by replaying rewards.

Evidence: an in-memory probe against the built provisional DLL created a character, accepted Developer employment, set a candidate restored minute to 600, and called StateValidation successfully. The next SimulationEngine advance returned/threw `activity.invalid_boundary`. Console output:

```text
Restored in-shift minute 600: validator ACCEPTED
activity.invalid_boundary
Snapshot detached history: True
```

This probe made no source changes or filesystem save writes. The cross-career deck variant was identified statically and needs a regression fixture.

## Reviewed behavior that matches the current provisional scope

- Application clones the initial state and each transaction candidate through validated serialization. Rejected/overflowing candidate evaluation does not mutate the live state or its RNG. The immutable command envelope binds canonical payloads to retry IDs.
- Committed candidate state and receipt are saved before publication; ambiguous post-replace results set RecoveryRequired. Receipt retry returns the original operation rather than applying rewards again. Store generation compare and command serialization support this boundary, subject to AR-01/AR-02.
- Snapshot primitive values and read-only copied skill/history collections do not expose the session's mutable collections. The review probe confirmed changing source history after snapshot construction does not change snapshot history.
- Salary uses exact rational claims, a full-calendar-month scheduled-day denominator, and a whole-VND payday floor with remaining claims retained. Resignation moves employment to history without deleting claims. Payday excludes claims earned on that date. No rounding-loss bug was found in these current paths.
- Daytime work and study use the same minute cursor. FreeMinutes caps morning study at work start and evening study at sleep; content validation rejects work outside wake/sleep bounds. No ordinary first-playable work/study overlap was found. Businesses are not implemented and their conservation gate remains pending.
- Quota state persists its actual deck/cursor, independent scheduler/event states, and deterministic RNG version. Consumption keeps the used prefix during a signature change. Existing rank changes rebuild the suffix on the next consumption, rather than in the promotion transaction; restore remains deterministic under the same content, but that timing differs from the frozen contract and should be reconciled before M1/M5 acceptance.

## Scope gaps to retain in the ledger

These are incomplete planned features, not additional defects in a claimed completed milestone: immutable structured SimulationOutcome/history/reward cues are reduced to primitive results/string cues; rank-specific quota distributions, calendar-anniversary milestone grants, prior-history starting packages, and the complete initial save root (business/event/settings/helper fields) are not implemented. The current rank schema uses service days. Historical salary claims preserve exact earned numbers but do not record the full salary-rate/schedule-segment attribution specified by the contract. None of these simplifications should silently become the final schema; later shape changes require versioned migration coverage.

A `.NET netstandard2.1/C# 9` compile or the [.NET scenario report](test-report.json) does not prove Unity import, asmdef dependency enforcement, PlayMode, IL2CPP serializer preservation, Android/iOS lifecycle, mobile replacement durability, or physical-device behavior. The reviewer inspected the report; did not independently rerun the full harness. Editor/device/Mac prerequisites remain external blockers, and M0/M1 remain incomplete.

## Reviewed source fingerprints

SHA-256 at review time:

| File | SHA-256 |
|---|---|
| AtomicFileSaveStore.cs | 0496D40D321680D60296284F302D2EBF3DE92FF33F70C449FF21019F99C529F1 |
| StateValidation.cs | 7DC5C4E8DE70B2F5EC52942B2AEFDCE704DFDC87C8B50485CD526E70EA0FE721 |
| SimulationEngine.cs | 3F9E88A3935BAB5C8DD3220BCD4C21C5A10BA963AE59CA7535E6EE979AAFBC77 |
| GameSession.cs | C1D835F79451E706122737BC6EF847EC190888C820B24EF5D82536A668BCC395 |

## Session closeout

- Skills used: startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-debug-profiler, unity-game-economy; previously loaded in this architecture session.
- Tests: read-only source/contract review; inspected current .NET report; in-memory invalid-boundary and detached-snapshot probes against compiled .NET DLL. No Unity, device, or new filesystem fault-injection tests.
- Visual evidence: None; no visible changes.
- Save impact: review only; no schema or save files changed by this reviewer.
- Known limitations: findings await owner fixes/regressions; all Unity/mobile/platform gates remain pending; no complete-milestone claim.
- Files changed: only `docs/evidence/PROVISIONAL-FIRST-PLAYABLE/ARCHITECTURE_REVIEW.md`.
- Commit: not created by reviewer.

## Follow-up verification — 2026-09-30

The primary owner applied fixes after the source snapshot above. Reconciliation below supersedes the original “three findings remain” verdict for these specific defects; original findings and fingerprints remain as history. **AR-01, AR-02, and the demonstrated AR-03 defects are addressed in the provisional .NET implementation.** No milestone acceptance is implied.

| Finding | Source recheck | Regression evidence | Status |
|---|---|---|---|
| AR-01 | Each attempt now chooses `primary + ".tmp-" + Guid`, and finally cleans up only its own path. A losing writer cannot name the winner's temp. | `competing writer cannot delete active writer temp`: pauses writer A before replace, runs writer B against the held file lock, checks B fails and A's temp survives, then verifies A commits and temp cleanup completes. | Addressed for reviewed local-file implementation. |
| AR-02 | ReadFile calls ReadAllBytes directly; FileNotFoundException/DirectoryNotFoundException return Missing, other I/O/access errors return Unreadable. Read returns Unreadable without backup fallback. Recover publishes only Valid/RecoveredBackup, preserving existing state for Unreadable. | `transient read lock does not roll back to backup`: holds a real exclusive lock on current primary with a valid backup present; Read and Recover return Unreadable and checkpoint bytes remain unchanged; unlocked primary returns Valid. | Addressed for tested Windows/.NET lock path. |
| AR-03 | Active work cursor must align to configured slots; expected completed scene count and WorkDateIso must agree with the date/time. Active deck IDs must belong to the current career. | `invalid restored work cursor and reward coherence rejected`: checksummed fixtures at invalid minute 600 and aligned minute 660 with missing completed scene both return Corrupt. | Demonstrated cursor/reward defects addressed; cross-career membership fix inspected in source. |

The final [test-report.json](test-report.json), written at `2026-09-30T02:22:14Z` after the direct-read source edit, reports **35 passed, 0 failed on .NET 8.0.31**. The reviewer inspected the changed source, the regression implementations, report contents, and file timestamps; the full suite was run by the implementation owner, not rerun by the reviewer. This remains .NET evidence, not Unity/EditMode/PlayMode or device evidence.

Residual validation work before the complete save milestone includes exact scheduler signature/cursor/LastScene coherence and explicit multi-career invalid-deck fixtures. The current source rejects foreign-career IDs but has no added regression for that variant. These remaining coverage items do not invalidate the two new restored-work-state regressions.

Rechecked fingerprints:

| File | SHA-256 |
|---|---|
| AtomicFileSaveStore.cs | F8770DB795F2330C64B4DDCFCD20228C9CD5A0627631E46EC8AF39BED8A36A22 |
| StateValidation.cs | 1C5C91B4685790B60EEF93D7EB177752D8D8EDEAEEAF19B09217E3A6AD0BDB68 |
| Contracts.cs | A3AC9F41D121E8398A5F690B3CA07137398CEABF11A947CDC4E5E0FB07033920 |
| test-report.json | AD3B6652CF91B8FB0EFFE3EEA246B7625EDFDB3A54363218E0AB9D682907A527 |

The full initial schema remains provisional: metadata timestamps, independently modeled persistent employer history, background age/history/grant packages, salary-rate segment attribution, complete ISimulation/Evaluation/SimulationOutcome contracts, and reserved business/event/helper/settings state remain incomplete. This is a tested subset prepared ahead of dependency verification. M0/M1, Unity integration, serializer/BigInteger IL2CPP preservation, mobile storage/lifecycle, Mac/iOS, and physical-device acceptance remain pending.

Follow-up closeout: same loaded skills; source/report review only; visual evidence none; save/schema impact none from reviewer; only this review document appended; no reviewer commit.
