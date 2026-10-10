# M12-T01 — Immutable career, skill and finance dashboard projection

**Owner:** GPT-6 Technical Director. **Parent:** main `381cdc734b4c912a0ad1c4c7c2924d3d5ffd8419`.

## Architecture and scope
- A single **read-only, same-revision** `GameSession.ReadLifeDashboard()` snapshots career biography, skill levels/exposure, course requirements, cash, exact rational unpaid salary, debt rows and *committed* daily business settlements.
- Reuses existing `CareerBiographyProjection`, `GameSnapshot.Level` and `BusinessFinanceProjection`; never reimplements career progression, course study increments, salary or finance settlement.
- Six skill rows derive from authored content, not a hand-written list in UI. Course rows expose authored base minutes, target levels, prerequisites and price, plus actual progress for active course; they are informational, not a duplicate eligibility engine.
- V3 settled amounts are recorded facts, not a revenue projection; no synthetic historical settlement on V2.
- Immutable lists and no mutable save DTO exposure. No UI event can cause an implicit command through this projection.

## Session and gates
- **Skills:** startup-life-session-orchestrator; startup-life-gameplay-guardian; ui; ui-ugui; ui-ugui/references/scrollview-setup.md; localization; vietnam-art-direction; unity-game-director; unity-gameplay-systems; unity-mcp-bridge; unity-qa-release.
- **Tests:** Engine-free SimulationChecks checks no-write reads, 6 authored skills, course metadata, unpaid exact rational salary and cold restore. Exact-HEAD CI and CodeQL required before integration.
- **Save impact:** none; no changed DTO, migration, receipt or balance.
- **Visual:** NOT RUN; binding into portrait navigation and proof of phone-safe scrolling remain next M12 slice.
- **Known limitations:** This is a pure dashboard data contract, not the finished 5-tab UX; full mobile screens, Portuguese/Vietnamese font coverage and animation unaffected. **M12-T01 not CLOSED.**
- **Files changed:** new LifeDashboardProjection.cs and .meta; GameSession.cs; tools/SimulationChecks/Program.cs; evidence note.
