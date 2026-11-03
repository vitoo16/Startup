# P04-CAREER-02 — Career Batch A authoring

**Date:** 2026-11-03. **Owner model:** GPT-6 Technical Director.
**Baseline:** `main` at `a37e1bff1d63b19dc27c40c548d34faa025a3d1f`.
**Status:** ENGINE-FREE AUTHORING IMPLEMENTED; Unity-generated content asset refresh and runtime/device QA deferred.

## Authority and scope

This slice follows `docs/GDD.md` sections 8–12 and the roadmap's P04
career batch A. It adds the first three non-Developer initial professions:
Marketing Executive, Sales Representative, and Office / Administrative Staff.

It deliberately does **not** change the Developer career definition, the
`first-playable.v1` content identity, the save schema, simulation algorithms,
business rules, or the frozen V2 historical resolver. New careers are additive
catalog content. The exact Developer scene rewards, schedule, rank IDs and
salary values remain unchanged.

## Authored content

All three careers use the existing deterministic 20-slot quota scheduler,
four scene beats per completed shift, Career XP of 10 per scene, and the
existing promotion evaluator. No new scheduler, minigame or career-specific
simulation fork was added.

| Career | Work schedule | Scene quota (in authored order) | Monthly salary by rank |
| --- | --- | --- | --- |
| Marketing | Mon–Fri, 09:00–17:00 | Campaign planning 25%; content review 20%; analytics 20%; team meeting 15%; client presentation 10%; partner communication 10% | 9M → 13.5M → 18M VND |
| Sales | Mon–Sat, 09:00–18:00 | Prospecting 20%; needs discovery 20%; product demo 15%; negotiation 20%; follow-up 15%; sales report 10% | 8M → 12M → 16M VND |
| Office/Admin | Mon–Fri, 08:30–17:30 | Document processing 25%; schedule coordination 20%; data review 20%; internal support 15%; record keeping 10%; team briefing 10% | 7M → 10.5M → 14M VND |

Rank thresholds are authored as:
- Junior: 0 XP / 0 service days.
- Mid: 1,000 Career XP / 30 calendar service days.
- Senior: 3,000 Career XP / 90 calendar service days.

Scene exposure maps only to the existing six transferable skills. Marketing
emphasizes Problem Solving, Communication, Leadership and Networking; Sales
emphasizes Networking, Communication and Negotiation; Office/Admin emphasizes
Time Management, Communication, Problem Solving and Leadership. Career XP
remains separate from transferable skill exposure.

**Balance caveat:** the new monthly salary ladders are provisional authored
content values anchored around the existing Developer salary scale. The GDD
does not lock exact salary bands. Sales has fixed salary only; no commission
system was invented. Final economy balance still needs the wider economy
review and owner approval.

## UI and localization

- Career choices remain data-driven from the active content catalog.
- The selected offer now shows its complete salary ladder, shift hours and
  actual working days (not just a count of days per week).
- Career titles and all 24 Developer/Batch A work-scene strings share
  `DashboardLocalizationCatalog` as their Vietnamese source of truth. The
  existing Unity StringTable sync writes both table and SharedTableData; no
  localization YAML or generated asset IDs were hand-edited.
- The Unity Editor menu `Startup Life/Content/Refresh career catalog and
  Vietnamese localization` refreshes the ScriptableObject and strings without
  running the test suites.
- Career and Course choice buttons continue to use the same reusable list
  renderer.

## Content asset generation

`FirstPlayableContentBuilder.CreateOrUpdate()` remains the single source-to-
ScriptableObject path. Its verification now correctly expects all **four**
current business definitions (fixing the stale two-business assertion) and
checks all four career schedules, scene quotas and salary ladders.

The committed `Assets/StartupLife/Data/FirstPlayableContent.asset` has **not**
been regenerated in this source-only session. It must be refreshed by the
existing Unity Editor authoring gate
`StartupLife.Editor.FirstPlayableRuntimeGate.BuildAndVerify` (also used by
`Test-UnityHeadless.ps1 -PrepareFirstPlayableScene`). That gate runs the
content builder and existing scene/localization repair before the Unity test
suites. Until that Editor step runs, do not claim the new professions are
present in the serialized runtime asset or in a device build.

## Save / historical compatibility

- No save fields or schema version changed.
- `first-playable.v1` remains the content version; existing Developer content
  and its revision remain unchanged.
- The V2 archive still contains only its frozen original Developer career.
  `tools/EconomicChecks/Program.cs` now explicitly filters the live additive
  career/business roster to the exact original roster before comparing its
  fingerprint to the frozen archive.
- No new career affinity, commission, employee, or business eligibility rule
  was introduced.

## Verification

**Engine-free:** added/updated content catalog checks for all four careers,
all skill references, exact work schedules, rank salaries, 20-slot
apportionment, 100 deterministic quota-deck cycles per career, no adjacent
repeat scenes, and same-seed determinism. Added source-join checks for
localization, UI and Unity asset builder contracts.

**Unity:** EditMode, PlayMode, Editor asset regeneration, visual layout and
device checks were not run in this session, in line with the owner's request
to finish code before the Unity test pass.

**Skills used:** startup-life-session-orchestrator,
startup-life-gameplay-guardian, localization, ui-ugui.

**Visual evidence:** none; runtime/UI validation deferred.
**Save impact:** no schema change; Developer and frozen V2 source retained.
**Known limitations:** Chef/Cook and Barista/F&B are Batch B; career-to-business
affinity and profession-specific animation/environment content remain future
work; salary values are provisional; Unity serialized asset refresh is
pending.

**Files changed:** `FirstPlayableContentTemplate.cs`,
`FirstPlayableContentBuilder.cs`, `FirstPlayableShellBuilder.cs`,
`LifeDashboardProjection.cs`, `LifeDashboardOverlay.cs`,
`ContentCatalogTests.cs`, `ContentCatalogChecks/Program.cs`,
`SimulationChecks/Program.cs`, `EconomicChecks/Program.cs`,
`test_career_content_batch_a.py`, `test_career_dashboard_wiring.py`,
`.github/workflows/engine-free-ci.yml`, this report.

**Commit:** see final `main` HEAD and Engine-free CI result.
