# M12-T03 — Vietnamese dashboard localization source and Unity Editor sync

**Status:** Development-first implementation. **Base:** `main` `c317247a4862ad4e15961e84484be6c6d761c24f`.

## Architecture
- Add a canonical immutable `DashboardLocalizationCatalog` containing 45 stable `dashboard.*` keys and Vietnamese source strings for five tabs, headings, buttons, controls, five onboarding topics and completion. Duplicated visible captions deliberately share keys; no gameplay IDs or money formulas in this catalog.
- Editor helper `DashboardLocalizationEditorSync` imports these into **existing** Unity Localization `FirstPlayableUI` Vietnamese String Table via UnityEditor.Localization, updating both table and shared ID asset atomically through Unity Editor APIs. No direct YAML asset modification.
- Editor `InitializeOnLoadMethod` schedules a safe one-time idempotent import after reload; it saves files only when actual table values/keys change. Also exposes manual **Tools → Startup Life → Localization → Sync Dashboard Vietnamese**.
- Existing `FirstPlayableShellBuilder` calls the same importer in all three rebuild paths; no second table or dynamic unversioned asset.
- Runtime UI routes all matched static `TextNode` and button captions, tab heading, and onboarding explanation body to existing `LocalizedKeyLabel` mechanism. Immediate Vietnamese fallback remains in C# for initial paint; Unity table takes authority after resolution.
- Editor menu **Validate Dashboard Vietnamese** checks every key/value and every visible codepoint against the licensed `NotoSansVietnamese.asset` TMP primary font. Never assert font coverage without running the Unity Editor command.
- Engine-free PresentationChecks now compiles the pure string catalog, testing unique keys, nonempty values, all five guide topic title/body translations and read-only source array.

## Evidence/gates
- Skills: startup-life-session-orchestrator, ui, ui-ugui, scrollview-setup, localization, vietnam-art-direction, unity-ui-designer, unity-localization, unity-game-director, unity-mcp-bridge, unity-qa-release.
- Source CI: exact-head Engine-free CI and CodeQL required, not yet recorded in this doc.
- Unity Editor compilation, actual serialized String Table import, TMP glyph test, PlayMode text expansion, small/notched device layout: **NOT RUN** — deferred combined end-of-MVP QA.
- Save impact: **None**, `PlayerPrefs` onboarding/settings v1 remain local-device and not part of V3 migration.
- Limitations: Dynamically formatted finance/business lines and numeric/guide step prefixes are **not fully localized** yet; only static captions/onboarding explanation are sourced from the table. Do not mark M12-T03 CLOSED or claim full glyph QA.
- Do not edit font binaries or distribute font files. NotoSans-Regular already exists in project with OFL license.
