# P04-BIZ-05 — Café/Fashion owner schedule integration contract

**Date:** 2026-11-14 (Asia/Ho_Chi_Minh)  
**Owner model:** GPT-6 Technical Director  
**Baseline:** `a3684e0e400a97fbff6652d3e590faa2cfea7aa2` (`main`)  
**Status:** Regression SOURCE COMMITTED; CI/runtime verification BLOCKED

## Scope / actual changes

The seven-business opt-in catalog already contains provisional Small Café,
Fashion Store and Marketing Agency definitions. This slice extends the
existing engine-free `ContentCatalogChecks` with a deterministic
`BusinessBatchAOwnerCombinations` test of the *existing* canonical
`OwnerDayAllocator.Plan`; it does not create a separate time simulation
or change any locked gameplay rule.

Cases (Wednesday 2026-11-04; all business timestamps refer to the same
authored business definitions):

- **Small Café + Agency**: one full-time café operation needs 480 minutes
  inside 09:00–18:00, with Agency needing another 180 minutes between
  18:00–22:00. Expect both `FullyAllocated` without overlapping minutes.
- **Fashion Store + Agency**: full-time fashion operation needs 420 minutes
  inside 10:00–19:00, and the evening Agency needs 180 minutes. Both can
  be fully scheduled with no overlapping owner minutes.
- **Small Café + Fashion Store**: both `FullTimeRequired` at once must
  yield `MultipleFullTimeUnsupported`, with **zero speculative
  owner-minute allocations**. Do not silently invent an unimplemented
  manager/employee capability.
- Reversing authored portfolio order cannot change canonical allocation.
- An employed Developer cannot personally operate either full-time
  business; evaluator must report `EmploymentIncompatible` for both.

The test uses a read-only, receipt-free midnight planning fixture (not a
persisted or fabricated save). No wages, revenue, fixed expenses, or
receipt changes are generated. `P04-BIZ-04` separately holds the genuine
V3 multi-business GameSession/CAS replay regression.

An additional Python source guard checks that these cases remain wired
to the unchanged canonical owner allocator.

## Verification and blocked gate

Previous stable baseline `eb5463f` has verified Engine-free CI PASS
(run `41715997287`). The newly committed P04-BIZ-04 and P04-BIZ-05
tests do **not** inherit that PASS.

At this session the exact new-head GitHub Actions jobs, including
`43599681850`, ended in FAILURE **before executing any steps**.
The job-details API reported an empty step list, and the earlier
new-head runs behaved the same. Hence no C# compile result, no
executed simulation test, and no Python CI result are available.
Cause (GitHub Actions account/billing/runner/incident) is **not
verified**. Neither a source inspection nor a zero-step failure
proves a gameplay regression.

When a .NET 8-capable environment is available, run from repo root:

```bash
dotnet run --project tools/ContentCatalogChecks/ContentCatalogChecks.csproj --configuration Release
dotnet run --project tools/EconomicChecks/EconomicChecks.csproj --configuration Release
python3 -m unittest discover -s tools/tests -p 'test_business_batch_a_content.py' -v
```

Then require exact-HEAD CI PASS, and investigate any test or compile
failures without loosening either core planner or replay validation.

## Session closeout

- **Skills used:** `startup-life-session-orchestrator`,
  `startup-life-gameplay-guardian` (repository skills);
  economy/QA workflow contract read in `AI_PRODUCTION_WORKFLOW.md`
- **Acceptance criteria:** correct full-time/side-hustle pairings,
  rejection of two full-time owner operations, nonoverlap, order
  determinism, employment incompatibility; unchanged old saves
- **Tests:** C# `ContentCatalogChecks` case and Python source guard
  authored, NOT EXECUTED (Actions jobs started zero steps; local
  environment has no .NET SDK)
- **Visual evidence:** none; no UI/scene edits; Unity/device QA deferred
- **Save impact:** NONE — no schema, serialized catalog, archived
  source, command, or economic rules change
- **Known limitations:** new V3 multi-day and owner-plan regressions
  uncompiled/unexecuted; V4 multi-business and balance tests still open;
  GitHub Actions infrastructure issue unresolved
- **Files changed:** `tools/ContentCatalogChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`, this file
- **Commit:** current `main` HEAD after this evidence file
