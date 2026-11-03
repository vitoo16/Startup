# P04-COURSE-02 — Study and career promotion progress read models

**Date:** 2026-11-03 · **Owner:** GPT-6 Technical Director · **Lane:** owner-authorized `main`.

## Scope and authoritative behavior

- The active course dashboard derives `StudyMinutesRemaining` from the
  existing authoritative `ActiveCourseSnapshot.TargetUnits`,
  `ProgressUnits`, and the committed character's `LearningSpeed`.
  It applies the same ceiling arithmetic as `SimulationEngine.Study`.
- `Minimum60MinuteSessions` is a lower-bound count, **not** a guaranteed
  number of evenings: free minutes depend on the actual shift/calendar and
  the simulator clamps each study command.
- `CareerPromotionDashboardItem` reads the **next** authored rank's XP,
  calendar-tenure and monthly salary thresholds, comparing them with the
  currently committed job's XP and `StartedIso`.
  It guards career-definition revision mismatch, missing employment and
  highest-rank cases; it never predicts a promotion command or changes salary.
- The Life dashboard displays progress as Vietnamese strings with explicit
  “ngày lịch” and “tối thiểu” units. No new balance values, local career
  switches or duplicate simulation engine are introduced.

## Verification

- Added deterministic checks to `tools/SimulationChecks/Program.cs` for
  initial study duration at a non-default learning speed, cold restored progress,
  non-divisible remaining units, unavailable invalid-speed hints, separate
  promotion XP/tenure thresholds, threshold achievement, highest rank,
  stale career revision, unemployment and unchanged checkpoint.
- Added source join guard in `tools/tests/test_course_dashboard_wiring.py`.
- Engine-free CI must report its own verdict on the **final commit SHA**;
  earlier checks and cancelled runs are not accepted as PASS.
- Unity Editor/PlayMode and portrait device visual QA are explicitly **not
  run** in this source-only session.

## Closeout

- **Owner model:** GPT-6 Technical Director
- **Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian,
  ui, ui-ugui, vietnam-art-direction, localization
- **Acceptance criteria:** exact learning-speed arithmetic, no persisted
  projection state, non-misleading remaining duration and promotion hints,
  old Developer/save mechanics untouched
- **Tests:** newly authored .NET and Python regression tests; final CI result
  pending until a GitHub Actions run completes
- **Visual evidence:** NONE — Unity/device test deferred
- **Save impact:** NONE — no schema, receipt, catalog version or archived replay edits
- **Known limitations:** study sessions are minimal counts, not guaranteed
  evenings; the career promotion hint is informational until a qualifying
  simulation boundary advances; UI localization/200% scale need Unity QA
- **Files changed:**
  `Assets/StartupLife/Scripts/Application/LifeDashboardProjection.cs`,
  `Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs`,
  `tools/SimulationChecks/Program.cs`,
  `tools/tests/test_course_dashboard_wiring.py`, this report
- **Commits:** see GitHub main history and exact-head CI
