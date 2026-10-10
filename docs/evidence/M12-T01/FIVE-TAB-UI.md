# M12-T01 — Five-tab Vietnamese portrait navigation and data binding

**Owner:** GPT-6 Technical Director; development-first code (Unity runtime deferred).  
**Base main:** `23582a863e310b1c5931967ff5d5ca8681340c91`.

## Implementation
- New reusable runtime uGUI `LifeDashboardOverlay` installed under the existing **LifePanel → MobileSafeArea** hierarchy; no scene YAML or prefab GUID edits. Entry point and modal close, five visible tab buttons (`Career`, `Skills`, `Finance`, `Business`, `Profile`).
- Each tab owns a persistent page with a vertical layout under a properly configured `ScrollRect → Viewport(Image+Mask) → Content(VerticalLayoutGroup+ContentSizeFitter)`. No horizontal scroll; tab buttons and business jump use minimum 96 units at the existing reference resolution.
- Career: current rank/pay/XP and past employment, exact unpaid wage claim represented as **floor VND with explicit fraction remainder notice**, existing accept/work/resign actions.
- Skills: all six authored skills, achieved level, current exposure, next-level authored threshold; courses show authored prices, base study length and active progress; existing purchase/study actions reused.
- Finance: wallet, arrears, exact-salary claim display, **committed only** gross revenue and realized profit; debt due dates.
- Business: brief summary and a single **handoff** to existing `BusinessManagementOverlay` (no shadow duplicate pricing/launch/reinvest logic).
- Profile: name, date, work/founder status, biography count. Settings/onboarding and full localized string tables remain future M12-T03 work.
- Opening/switching tabs calls **read-only** `GameSession.ReadLifeDashboard()`; no new commands, save writes or simulation ticks. Panels display data from single-revision projection.

## Session evidence
- **Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; ui; ui-ugui; ui-ugui/references/scrollview-setup.md; localization; vietnam-art-direction; unity-ui-designer; unity-localization; unity-mcp-bridge; unity-qa-release; unity-game-director; unity-gameplay-systems.
- **Acceptance:** Five reachable tabs, 96-unit touch targets, ScrollRect/mask, tab changes preserving revision/cash, readouts and business delegation (PlayMode test added but not executed).
- **Tests:** Engine-free CI and static Unity checks required; Unity PlayMode **NOT RUN** until consolidated visual/runtime QA.
- **Visual evidence:** NOT RUN — no screenshot/device output. Font glyph coverage, small-screen layout, Vietnamese line wrap and animation still require live Unity/device verification.
- **Save impact:** None. No schema, economy, receipt or migration changes.
- **Known limitations:** Short Vietnamese UI copy is centralized in the view for this iteration. It must migrate into versioned localization tables in M12-T03 before release; this implementation is NOT final font/localization acceptance and M12-T01 is not CLOSED.
- **Changed files:** LifeDashboardOverlay.cs + .meta, LifeScreenViewController.cs, StartupLifeBootstrapper.cs, FirstPlayableFlowTests.cs and this evidence.
