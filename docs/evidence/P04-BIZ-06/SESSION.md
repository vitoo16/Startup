# P04-BIZ-06 — V4 multi-business two-day cold replay regression

**Date:** 2026-11-14 (Asia/Ho_Chi_Minh)  
**Owner model:** GPT-6 Technical Director  
**Baseline:** `dee3d6e6934b227c18cc29ed8020f2358e24d70f` (`main`)  
**Status:** SOURCE COMMITTED, NOT COMPILED OR EXECUTED (CI zero-step failure)  
**Runtime activation:** `enableBusinessBatchAForNewRuns = false`; no Unity changes.

## Objective and implementation

Extended the existing engine-free `tools/EconomicChecks/Program.cs`
with `P04-BIZ-06 V4 three-business portfolio cold replay stays
idempotent over two dates`.

The regression uses the same V4 authority as production:
`EventV4CommitGateway`, `EventV4CoreCommandRunner`,
`EventV4CompatibilitySerializer`, `StagedEventV4SaveSerializer`,
and the versioned V3 source with a frozen V2 frontier. There is **no**
parallel economy, mutable UI forecast, or direct V3 save writer.

The test creates a character and launches three different side-hustle
businesses (Marketing Agency, Online Store and Home Food Preorder).
After each V4 core commit it instantiates a fresh gateway, reads the
checkpoint through the existing serializer, and checks the actual
journal and embedded V3 economy. It then checks:

1. Exactly three distinct launched business identities and four
   canonical initial core receipts.
2. One first-day fixed obligation per business, fully paid under
   this affordable test fixture, and exact capital/fixed-fee debit.
3. A single persisted market epoch with unique owner-minute
   assignments in the 18:00–22:00 window, never above 240 minutes.
4. Read-only V4 operations/business projections make no physical
   save change; an already-committed launch request does not recharge
   startup capital or obligations.
5. Five separately committed boundary commands with a fresh cold
   read after **every** command, crossing two midnight transitions.
   Obligations retain unique IDs, no overpayment, and realized
   fulfillments remain within the authored daily market pool.
6. Final date, revision and active business roster are unchanged by
   a cold duplicate of the second midnight command, including exact
   byte-for-byte save equality.

The existing `test_business_batch_a_content.py` source suite now
guards the presence of the complete V4 gateway/CAS/finite-market/time
replay checks without modifying the original V3 cases or validation
thresholds.

## Verification actually performed

- Inspected the current GitHub `main` HEAD and existing P04-BIZ-03
  V4 gateway regression before authoring.
- Checked the actual method signatures for
  `EventV4CommitGateway.ExecuteCore/Read`,
  `EventV4CoreCommandRunner`, `EventV4GameCommands` and the
  historical/live content serializer constructor.
- Source-level balanced delimiter check of the new C# test block:
  PASS. This is not a compiler.
- GitHub Actions Engine-free run `43601537978` at source guard
  commit `8c7a75e` concluded FAILURE with **zero executed
  steps**. Job-details explicitly report 0 steps. The earlier
  `43601489926` similarly failed. There are no execution logs
  proving a C# compilation or behavioral regression.
- Local execution was unavailable because this tool runtime has no
  .NET SDK and cannot fetch the repository from the public internet.

## Open gates / next work

- Investigate why GitHub Actions jobs fail before their first step
  (repository Actions controls, account/usage restrictions or runner
  availability). The exact root cause is **unconfirmed**.
- Run the real compiled .NET suite and fix any actual assertion
  failures without relaxing replay rules.
- Add economic settlement cases for Café and Fashion Store after
  their required startup capital is earned through legitimate
  commands; do not forge money inside historic V2 state.
- Unity Editor/PlayMode/portrait mobile verification remains deferred;
  production balancing and asset approval remain pending.

## Session closeout

- **Owner model:** GPT-6 Technical Director
- **Skills used:** repository `startup-life-session-orchestrator`,
  `startup-life-gameplay-guardian`; AI production economy/QA
  workflow requirements
- **Acceptance criteria:** real V4 gateway, 3-business owner-time
  bound, unique obligations, finite market, two-day cold replay,
  no double charge on command retries
- **Tests:** C# regression and Python source guard committed;
  syntax-level source check done; NOT EXECUTED against .NET/CI
- **Visual evidence:** none; no UI/asset work in this slice
- **Save impact:** NONE — no schema, frozen archive, catalog
  revision, economic rules or runtime toggle change
- **Known limitations:** 0-step Actions failure blocks CI; full-time
  Café/Fashion settlement and Unity/device QA remain open
- **Files changed:** `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`, this evidence record
- **Commit:** final main HEAD after this evidence record; exact-head
  run must be verified separately before marking any QA gate PASS
