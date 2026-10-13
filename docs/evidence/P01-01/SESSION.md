# P01-01 — V4 batch binding and overtime CAS recovery

**Date:** 2026-10-14  
**Owner model:** GPT-6 Technical Director  
**Starting main SHA:** `3acb96162b3dd8ac2c5f3fa5b468a1d4bcaac3b0`  
**Status:** IMPLEMENTED_UNVERIFIED until exact-commit CI result is collected; Unity/device pending.

## Slice / authority

- **Domain:** V4 command batch / atomic save / simulated finance and time.
- **Skills used:** `.agents/skills/startup-life-session-orchestrator/SKILL.md`, `.agents/skills/startup-life-gameplay-guardian/SKILL.md`. Reviewed AGENTS, roadmap P01-01, GDD, historical MVP plan, AI workflow and skill manifest.
- **Acceptance criteria:** A day/month batch that successfully saves its intent must rebind to its own root command on subsequent cold reads rather than using whichever batch another writer most recently appended. A verified completed-shift overtime choice must survive pre-promotion failed writes and after-promotion ambiguous writes; cold retry credits exactly one cash delta, time debit, V3 receipt and V4 outcome without duplicating anything.
- **Implementation:** Select `AdvanceBatches.SingleOrDefault(x => x.RootCommandId == request.CommandId)` after durable intent registration and cold read. Reject unexpected missing/mismatched intent with recovery status rather than running another batch. Extend existing overtime end-to-end economic regression with actual `AtomicFileSaveStore` fault hooks `during-temp-write` and `after-replace`, then cold replay/byte-identity assertions. Reuse established runner, V3 serializer and V4 journal. No second scheduler or payout engine.
- **Tests:** Source changes will run through `scripts/Test-Simulation.ps1` / `tools/EconomicChecks/Program.cs` in GitHub engine-free CI. **No local .NET/Unity installed here; no self-reported PASS.** Re-run on exact commit SHA and inspect failures.
- **Visual evidence:** NOT RUN; simulation-only scope, no UI/asset modifications.
- **Save impact:** NONE intended. No schema or wire fields, no historical fixtures/catalogs and no gameplay balances changed. Existing V2/V3/V4 historical replay behavior preserved; proof must come from CI.
- **Known limitations:** Batch rebinding fix is defensive against concurrent post-commit readers; a deterministic injected concurrent interleaving test is still desirable. Unity EditMode/PlayMode, Android/iOS runtime, and field upgrade evidence remain pending. The expanded overtime test covers one failure-before-promotion and one ambiguity-after-promotion for the same reward.
- **Files changed:** `Assets/StartupLife/Scripts/Integration/EventV4AdvanceCoordinator.cs`, `tools/EconomicChecks/Program.cs`, `docs/evidence/P01-01/SESSION.md`.
- **Commit:** Inspect the enclosing Git commit; documentation must never preclaim an uncreated SHA.
