# M2 restore provenance follow-up

Date: 2026-10-01. Base: merged PR9 (`c8dd245`, PR head `1fde56a`). Branch: `codex/m2-restore-provenance`.

This slice fixes the remaining M2 review findings: a coordinated cursor/activity rewind could retain salary and XP, impossible scheduler decks/generations or stale signatures could load, and a global entity sequence could be reused across kinds or ignore retained references. M1 outcome/playback work and milestone checkboxes remain outside this slice.

## Corrections

- **M2-A:** `StateValidation.ValidateReceiptTimeline` binds the complete ordered receipt history to canonical boundary activities, contiguous revisions, simulation time and `CurrentActivity`. A detached `SimulationRestoreValidator` then verifies persisted rewards, employment, salary claims, arrears, ledger, courses and history against actual committed commands. Removing a receipt and adjusting the cursor cannot retain its rewards. Invalid saves classify as `Corrupt` before `GameSession.TryRestore` publishes a session; the valid authoritative checkpoint is unchanged.
- **M2-B:** the verifier uses the runtime `QuotaScheduler` and shared promotion eligibility calculation to reconstruct every known employment transition. Deck order/quota, cursor, cycle, generation, signature, last scene and scheduler RNG must match exactly. A legitimate old-rank signature immediately after promotion remains valid until the next consume; the already-consumed stale signature is rejected. Cycle seams, resignation and subsequent employment continue byte-identically after restore.
- **M2-C:** entity sequence numbers are unique across generated kinds. `NextEntity` covers issued entities and typed references retained by ledger, grants and resignation history, including settled arrears. Same-ID references may repeat; two distinct IDs cannot own the same global sequence.

The serializer requires the Core verifier port explicitly. Application supplies the Simulation implementation, so Infrastructure gains no Simulation dependency and no caller can accidentally omit verification through the old constructor. Detached reconstruction never changes the candidate or commits storage.

Intrinsic corruption checks run before compatibility resolution. Known scheduler checks still run if an unrelated background is unavailable. Missing required current or historical scheduler scenes remain `UnsupportedContent`; an existing scene belonging to another career remains `Corrupt`. Required unavailable career revisions defer reconstruction that cannot be determined. The existing H1/H2/R2 ownership and unsupported-primary storage paths are unchanged.

## Verification

Local .NET/static evidence:

- [Existing simulation](m2-provenance-simulation.json): **35/35**.
- [H1/H2/R2 regression](astra-foundation-highs-report.json): **19/19**, including durable historical ownership, completed/partial day/month retries, backup protection and corruption precedence.
- [M2 restore regression](astra-m2-restore-invariants-report.json): **31/31**. The original seven cases remain; new cases include the review reproductions, coordinated receipt/cursor rewind, forged rewards and salary, non-career activity removal, equal-count history forgery, malformed/reordered receipts, global identity/reference reuse, plausible generation/RNG tampering, compatibility precedence and valid multi-scene promotion/cycle/resignation continuations.
- All three harness builds: **0 warnings / 0 errors**.
- [Static Unity foundation](m2-provenance-static-unity.json): **31/31**.
- [Static runner contract](m2-provenance-static-runner.json): **30/30**.
- [Repository security/release hygiene](m2-provenance-static-security.json): **28/28**.
- Authoritative documentation validation and `git diff --check`: PASS.

The overflow test now reaches maximum XP through a legitimate committed scene, then verifies that the next overflowing boundary preserves the checkpoint byte-for-byte. Its previous manually forged XP fixture is intentionally no longer accepted by restore validation. Production balance data and the original 35/19 test counts are unchanged.

The committed snapshot is also verified from a fresh checkout, with reports outside the repository; the engine-free PR CI checks the same clean-worktree contract. PR/CI status is reported with the published change.

## Session closeout

```text
Owner model: Primary Codex implementation fallback, as recorded in ACTIVE_STATUS.
Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
  unity-game-director; unity-gameplay-systems; unity-game-economy;
  unity-mcp-bridge; unity-debug-profiler; unity-qa-release.
Acceptance criteria: reject coordinated receipt/activity/cursor rewinds before
  publication; reconstruct deterministic scheduler transitions; enforce global entity
  sequence/reference continuity; retain valid historical/current continuation and
  H1/H2/R2 behavior; no schema change, M1 implementation or checkbox change.
Tests: simulation 35/35; H1/H2/R2 19/19; M2 31/31; three .NET builds clean;
  static foundation 31/31; runner 30/30; security 28/28; documentation and diff checks;
  isolated checkout verification and engine-free PR CI.
Visual evidence: not applicable; no scene, prefab, UI, art or content asset changed.
Save impact: schema/envelope/DTO version 1 remains unchanged; synthetic-v0 migration
  retained. Validation tightens existing consistency rules without repairing saves.
  Supported historical fixtures still restore/retry; no receipt pruning is supported.
Known limitations: engine-free .NET/static evidence only. No Unity Editor import or
  compile, EditMode, PlayMode, IL2CPP/AOT, Android/iOS build, signing, real-device
  filesystem durability or physical-device save/resume evidence. Reconstruction
  scales with persisted receipt history; long-run device timing/allocation evidence
  remains a runtime gate. Pending choices, event RNG and outcome/playback fields have
  separate deferred contracts; this is not M1 or a complete wallet integrity audit.
Files changed: Contracts.cs; StateValidation.cs; SimulationEngine.cs; GameSession.cs;
  JsonSaveSerializer.cs; SimulationChecks/Program.cs; AstraFoundationChecks/Program.cs;
  RestoreInvariantChecks/Program.cs; ADR-009; IMPLEMENTATION_PROGRESS.md;
  this closeout and the six linked evidence reports.
Commit: Git commit introducing this closeout; published PR/CI identifies its head.
```
