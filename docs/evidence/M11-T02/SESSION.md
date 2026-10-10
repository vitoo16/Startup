# M11-T02 — Released save V1/V2/V3 replay matrix and test-fixture V0 refusal

**Owner:** GPT-6 (Technical Director/implementation).  
**Base:** `main` `a1baf23763efdd9432bfe2a389829d1c106c3a48`.

## Test coverage
- Positive historical V1 fixture: `docs/evidence/M7-T02/RUNTIME-2026-10-04/android-final/B-before-work.save.json` — a real Android-captured source with `ContentVersion=first-playable.v1` and two receipts.
- Positive V2 candidate is deterministically raised by the unchanged `V1ToV2Migration` from the real V1 source bytes. No alternate authored/reinvented checkpoint is used.
- Positive V3 fixture: `tools/EconomicChecks/Fixtures/pre-fix-v3-pending-business.json` — real previously-valid V3 save with pending price/resume.
- The existing `tools/SimulationChecks/Fixtures/synthetic-v0.json` and `current-v1.json` instead use `ContentVersion=fixture.v1` (unreleased test catalog). They **must be rejected as UnsupportedContent** by production V3; test both. Synthetic V0 never shipped.
- Link all historical fixture inputs into EconomicChecks output without duplicating source-control bytes.
- Run released V1, promoted V2 and V3 through the **actual live V3 compatibility serializer**; require valid original schema metadata, exact original-source byte preservation and no write during cold restore. Test-only fixtures use fail-closed behavior.
- Continue the **same deterministic authorized command** from two separate cold restores, verify byte-identical V3 persisted output, prior original-wire backup preservation, post-cutover cold replay and idempotent retry.
- For the pre-fix V3 fixture, preserve pending pricing/resume and require exact wire round trip.
- Mutate scheduler and event RNG **independently**, rebuild a valid checksummed envelope, then require strict V3 replay to reject each mutation. Original source remains unchanged.
- Remove one repeated project reference in `EconomicChecks.csproj`; no gameplay rule duplication or dependency changes.

## Session
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-cli, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Save impact:** None; tests and csproj only. No changes to serializer, migration, frozen historical source, economic balance, validator or receipt replay thresholds.
- **Tests:** Exact-HEAD engine-free CI + CodeQL required; pending until completed.
- **Visual evidence:** NOT RUN (no visuals changed, Unity device acceptance deferred).
- **Known limitations:** V0 is intentionally a synthetic fixture with unknown content and must not be claimed as production-migratable; this proves one Android-captured V1 fixture, one V1-promoted V2, and one pre-fix V3 checkpoint, not every historical gameplay state or real device interruption. **M11 not CLOSED.**
- **Files changed:** tools/EconomicChecks/Program.cs; tools/EconomicChecks/EconomicChecks.csproj; this evidence.
