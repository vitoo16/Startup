# M10-T01 — Founder journey stage, development-first implementation

**Owner:** GPT-6 (Technical Director / implementer)  
**Base:** main `1fb388796385cc382ef849820f21c4f01f216c1f`  
**Scope:** Reusable derived character career stage and V3 founder transition regression. No authoritative save DTO/schema change.

## Session
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-game-economy, unity-qa-release.
- **Acceptance criteria:** Distinguish unemployment from active founding; preserve current and historical employment identities; transition from side business owner to full-time founder on resignation; transition back on re-employment and after business closure; rejected duplicate resignation must leave checkpoint untouched. Existing CAS/replay, historical Save V2/V3 regression, economic tests remain mandatory.
- **Save impact:** None. Read model uses the existing employment, business, claim and receipt-backed state. Frozen historical engine, migration and financial rules unchanged.
- **Visual evidence:** NOT RUN (consolidated MVP visual/runtime acceptance as authorized).
- **Known limitations:** M10-T02 fixed helper, kiosk art and movement remain pending; this is not M10 closeout.
- **Tests:** Engine-free CI required for branch HEAD; record result only after actual workflow completes.
- **Files changed:** CareerBiographyProjection.cs; SimulationChecks/Program.cs; EconomicChecks/Program.cs; this session record.
