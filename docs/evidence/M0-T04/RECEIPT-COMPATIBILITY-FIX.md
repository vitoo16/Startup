# PR7 receipt compatibility and truncated-skill follow-up

Date: 2026-09-30. Scope: the remaining H1 receipt-format gap and R2 skill truncation classification. H2 preservation/classification behavior is retained. This evidence does not complete a milestone or implement M1/M2.

PR6 `abc` batch receipts and pre-PR6 `$batch/3:abc` batch receipts can be byte-identical. The four [historical fixtures](../../../tools/AstraFoundationChecks/Fixtures/README.md) were produced independently by both historical implementations and compared by SHA-256. Automatic caller reassignment would reintroduce the collision. The [ADR-003 compatibility contract](../../adr/ADR-003-transactional-outcomes.md) therefore requires trusted per-group owner bindings for ambiguous records, blocks writes without them, and persists their ownership atomically on the next successful normal commit. Completed retries replay the original results without rewriting the checkpoint. Ordinary legacy roots and PR6 encoded roots whose alternative raw owner exceeds the historical limit do not need bindings.

`StateValidation.Validate` still reports unavailable saved skill definitions as `UnsupportedContent`. Once all saved IDs resolve, an incomplete character skill collection is structural corruption, allowing valid-backup recovery. New content sets must change the catalog content version; incomplete state is not used to infer a catalog change.

Session closeout:

```text
Owner model: Primary Codex implementation fallback (Sol unavailable in this session).
Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian;
  unity-gameplay-systems; unity-game-economy; unity-game-director;
  unity-mcp-bridge; unity-debug-profiler; unity-qa-release.
Acceptance criteria: no guessed receipt ownership; PR6/pre-PR6 completed and partial
  day/month retries with proven ownership; no duplicate effects after restore;
  atomic ownership normalization; unsupported primary protection retained;
  truncated skill collections remain corruption; no milestone checkbox changes.
Tests: engine-free simulation 35/35; focused regression 14/14;
  .NET Release builds 0 warnings / 0 errors;
  static Unity foundation 31/31; runner contract 30/30;
  repository security/release hygiene 28/28; documentation validation;
  git diff --check; clean-worktree checks after verification.
Visual evidence: not applicable; no visible scene/UI change.
Save impact: schema 1 unchanged; existing synthetic-v0 migration retained.
  Historical internal receipt IDs normalize only within a successful candidate commit;
  operation/revision/payload/target/cue/minutes and gameplay state remain intact.
Known limitations: ambiguous historical records require trusted provenance, not a
  caller-supplied retry guess. Completed retries retain old bytes until a later commit,
  so provenance must be supplied on subsequent restores until then. No Unity runtime,
  EditMode, PlayMode, IL2CPP, mobile build, signing or physical-device evidence.
Files changed: GameSession.cs; StateValidation.cs; AstraFoundationChecks.csproj;
  AstraFoundationChecks/Program.cs; four historical JSON fixtures and their README;
  ADR-003; this closeout; IMPLEMENTATION_PROGRESS.md.
Commit: see the Git commit introducing this evidence; CI validates that committed head.
```
