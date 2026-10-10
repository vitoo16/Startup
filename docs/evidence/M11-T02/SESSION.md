# M11-T02 — Historical save V0/V1/V2/V3 end-to-end replay matrix

**Owner:** GPT-6 (Technical Director/implementation).  
**Base:** `main` `a1baf23763efdd9432bfe2a389829d1c106c3a48`.

## Test coverage
- Immutable source fixtures: `tools/SimulationChecks/Fixtures/synthetic-v0.json` (test-only synthetic V0, **never shipped**), `tools/SimulationChecks/Fixtures/current-v1.json` (historical V1), and `tools/EconomicChecks/Fixtures/pre-fix-v3-pending-business.json` (real previously-valid production V3 save).
- V2 candidate is deterministically raised by the unchanged `V1ToV2Migration` from the frozen V1 source bytes. No alternate/new source fixture is invented.
- Link V0/V1 source fixtures into EconomicChecks output without copying their bytes to a second source-control location.
- Run every source through the **actual live V3 compatibility serializer**; require valid original schema metadata, exact original-source byte preservation and no write during cold restore.
- Continue the **same deterministic authorized command** from two separate cold restores, verify byte-identical V3 persisted output, prior original-wire backup preservation, post-cutover cold replay and idempotent retry.
- For the pre-fix V3 fixture, preserve pending pricing/resume and require exact wire round trip.
- Mutate scheduler and event RNG **independently**, rebuild a valid checksummed envelope, then require strict V3 replay to reject each mutation. Original source remains unchanged.
- Remove one repeated project reference in `EconomicChecks.csproj`; no gameplay rule duplication or dependency changes.

## Session
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-cli, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-debug-profiler.
- **Save impact:** None; tests and csproj only. No changes to serializer, migration, frozen historical source, economic balance, validator or receipt replay thresholds.
- **Tests:** Exact-HEAD engine-free CI + CodeQL required; pending until completed.
- **Visual evidence:** NOT RUN (no visuals changed, Unity device acceptance deferred).
- **Known limitations:** This proves known V0/V1/V2/V3 fixture continuation, not all historical save states or real device process-kill; remaining scope requires later combined Unity/save QA. **M11 not CLOSED.**
- **Files changed:** tools/EconomicChecks/Program.cs; tools/EconomicChecks/EconomicChecks.csproj; this evidence.
