# P04-BIZ-07 — Salary-funded Café and Fashion Store V3 settlement

**Date:** 2026-11-17 (Asia/Ho_Chi_Minh)  
**Owner model:** GPT-6 Technical Director  
**Baseline:** `1fa515e5c1f1f93af82493a499dfc3bc1b4a6c2b` (`main`)  
**Status:** Source regression committed, **NOT COMPILED / NOT EXECUTED**; Engine-free CI blocked before first step.

## Intent and boundaries

The opt-in Business Batch A definitions for Small Café and Fashion Store
require 6,000,000 VND and 4,000,000 VND respectively, beyond the
3,000,000 VND original character start (and first-day living charge).
Do **not** forge cash, edit a persisted save payload, alter archived V2
content, add a test-only rich background to production content, or
weaken `SimulationEngine.LaunchBusiness`.

This slice exercises the actual `GameSession` career → salary → founder
transition and source-verified V3 economic settlement. The content
track remains `first-playable.biz-a.v1` opt-in; existing baseline
and economic rules are untouched.

## Regression authored

Added `P04-BIZ-07 salary-funded café and fashion V3 settlement uses
genuine commands` in `tools/EconomicChecks/Program.cs`.

1. Create a genuine V2 starting state through
   `FirstPlayableContentTemplate.BuildCatalog()`, migrate it through the
   bundled historical V2 resolver and use the **existing** V3
   `GameSession`, `AtomicFileSaveStore` and verified serializer.
2. Create a character, accept the Developer career, and advance all of
   October 2026 using `GameSession.AdvanceMonth`. That command must
   commit the real shift scenes and the November 1 salary settlement.
   Verify both `salary.accrual` and `salary.payment` ledger records
   and an adequately funded cash balance; never set `Cash` or fabricate
   salary claims in memory.
3. Resign via `CommandKind.Resign`, cross Nov 1 and Nov 2 using
   `AdvanceDay`, and advance to the authored wake time Tuesday Nov 3.
   Verify the **actual** checkpoint is unemployed, has no arrears,
   and contains sufficient received salary.
4. Copy this fully verified checkpoint into **two independent test
   branches**. In one, launch Café; in the other, Fashion Store.
   These are alternative deterministic test scenarios, not two
   simultaneous full-time businesses in the same player history.
5. Verify the new business and its exact authored minimum capital,
   one paid mandatory day-opening invoice, and exact wallet debit.
   Café's 110,000 VND assistant wage must remain included within the
   single 250,000 VND fixed invoice (no separate duplicate wage).
6. Verify frozen future owner minutes stay unique, within each
   authored operating window, and never exceed required time.
7. Advance the actual founder to sleep then midnight using committed
   `AdvanceBoundary` commands. Reconcile the genuine
   `V3BusinessDaySettlement` with fulfillment unit prices/variable
   expenses, one recognized fixed invoice and the market pool cap.
8. Cold-restore each branch and retry the same midnight command:
   `AlreadyCommitted` must be returned without changing saved bytes.

The existing Python
`tools/tests/test_business_batch_a_content.py` now contains
`test_salary_backed_full_time_settlement_contract` to protect all
major source joins against accidental deletion.

## Verification performed (do not extrapolate)

- Independently compared the new test's referenced catalog constants,
  `GameState.Ledger`, V3 settlement fields, income categories and
  business economic profile API with repository source.
- Tested that the inserted C# test block has balanced delimiters and
  that the Python guard's critical source fragments are present.
  These are **static reviews only**.
- No C# compilation or C# / Python regression execution was performed.
  A local .NET SDK was unavailable and GitHub Actions jobs still
  report **zero executed steps**. The exact-head run must be inspected
  before claiming any green status. The account/runner/usage cause
  remains unknown; Actions permissions/runners API access was
  unavailable to this session.
- No Unity Editor, PlayMode or device/visual QA ran.

## Remaining work

1. Restore an actual .NET 8 runner or local SDK and run:
   `dotnet run --project tools/ContentCatalogChecks/ContentCatalogChecks.csproj -c Release`
   and `dotnet run --project tools/EconomicChecks/EconomicChecks.csproj -c Release`.
2. Verify and debug C# diagnostics/settlement outcomes without
   changing economic laws to force tests to pass.
3. Profile execution cost of a one-month salary-earning regression;
   keep deterministic scenario evidence but optimize fixtures only
   if CI runtime warrants it.
4. Follow with V4 café/fashion settlement and production balance
   viability across funded, starved and interrupted days.
5. Keep Batch A disabled by default and require Unity/device gate
   before production activation.

## Closeout

- **Skills used:** repo `startup-life-session-orchestrator`,
  `startup-life-gameplay-guardian`; GDD and production economy/QA workflow.
- **Acceptance criteria:** authentic salary-funded founder capital,
  no invented money, paid fixed obligations once, nonoverlapping owner
  time, finite market, exact V3 cold replay and idempotent midnight retry.
- **Tests:** one new C# source regression and one Python source guard;
  static inspection only; execution blocked.
- **Visual evidence:** none (no UI or art changes).
- **Save impact:** none; no schema, archive, receipt semantics or live
  bootstrap switch changes.
- **Known limitations:** zero-step CI failure, no .NET compiler,
  potential integration/runtime assertions unverified.
- **Files changed:** `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`, this document.
- **Commit:** final `main` HEAD (check exact SHA and Actions run).
