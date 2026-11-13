# P04-BIZ-04 — Multi-business owner-time and multi-day V3 replay regression

**Date:** 2026-11-13 (Asia/Ho_Chi_Minh)  
**Owner model:** GPT-6 Technical Director  
**Baseline:** `eb5463fa673c17b186580517af4bafdcea6b869c` (`main`)  
**Status:** Regression committed; exact-head CI BLOCKED (GitHub Actions jobs terminate without steps); Unity/device NOT RUN

## What changed

Added one real `GameSession` V3/CAS integration regression to
`tools/EconomicChecks/Program.cs`, rather than a second business simulator.
The test constructs the version-isolated seven-business content and economic
catalog, migrates an unmodified baseline `first-playable.v1` revision-zero
checkpoint through the bundled historical resolver, then creates a character
and launches **Marketing Agency, Online Store and Home Food Preorder**.

The combined owner requirement is 180 + 120 + 120 = 420 minutes, but the three
businesses share only the 18:00–22:00 window (240 minutes). The test asserts
that frozen future owner-minute assignments are unique, stay inside that
window and do not exceed 240 minutes; it **does not** assert full operation
of all businesses when there is inadequate owner time. Ineligible/partially
served businesses remain governed by the existing allocator.

The same test verifies:
- all three businesses are present in the actual persisted, validated
  portfolio and have exactly one day-opening contractual obligation each;
- startup capital and contractual fixed invoices are debited once,
  without crediting uncommitted future sales;
- full funding of opening invoices when the fixture wallet can afford them;
- read-only business/finance projections never rewrite the save;
- five authoritative `AdvanceBoundary` commands across two calendar
  midnights, each with an independently verified cold replay;
- obligation identities are not duplicated and amount-paid never exceeds
  amount-due; sold market units never exceed authored finite daily pool
  supply; final business roster persists after both dates;
- reopening `GameSession` through the existing serializer/store and
  retrying the final midnight command ID yields `AlreadyCommitted`
  without changing any physical save bytes.

Added a Python source contract in
`tools/tests/test_business_batch_a_content.py` that guards this test
against removing the authoritative V3 CAS, time-overlap, invoice,
finite-market, cold-replay and idempotent-retry assertions.

## Compatibility / production constraints

- `enableBusinessBatchAForNewRuns` remains **false by default**.
- No new business IDs, ruleset revisions, save schema, payout formula,
  receipt shape, economic engine, scene or runtime asset was added.
- The original four-business catalog and historical archive are unchanged.
- **Still missing:** separate live V4 multi-business/day tests and
  production economic balance evidence for full-time Café/Fashion Store,
  Unity asset generation, visual/device QA, and approval of tuning.

## Verification / current blocker

- **Previous baseline:** exact `eb5463f` Engine-free CI PASS:
  GitHub Actions run `41715997287`.
- **This slice:** GitHub Actions run `43440506841` for
  `e018c788` concludes FAILURE but its job reports **zero executed steps**,
  with no downloadable test/build logs. The preceding slice run
  `43440085014` and CodeQL jobs similarly report zero steps.
  This is **not** a C# test result; an Actions service/account/runner
  execution problem is suspected, but the cause has not been verified.
  The owner should inspect GitHub Actions run notices, runner availability
  and billing/usage limits. Do **not** mark CI PASS.
- **Static inspection:** source signatures for V3 financial provenance,
  frozen epoch assignments, saved fulfillment roster, `GameSession`,
  and the reused command/store path were checked through GitHub.
  The new C# test block's balanced delimiters were checked. Neither is
  a substitute for .NET compilation or engine-free execution.
- **Unity visual/device:** NOT RUN; owner-requested deferment remains.

## Session closeout

- **Skills used:** startup-life-session-orchestrator,
  startup-life-gameplay-guardian (repository-specific)
- **Acceptance criteria:** no double booking of overlapping owner time;
  no duplicated contractual invoices or over-sold market supply;
  no silent save writes, exact multi-date cold replay and idempotent retry
- **Tests:** deterministic C# and Python regression authored; no
  executed new SHA CI proof because jobs did not launch
- **Visual evidence:** none — not a visual change; Unity/device still deferred
- **Save impact:** none, no schema/archived source changes
- **Known limitations:** new regression unverified by CI; replay V4 and
  FullTimeRequired business balance still need further integration coverage
- **Files changed:** `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`, this report
- **Commit:** use the final `main` HEAD and its exact CI run
