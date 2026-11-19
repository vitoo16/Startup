# P05-UNITY-01 — Local Unity PlayMode failure triage and safety fixes

**Date:** 2026-11-20 (Asia/Ho_Chi_Minh)  
**Owner model:** GPT-6 Technical Director  
**Baseline remote main:** `cd613f624604a6670915162eabf97255f779307f`  
**Status:** FIXES COMMITTED / UNITY PLAYMODE AND LOCAL CI NOT RERUN  
**Release:** BLOCKED — do not mark Unity or Startup Release PASS.

## Evidence boundary

Owner reported Local CI 2026-11-19: Quick 45/45, Full 69/69,
EditMode 48/48, PlayMode 7/24 and Unity gate 69/70, at local SHA
`42f59ef`. The SHA is **not available on GitHub** as of intake;
the remote `ci/local-first-20261118` and `main` both pointed to
`cd613f6`. No original local NUnit XML, Unity Editor log or captured
screen files were available through the connected repository tools.
Thus failure-group counts are the owner's report, not reproduced test
failures on the current remote checkout. No merge was performed.

## Implemented on remote main

1. **UI localization contract:** the Finance PlayMode test checks the
   already-authored Vietnamese label `Nợ cần trả`, instead of the
   obsolete `Tổng nợ`. No production UI copy or debt semantics changed.
2. **Intentional SaveBoot diagnostics:** added a shared test-only
   `SaveBootLogExpectations` helper. Lifecycle and flow tests explicitly
   register one strictly formatted `[StartupLife][SaveBoot]` informational
   record before each successful scene load. The unreadable-primary
   recovery test expects the specific `Unreadable` status record.
   Unexpected warnings/errors/exceptions are **not** disabled; there is
   no global `ignoreFailingMessages`.
3. **Both invalid checkpoints:** the authoritative
   `VerifiedBusinessContentSelector` now throws the
   `UnverifiedBusinessContentException` subtype (still compatible with
   `InvalidOperationException` catches), and Unity bootstrap handles
   this expected fail-closed recovery condition via
   `reason.save.unrecoverable`. It preserves both invalid source
   files and does not misclassify them as a generic bootstrap bug.
   Existing C# and Python source guards now demand this exact type.
4. **Event V4 day progression:** an Editor-only
   `PlayModeEventAdvanceHarness` handles a real
   `PlayerChoice` stop. It requires a visible V4 modal, picks a legal
   interactable authored choice, commits through the existing UI,
   then resumes the original day batch through the real Flow/button.
   Both lifecycle day tests and the end-to-end flow test reuse it.
   `automaticDailyChecks: true` remains the default; the simulation
   and event journal were **not** changed for test convenience.
5. **Responsive evidence:** local Unity runner now clears stale
   captures, exports creator and main-menu screenshot directories,
   runs the existing graphical Unity test suite, and verifies both
   native-resolution PNG manifests. A source-only preflight guards
   fail-closed recovery, event resumption and strict resolution checks.
   Requested `360x640`, `390x844` and `430x932` must still be
   accepted by Unity; unsupported Editor resolution is a real red QA
   gate, **not** a skip, fake resize or PASS.

## Verification and remaining blockers

- Source was re-read after every optimistic GitHub contents write.
  The new helper and modified C# sources passed simple lexical
  delimiter/source-join checks. This is **not** a C# compiler.
- No .NET SDK or Unity Editor is accessible in this assistant runtime.
  The owner's Windows Local CI and PlayMode 24-test suite were
  **not rerun** on these commits. Quick/Full status from the owner's
  previous local SHA must not be reused for the new remote HEAD.
- The responsive tests may still FAIL when Unity Editor refuses
  `Screen.SetResolution`. Use a graphics-capable runner or a
  supported player/device route, verify both PNG reports and conduct
  separate visual review.
- Remaining gameplay/lifecycle failures require the full
  failing NUnit XML and Editor log to map exact test cases,
  stack traces, pending-event tickets, and checkpoint revisions.
  The new helper is a principled candidate fix, not proof of PASS.
- Re-run **local Quick -> Full -> Unity -> Release** on one integrated
  checkout. Do not merge unrelated Local CI history blindly;
  if local `42f59ef` has further commits, compare source/diffs first.
- No changes to save schema, frozen V2/V3/V4 receipts, engine economy
  formulas, player-facing save content, or art were introduced.

## Session closeout

- **Skills used:** `startup-life-session-orchestrator`,
  `startup-life-gameplay-guardian`, `unity-cli`,
  `ui-ugui`, `localization` (repository copies).
- **Acceptance criteria:** correct localization assertion; expected
  SaveBoot logs consumed precisely; invalid saves fail closed with
  localized recovery; V4 pending choices resumed without bypass;
  native screenshot evidence required; no regression on save/rules.
- **Tests:** targeted static source checks only;
  `test_unity_playmode_recovery_wiring.py` committed and wired as
  a local Unity preflight, **not executed in this session**.
- **Visual evidence:** none (no local Unity runner or device access).
- **Save impact:** none; typed selector failure and localized handling
  only, no persisted state, archive, command or economic mutation.
- **Known limitations:** 17 reported PlayMode failures are not
  independently rechecked; 2 responsive cases require a functional
  graphical resolution; 5 lifecycle symptoms still need XML evidence.
- **Files changed:** `FirstPlayableFlowTests.cs`,
  `FirstPlayableLifecycleTests.cs`,
  `SaveBootLogExpectations.cs` + `.meta`,
  `PlayModeEventAdvanceHarness.cs` + `.meta`,
  `VerifiedBusinessContentSelector.cs`,
  `StartupLifeBootstrapper.cs`,
  `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`,
  `tools/tests/test_unity_playmode_recovery_wiring.py`,
  `ci/Run-UnityLocal.ps1`, this evidence record.
- **Commit:** final main SHA to be verified after this document.
