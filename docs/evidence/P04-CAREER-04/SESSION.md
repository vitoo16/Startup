# P04-CAREER-04 — Career transition and employment history dashboard

**Date:** 2026-11-03. **Owner:** GPT-6 Technical Director. **Lane:** owner-authorized `main`.
**Baseline:** `90e2eec8e5abfba251c08a77dc47d830a2b49759`.

## User-facing outcome

- The career dashboard now summarizes completed and active employment periods,
  distinct professions and *actual changes* between consecutive professions.
  Resigning and later rejoining the same profession counts as two periods,
  but not a profession change.
- The completed-job timeline displays newest entries first, showing existing
  career name (or revision-safe placeholder), rank, start/end dates, accrued
  career XP, calendar tenure, rank salary *only if the authored revision matches*,
  and outstanding salary claims for that particular employment.
- A selected catalog job gets a non-persistent, advisory explanation for:
  first employment; returning to a previously worked profession; switching
  profession after resignation; or already-employed status. The UI does
  not quietly resign, hire or reset the player when a selection changes.

## Implementation contracts

Authoritative `SimulationEngine.Accept` and `Resign` remain unchanged:
each new hire starts a new `EmploymentState` at entry rank/XP. Old
employment instances, claims, savings and transferable skills remain in the
saved state. The read model never merges employment XP across job periods or
projects current salary values onto an old career-definition revision.

`CareerTenureSnapshot.ElapsedCalendarDays` uses ISO dates and the same
elapsed-calendar-day basis as the existing promotion logic; it is nullable
when old metadata is incomplete, rather than inventing a date. No new
serialized fields, economy logic, balance parameters, business eligibility,
skill modifiers or UI persistence were introduced.

## Verification

- Deterministic simulation regression covers Developer -> Office -> Developer,
  separately accrued career XP, persistent outstanding salary claim, cash and
  transferable skills, completed-job chronology, counts, same-profession
  rehire, stale content revision and missing historical date.
- Engine-free Python guard checks Application projection / UI / command joins.
- Previously failed CI `90e2eec8`: the unrelated P04-COURSE-02 test fixture
  attempted to study after the unemployed day had advanced beyond sleep.
  Fixed the *test setup* by accepting employment before advancing to evening;
  did not change `SimulationEngine` time rules or weaken assertions.
- Final Engine-free CI verdict must come from the exact final commit; cancelled
  predecessor runs do not establish PASS.
- Unity Editor, PlayMode, device screenshots and Vietnamese TMP scale QA are
  intentionally **not run** in this source-only continuation.

## Closeout

- **Skills used:** startup-life-session-orchestrator,
  startup-life-gameplay-guardian, ui, ui-ugui, vietnam-art-direction
- **Tests:** code-added .NET `SimulationChecks`, Python source guard; final
  CI outcome to be reported against final HEAD
- **Visual evidence:** NONE (Unity/device intentionally deferred)
- **Save impact:** NONE (current/historical schema and receipts untouched)
- **Known limitations:** no actual Unity layout/device approval; old revisions
  without matching names show a neutral placeholder; transitions remain the
  existing two-step resign / accept flow, not an atomic job-switch command
- **Files changed:** `CareerBiographyProjection.cs`,
  `CareerSelectionPolicy.cs`, `LifeDashboardOverlay.cs`,
  `tools/SimulationChecks/Program.cs`,
  `tools/tests/test_career_dashboard_wiring.py`, this document
- **Commit:** final main HEAD and associated Engine-free CI run
