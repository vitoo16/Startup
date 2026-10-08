# M9-T01 — Owner-time allocator implementation handoff

Date: 2026-10-08 (+07:00)

Status: **IMPLEMENTATION DRAFT — exact-head engine-free verification and Astra source audit required. NOT CLOSED.**

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
- Unavailable exact definition or content version returns a fail-closed planning status.
- Read-only previews do not increment revision, allocate entity/operation IDs, mutate RNG, save, add receipts, touch cash or settle a business day.
- No `BusinessDayPlan`, pricing/customer demand, operating costs/revenue, M9-T02 settlement or M10 kiosk helper.
- SaveVersion remains **2**, no new persisted fields or migration. No frozen M1 public API changes.
- No scenes, prefabs, serialized assets, fonts or ProjectSettings changed.

## Tests and evidence boundary

- Initial PR CI #133: failed **static Unity asset hygiene** because newly authored C# files lacked Unity `.meta` sidecars. The three sidecars were added.
- CI #134: source compiled; 10 newly authored tests failed because *their synthetic fixture cursors had no matching receipts*. The production occupancy projector correctly rejected those invalid fixtures.
- Synthetic fixture receipt provenance was corrected without weakening the projector.
- CI #135: **SUCCESS** after fixture correction; all engine-free checks completed successfully.
- A subsequent exact-math/content-version hardening pass has a separate exact-head CI check. Its result must be verified from the PR before source audit; do not cite #135 as proof for a later SHA.
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
- Tests: GitHub engine-free CI on PR #33; use final-head run for definitive status.
- Visual evidence: N/A.
- Save impact: SaveVersion 2 unchanged; no fields/migration.
- Known limitations: source not Astra-reviewed; Unity not executed; no M9-T02 financial settlement; available future capacity is a preview, not reserved.
- Files changed: Core owner-time contracts and .meta, Simulation projection/allocator and .meta, GameSession read model, ownership test harness, this evidence note.
- Merge: **NOT AUTHORIZED**.

`M9-T01 IMPLEMENTED FOR SOURCE REVIEW — FINAL VERIFICATION PENDING`

`M9-T02 NOT READY — blocked by M9-T01 source and runtime acceptance`
