# M12-T03 — Profile accessibility and contextual onboarding (development slice)

**Owner:** GPT-6 Technical Director. **Base** main `f22d5eac3a67847fdab492d099bd33a3f61c17fd`.

## Implemented
- Keep the existing five-tab safe-area uGUI UI. Inside Profile, add real font-size controls (90% to 120%, ten-point steps) applying to **all dashboard TMP text nodes**, not just a fake settings switch; persist per device with `PlayerPrefs`.
- Versioned, immutable onboarding topic contract `OnboardingGuideCatalog`: free skill practice; paid study and prerequisites; full-time employment vs kiosk compatibility; automatic multi-business owner-time allocation; resignation **does not** automatically force founding.
- Onboarding first-use progress uses `PlayerPrefs` completion status keyed by stable `GameSnapshot.RunId` and guide Version, with previous/next/finish/replay controls. Reading or dismissing this device UI guide never writes gameplay save or submits commands.
- Normal Career/Skills/Finance/Business/Profile actions remain delegated to the original GameSession flow; existing source read model and economics untouched.
- Engine-free regression checks five ordered unique topic IDs; Unity PlayMode test added for guide progression, font-size behavior, per-run persistence, replay, identical gameplay revision/cash. PlayMode tests restore pre-existing PlayerPrefs to prevent contaminating other runs.

## Compliance / deferred gates
- **Skills:** startup-life-session-orchestrator, startup-life-gameplay-guardian, ui, ui-ugui, scrollview-setup, localization, vietnam-art-direction, unity-ui-designer, unity-localization, unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release.
- **Save impact:** No gameplay save schema changes; new preferences currently live in Unity device PlayerPrefs, **not in the V3 save file**. This is a conscious interim scope to preserve strict historical replay, NOT compliance with final M12-T03 cross-device onboarding/settings migration requirement.
- **Visual evidence:** NOT RUN. Font coverage, 90%/120% wrapping, notched phone, screen-reader and long-name inspection deferred to Unity device QA.
- **Localization:** Five guide topic IDs are stable. Vietnamese strings are a temporary source-verified fallback in Presentation; must be migrated into Unity `FirstPlayableUI` String Table with exact per-claim variable/formats, proper diacritic glyph test. No raw `.asset`/scene YAML was edited without Unity Editor.
- **Regression:** Engine-free and CodeQL exact-head gates required, PlayMode authored but **NOT RUN**.
- **M12-T03:** PARTIAL; do not mark CLOSED or MVP complete.

**Changed files:** OnboardingGuideCatalog.cs and .meta; LifeDashboardOverlay.cs; tools/SimulationChecks/Program.cs; PlayMode/FirstPlayableFlowTests.cs; session evidence.
