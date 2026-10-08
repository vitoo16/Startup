# M9-T01 — Owner-time allocator implementation handoff

Date: 2026-10-08 (+07:00)

Status: **CORRECTION DRAFT — Astra source re-audit and final exact-head CI required. NOT CLOSED.**

Starting main: `05b9776ad515ed8a169decdf3f2b1a6fd42aab7d`

Branch: `feat/m9-t01-owner-time-allocator`

PR: [#33](https://github.com/vitoo16/Startup/pull/33) (draft, not merged).

Approved architecture: `M9-T01 ARCHITECTURE APPROVED — SOL 6.1 MAY IMPLEMENT`.

## Scope implemented

- `Core/OwnerTime.cs`: immutable half-open minute intervals, owner allocations, individual normalized capacities, typed statuses and revision/date/content-tagged `OwnerDayAllocation`.
- `Simulation/CommittedTimeTrace.cs`: read-only projection of committed study, employment and elapsed time from canonical receipts and saved employment history. Past time is never allocated retroactively.
- `Simulation/OwnerDayAllocator.cs`: single-day nonsettling preview, active business exact revision resolution, contiguous full-time block with priority, per-minute canonical feasible progressive equal sharing for side businesses, whole-day time classification.
- `Application/GameSession.cs`: `IOwnerDayAllocationReadModel` within the existing session lock.
- `tools/BusinessOwnershipChecks`: deterministic regression for 1–4 businesses, unequal operating windows, full-time block, closures, study receipts, unsupported content, exact capacity arithmetic, immutable contracts and read-model purity.

Algorithm: canonical ordinal DefinitionId/InstanceId ordering; minute-slot augmenting paths preserve existing quotas while adding at most one minute per successful iteration. Therefore each assigned minute has one owner and iterations terminate under finite remaining minute slots and per-business requirement caps.

## Frozen boundaries

- M8-T02 `BusinessEligibilityEvaluator` whole-date semantics remain unchanged.
- Side-business partial capacity is permitted; FullTimeRequired requires an all-or-nothing contiguous block.
- Current and historical committed study/work/elapsed time cannot be used as remaining owner time.
- Unavailable exact business/career revision or content version returns `UnsupportedContent` with **no advertised free/allocated/capacity intervals**. Unknown occupancy is never mistaken for time available. This is a read-model failure status, not a claim of verified elapsed/employment/study classification.
- Read-only previews do not increment revision, allocate entity/operation IDs, mutate RNG, save, add receipts, touch cash or settle a business day.
- No `BusinessDayPlan`, pricing/customer demand, operating costs/revenue, M9-T02 settlement or M10 kiosk helper.
- SaveVersion remains **2**, no new persisted fields or migration. No frozen M1 public API changes.
- No scenes, prefabs, serialized assets, fonts or ProjectSettings changed.

## Tests and evidence boundary

- Initial PR CI #133: failed **static Unity asset hygiene** because newly authored C# files lacked Unity `.meta` sidecars. The three sidecars were added.
- CI #134: source compiled; 10 newly authored tests failed because *their synthetic fixture cursors had no matching receipts*. The production occupancy projector correctly rejected those invalid fixtures.
- Synthetic fixture receipt provenance was corrected without weakening the projector.
- CI #135: **SUCCESS** after synthetic fixture repair; retained as historical recovery, not proof for subsequent heads.
- **Astra audited baseline:** exact head `a2b06ac5aa3b2ae5a9964362498c3d1adb670b5f`; Engine-free CI **#140**, run **37797006688**, completed **SUCCESS**. This verifies the **pre-correction** head only.
- Astra identified **M9-S01 / M9-T01 / M9-T02** (three MEDIUM) and **M9-D01** (LOW). Corrective source scope:
  - M9-S01: unsupported planning returns empty/unknown classifications instead of synthetic free intervals; regression covers changed business revision, content-version mismatch and unavailable historical career revision at a nonzero cutoff.
  - M9-T01: remove synthetic 1,080-minute `AdvanceBoundary` + unpurchased `Study` receipt. Create genuine career, business and purchased-course state; commit wake, shift and evening boundaries through `GameSession`, commit 180 minutes of Study and compare the *complete* allocation snapshot after cold restore.
  - M9-T02: pin 241-minute three-way remainder (81/80/80), cap redistribution (30/120), three/four-business reorder permutations and complete-output cold-restore byte determinism. Normalization now covers date/revision/content version/cutoff, all four interval classifications, allocation revisions/intervals and required/allocated/permyriad capacity.
- Correction commits: `1e01d0a8836d241edb1c9dcc6d9da67a29ee86ee` (fail-closed), `79af785f0ec28a08fde1afbb2f5de3110f2700ab` (provenance/determinism regressions). **Neither is a final approved SHA.**
- After this documentation commit, the final exact-head GitHub Actions check must be associated with the **new branch HEAD**. Record its run ID, SHA and conclusion in the PR review/evidence comment, not by presuming an earlier intermediate run covers the final tree. Retain #133/#134/#135/#140 as chronology; failed attempts were not erased.
- Actual Unity Editor `6000.3.25f1 (e1dba0a9aba4)` compile, EditMode and PlayMode: **NOT RUN** in this session. No Unity connection is available here.
- Visual evidence: **N/A**. No visible UI or art changed.

## Reproducibility gates for later Unity acceptance

- Hydrate and verify both Noto and Liberation Git LFS source fonts before fresh import.
- Do not set `STARTUP_LIFE_M7_EVIDENCE` when running the canonical `-nographics` suite.
- Run exact-head Unity fresh import and EditMode/PlayMode only after Astra source approval.
- Preserve TMP/content asset hash and clean post-run Git state.

## Session closeout

- Owner: GPT-6 implementing the Astra-approved M9-T01 architecture, with Sol 6.1/Astra review gates still pending.
- Skills read: repo `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, repo `unity-cli`; pinned `unity-game-director`, `unity-gameplay-systems`, `unity-game-economy`, `unity-mcp-bridge`, `unity-qa-release` source definitions at `tea-x-random/unity-game-skills@dafb97ef00f94e64e42e6260bc6b3af74cc83dad`. Remote skill source read is not a claim that corresponding local CLI tools are installed.
- Tests: CI #140 PASS on audited pre-correction SHA; **corrected final head CI pending verification**, then Astra source re-audit. Do not transfer CI success from one SHA to another.
- Visual evidence: N/A.
- Save impact: SaveVersion 2 unchanged; no fields/migration.
- Known limitations: Astra has **not yet approved corrected source**; Unity not executed; no M9-T02 financial settlement; available future capacity is a preview, not reserved.
- Files changed: Core owner-time contracts and .meta, Simulation projection/allocator and .meta, GameSession read model, ownership test harness, this evidence note.
- Merge: **NOT AUTHORIZED**.

`M9-T01 CORRECTED FOR ASTRA SOURCE RE-AUDIT — FINAL VERIFICATION PENDING`

`M9-T02 NOT READY — blocked by M9-T01 source and runtime acceptance`
