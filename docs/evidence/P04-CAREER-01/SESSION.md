# P04-CAREER-01 — Catalog-driven Career Selection

**Date:** 2026-11-03. **Owner model:** GPT-6 Technical Director.
**Baseline:** `6ded0a394e9417230161b1f2ea7fe6f5d3a36e0c`.
**Lane:** owner-authorized `main`. **Status:** IMPLEMENTED_ENGINE_FREE; Unity/device QA DEFERRED by owner.

## Problem and scope

`LifeDashboardOverlay` showed one hardcoded **Nhận việc lập trình** button
using `host.AcceptDeveloper`, though the authoritative
`FirstPlayableFlow.AcceptJob(careerId)` and `SimulationEngine.Accept`
already accept any authored catalog career. A future career could have correct
content but no working dashboard action. Current committed first-playable
catalog contains **developer only**; this slice does **not** invent a second
shipping career or revise an archived catalog fingerprint.

## Implementation

- `LifeDashboardProjection` exposes detached `CareerDashboardItem`
  objects: career ID/name key, first-rank monthly salary, start/end shift
  minute, and workday list. Ordered by stable career ID.
- `CareerSelectionPolicy` selects only IDs in the projected catalog; on
  restore the committed employment takes priority, otherwise the prior
  temporary UI selection survives refresh. It mirrors the simulation
  character/active-employment conditions as read-only guidance.
- `LifeDashboardOverlay` provides scrollable catalog choice buttons,
  selected offer salary and work-schedule summary, and disabled action state
  while employed. The new **Nhận việc** action submits the chosen career ID.
  Career and Course selectors share the same cached
  `SyncSelectionOptions<T>` UI builder; no duplicate Unity button factory,
  repeated allocation on refresh, or second set of style tokens.
- `LifeScreenViewController.AcceptCareer` validates ID against the
  *current* content catalog then delegates to the **existing**
  `flow.AcceptJob(careerId)` command. The old
  `AcceptDeveloper()` is kept for serialized UnityEvent bindings; no
  prefab/scene rewrite.
- Engine-free `SimulationChecks` includes a **synthetic two-career fixture**
  and tests catalog order, distinct salary/schedule, no character, selected
  job, actual hire, no extra fee, double-hire rejection without save mutation,
  resignation and a subsequent hire. No synthetic career is shipped.
- `test_career_dashboard_wiring.py` validates core↔read-model↔UI↔command
  joins; `test_course_dashboard_wiring.py` stays valid after the shared
  choice-list refactor.

## Acceptance and known limitations

The game model remains authoritative; job selection and display never
create an EmploymentState, Career XP, wage, salary claim, business adjustment,
new save field or PlayerPrefs entry. No content IDs or money balances are
hardcoded in the new action.

**Important product limitation:** only **developer** exists in the currently
authored first-playable catalog. Multiple real selectable careers need
production-grade authored careers, scene percentages, rank schedules,
localized names, historical archive compatibility and separate gameplay QA
before they can ship. Do not treat the synthetic fixture as production
content. Other older first-playable scene buttons still point to developer
for backward compatibility.

**No Unity test requirement in this slice by explicit owner request.**
Mobile 9:16 layout, 200% text, localized names for future new careers and
Unity EditMode/PlayMode need later runtime/device evaluation. The CI result
is not a Unity compile or screenshot approval.

**Skills used:** startup-life-session-orchestrator,
startup-life-gameplay-guardian, ui-ugui (including scrollview-setup),
vietnam-art-direction, accessibility (touch target/focus reference).
**Tests:** Engine-free CI on final HEAD: see linked GitHub Actions run.
**Visual evidence:** NONE, Unity deferred.
**Save impact:** NONE. No save schema/catalog mutation.
**Known limitations:** runtime catalog one career; Unity/visual/device
not yet tested; future career-name localization needed.
**Files changed:** `LifeDashboardProjection.cs`,
`CareerSelectionPolicy.cs[.meta]`,
`LifeDashboardOverlay.cs`, `LifeScreenViewController.cs`,
`tools/SimulationChecks/Program.cs`,
`tools/tests/{test_career_dashboard_wiring,test_course_dashboard_wiring}.py`,
`.github/workflows/engine-free-ci.yml`, this report.
**Commit:** see final main HEAD.
