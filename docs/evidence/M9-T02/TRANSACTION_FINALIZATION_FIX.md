# M9-T02 — Receipt-backed transaction finalization repair

Date: 2026-10-11 (Asia/Bangkok). Baseline: `7c3c581557040fc38153a83da7b8d036323b24dc`.
Scope: correct the nine failures from the Unity rerun; retain strict save/replay
validation and existing economic policy. This record supplements historical
session evidence; it does not close M9-T02 or authorize merging PR #39.

## Repair plan and acceptance

1. Reproduce employment rejection through production `GameSession`, serializer,
   and atomic file store before modifying the evaluator.
2. Finalize prospective economic planning after the canonical receipt is appended
   to the isolated candidate, before outcome creation, validation, serialization,
   and CAS. Apply the same order during cold replay.
3. Preserve transaction-start invoice ordering and byte-identical compatibility
   with a valid save captured before the repair. Preserve Study/midnight lazy
   epoch opening, archived v2 semantics, save schema and command discriminants.
4. Correct the obsolete two-business assertion against the authored four-business
   catalog, retaining its content identity/type/window checks.
5. Verify rejected commands, failed writes, retries, resignation after work,
   live/cold continuation, complete engine-free regression and Unity EditMode /
   PlayMode. Capture a separate graphical portrait flow.

## Root cause and correction

`V3EconomicReceiptProcessor.Evaluate` changed employment and immediately called
`EnsureOpened`. `CommittedTimeTrace.Project` then compared this new state against
a receipt list that still lacked `AcceptJob` or `Resign`. Its complete-history
check correctly rejected the inconsistent candidate. `GameSession` returned
`state.invalid`, leaving the career empty; seven save/lifecycle tests stopped at
the same employment setup assertion before exercising their intended scenarios.

`FinalizeReceipt` now requires the appended receipt frontier. Both `GameSession`
and `VersionedReceiptReplay` invoke it after receipt insertion. The candidate is
still isolated: finalization and source verification finish before publishing a
successful CAS result. The occupancy validator remains unchanged.

Moving obligation opening past receipt insertion exposed a compatibility detail:
invoice `CreationSequence` previously used the transaction's starting revision.
`OpenDue` now accepts that revision explicitly. Boundary opening supplies the
current checkpoint revision; post-receipt finalization supplies receipt revision
minus one. State revision stays authoritative throughout the transaction.

The previous EditMode count assertion expected two businesses while the same
asset and test already contained four. It now expects the actual authored roster.

Full regression also found a Windows checkout contract issue: `core.autocrlf`
converted the frozen `.cs.txt` archives from LF to CRLF, breaking their physical
SHA-256 gate although the Git blobs were unchanged. `.gitattributes` now preserves
archive and compatibility-fixture bytes. The local archive was restored from
original HEAD blobs, and all eight original blob identities and SHA-256 values
remain unchanged. Neither frozen source nor its expected checksums were edited.

## Verification evidence

Before repair, the new employment regression failed with `state.invalid`:
58/59 checks passed. After repair, 60/60 economic checks pass, including:

- Atomic accept/resign/re-employment with an active business and real career work.
- Duplicate acceptance and rejected acceptance preserve save bytes/revision.
- Failed resignation write leaves the continuously running session and file
  unchanged.
- Resignation preserves committed career occupancy and releases future career
  time.
- Independent cold and continuously running next-boundary continuation produce
  identical persisted bytes.
- The immutable pre-fix v3 fixture passes strict replay and serializes to the
  exact original envelope bytes, including pending pricing/pause/resume policy.

Fixture provenance and SHA-256 are documented in
`tools/EconomicChecks/Fixtures/README.md`.

Final canonical Unity run: Unity `6000.3.25f1 (e1dba0a9aba4)`, CLI beta.12;
EditMode 9/9 and PlayMode 14/14 PASS, zero failed/skipped/inconclusive. NUnit and
JUnit identities/counts agree. The runner process and both Unity modes exit 0.
`STARTUP_LIFE_M7_EVIDENCE` was unset for this canonical headless run.

The tested Unity source is the baseline plus the retained exact production/test
patch, SHA-256 `823630831b70480cee683473aa1788594979d79c7501d9ac78c4cafc090afddb`.
It is a local repair verification, not a new committed-HEAD or GitHub CI claim.

External evidence root:
`C:/Users/viett/.codex/visualizations/2026/10/10/01a126b7-df3d-7ee2-a43e-3e5a917da2f0/unity-fix-7c3c581/evidence/`.
Canonical logs/XML and patch are under `final-unity/`; engine-free reports and
separate graphical evidence are retained alongside them. Published log copies
must disclose token/path redaction; preserve private original logs separately.

## Session closeout

Owner model: Codex implementation; Astra read-only transaction/source review.

Skills used: `startup-life-session-orchestrator`,
`startup-life-gameplay-guardian`, official `unity-cli` and its test/run reference,
`ui`, `ui-ugui`, `vietnam-art-direction`. Community gameplay/economy/MCP/QA skills
were unavailable locally; this bounded repair followed repository invariants and
the official runner contract with Astra source review.

Tests: employment before/after reproduction, pre-fix v3 fixture, final Unity 9/9
EditMode + 14/14 PlayMode. Canonical `Test-Simulation.ps1` exits 0: simulation
49/49, business ownership 90/90, foundation 20/20, restore invariants 35/35,
presentation 12/12, catalog 13/13, economy 60/60; frozen v2 source 8/8. The final
employment regression retains the continuously running session while verifying
independent cold snapshots after each command. All seven static/documentation/
security invocations exit 0; `git diff --check` passes.

Visual evidence: graphical PlayMode capture was executed with real rendering,
1 test failed (Unity exit 8) on the existing URP import error:
`Host type is not matching any asset type at Path
Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/RenderingLayerMask/TraceRenderingLayerMask.urtshader.`
Only the initial character-creation PNG was captured before the unexpected-log
failure. No career/work/study visual acceptance is claimed. The error is retained
without adding a `LogAssert.Expect`, suppressing logs, switching pipelines, or
changing packages. Graphical verification remains a separate open limitation.

Save impact: schema remains 3; no migration format change. Old valid v3 fixture
retains exact bytes; all frozen v2 archival bytes are preserved. Tests use
disposable engine-free save files; Unity test fixtures preserve and restore
existing application saves.

Known limitations: graphical capture blocked by the URP `.urtshader` import
failure; no physical-device, Android issue #36, full business portrait audit or
entire M9-T02 milestone approval. Prior independent source/evidence governance
remains separate. A transient Windows build file-lock failure is retained
separately from the final sequential regression verdict.

Files changed: `.gitattributes`; `GameSession.cs`;
`V3EconomicReceiptProcessor.cs`; `V3ObligationTransactions.cs`;
`VersionedReceiptReplay.cs`; `ContentCatalogTests.cs`;
`tools/EconomicChecks/Program.cs` and project fixture-copy configuration;
pre-fix fixture and README; this repair record. Unity's dynamic TMP atlas changes
are confined to the isolated verification checkout.

Commit: local working changes; no merge or milestone closure.
