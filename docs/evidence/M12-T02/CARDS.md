# M12-T02 — Multi-business selection cards and founder lock explanation

**Owner:** GPT-6 Technical Director. **Base:** main `475a7ee97f543ec701307015aff02fc61ecb9e50`.

## Implemented
- Four reusable business-selection slots, created **once** from shared `BusinessPortfolioRules.MaxActiveBusinesses`, reuse `BusinessOperationsSnapshot` and existing `BusinessManagementOverlay`. Missing slots are hidden rather than allocating/rebuilding every refresh.
- Card displays localized business type, active/paused/closed status, owner minutes from authoritative allocator and effective price posture. Selected business is highlighted; card touch target 136 reference units and all commands continue to use the original selected-instance callbacks.
- Guidance about full-time employment conflict comes strictly from `BusinessLaunchReadinessCode.EmploymentIncompatible`, not a duplicate rule in uGUI. Never resign, allocate time, pause or charge automatically.
- Unity PlayMode contract: direct first-card selection preserves revision, has 136-unit target and shows live business type/activity.
- Cards remain under the existing safe-area-aware `ScrollRect`; no hand-edited Unity scene/prefab YAML.

## Gate and limitations
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, ui, ui-ugui, scrollview-setup, vietnam-art-direction, localization, unity-ui-designer, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release.
- **Save impact:** none. No economics, receipts, migrations, or archived-v2 changes.
- **Tests:** exact-head engine-free CI and CodeQL required; Unity PlayMode not run (deferred).
- **Visual:** NOT RUN — four-card density, long names/diacritics and notched phone interaction require final Unity visual gate.
- **Known limitations:** founder guidance and business-name strings remain authored presentation copy pending M12-T03 localization tables. Full M12-T02 controls and real-device testing still open, milestone NOT CLOSED.
- **Files:** BusinessManagementOverlay.cs, FirstPlayableFlowTests.cs, this record.
