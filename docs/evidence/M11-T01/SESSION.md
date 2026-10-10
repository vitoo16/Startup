# M11-T01 — Transaction interruption fault matrix (development-first)

**Owner:** GPT-6 Technical Director. **Base:** main `12d900d1855e62ce7fb6e72846e298271eef4002`.

## Change
- Add fault-injection-only phases to existing `AtomicFileSaveStore.CommitCore`: `before-temp-write`, `during-temp-write` (persist a deliberately incomplete temporary checkpoint), and `after-temp-flush`. Production path without the injected callback remains byte- and policy-identical.
- Reuse existing per-writer unique temp files, lock/CAS, validated-readback and `finally` cleanup. Failed pre-promotion writes never alter primary or backup.
- Engine-free regression loops through 4 pre-promotion faults; requires failed command to leave bytes & runtime state intact, zero orphan temp, and retry with identical command ID to commit once.
- Additional corruption tests require dual-corrupt primary/backup to refuse recovery without rewriting and future-version backup to outrank a corrupt primary (no destructive downgrade).

## Gates
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release; unity-debug-profiler; unity-cli; unity-localization.
- **Tests:** Run exact-head engine-free CI + CodeQL and record result. Existing save/replay tests remain unweakened.
- **Save impact:** None. No schema change/migration, no changes to canonical serializer or frozen v2 source.
- **Visual evidence:** NOT RUN; mobile lifecycle, real physical low-storage and process-kill at byte-level remain end-of-MVP QA.
- **Known limitations:** Artificial partial-write injection verifies pre-promotion isolation; real filesystem ENOSPC and storage media failures still need device acceptance. This task is not marked CLOSED.
- **Changed files:** AtomicFileSaveStore.cs; tools/SimulationChecks/Program.cs; this record.
