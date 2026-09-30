# Historical restore and corruption precedence closure

Date: 2026-10-01. Scope: the two remaining findings after PR7 (`f970a58`): durable ownership recovery, and cross-field corruption masked by compatibility resolution. H2 protection is retained. No milestone checkbox is completed.

H1 uses `GameSession.TryRestore` / `Recover(owners)` to normalize known historical ownership durably before returning a usable session. A completed batch needs no new gameplay command to persist its binding. A new store/session without the dictionary can immediately restore/retry, including after corruption of primary and recovery from the normalized backup. Historical pre-PR6 and PR6 fixtures run under both known provenance interpretations; completed and partial day/month cases retain original receipt operation/revision/minutes. Partial continuation matches an uninterrupted session byte-for-byte. Legacy direct `$batch/3:abc/0` remains independent of a new `abc` batch, and fresh `abc` cannot replay the pre-PR6 `$batch/3:abc` owner.

The dedicated recovery write compares the complete prior checkpoint, changes only historical child IDs, retains revision, and uses the writer lock and validated replacement protocol. Tests reject cash changes, direct receipt renaming, stale competing ownership resolution at the same revision, and normal-commit bypass. Fault injection covers before replacement, after replacement and before compatibility backup promotion. A malformed group that would create duplicate normalized IDs fails closed without writing.

R2 tests combine missing skill content, unavailable career revision or unavailable whole-catalog version with negative XP, empty/malformed career ID, empty revision, invalid scheduler cursor, negative receipt minutes, negative ledger amount and malformed historical employment/course data. All classify Corrupt. Missing scene content cannot hide a malformed receipt. A genuinely corrupt primary can recover a valid backup. Structurally valid missing skills/scenes/revisions still classify UnsupportedContent and remain protected from backup fallback and both normal and compatibility writes.

Closure: H1 CLOSED under the explicit trusted-provenance recovery contract; H2 CLOSED; R2 CLOSED. Unresolved historical provenance remains a recovery diagnostic, because the historical fixtures demonstrate two owners can produce identical bytes. It is never inferred from the retry caller. Once provenance is supplied to the supported import path, ownership is durable immediately.

```text
Owner model: Primary Codex implementation fallback recorded in the active ledger.
Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
  unity-gameplay-systems; unity-game-economy; unity-game-director;
  unity-mcp-bridge; unity-debug-profiler; unity-qa-release.
Acceptance criteria: supported durable legacy import/restore; completed retry requires
  no extra gameplay commit; fresh-process and backup retry without transient bindings;
  partial/completed day/month and direct/batch isolation; cross-field corruption
  precedence; unsupported primary protection; no schema or milestone checkbox change.
Tests: simulation 35/35; focused regression 19/19; .NET builds 0 warnings / 0 errors;
  static foundation 31/31; runner contract 30/30; security/release hygiene 28/28;
  documentation validation, git diff --check and clean-worktree verification.
Visual evidence: not applicable; no scene, UI or production asset changed.
Save impact: schema/envelope/DTO shape unchanged at version 1; synthetic-v0 migration
  retained. Dedicated receipt-ID recovery checkpoint keeps the logical generation and
  all gameplay/receipt result fields; backup becomes the same normalized generation.
Known limitations: unknown provenance cannot be reconstructed from identical bytes;
  safe recovery requires the trusted importer binding once. Unity runtime, EditMode,
  PlayMode, IL2CPP, mobile build/signing/device evidence remains unverified.
Files changed: GameSession.cs; Contracts.cs; StateValidation.cs; AtomicFileSaveStore.cs;
  JsonSaveSerializer.cs; AstraFoundationChecks/Program.cs; ADR-003; ADR-009;
  fixture README; this evidence; IMPLEMENTATION_PROGRESS.md.
Commit: Git commit introducing this evidence; PR/CI validate that committed head.
```
