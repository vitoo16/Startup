# M9-T01 — Owner-time allocator: implementation record and proposed evidence-only closeout

Date: 2026-10-08 (+07:00)

Status: **SOURCE APPROVED; UNITY RUNTIME VERIFIED per original manifest and supplied independent Astra verdict; EVIDENCE-ONLY CLOSEOUT REVIEW AND MERGE PENDING. M9-T01 NOT CLOSED; M9-T02 NOT READY.**

This file preserves the historical implementation-session notes below. The current provenance and remaining release gates are in the final closeout section at the end; historical statements such as “NOT RUN in this session” must not be mistaken for the later Windows runtime acceptance.

Starting main: `05b9776ad515ed8a169decdf3f2b1a6fd42aab7d`

Branch: `feat/m9-t01-owner-time-allocator`

Implementation PR: [#33](https://github.com/vitoo16/Startup/pull/33) — merged into `main` at `2026-10-08T16:20:13Z` (23:20:13 Asia/Ho_Chi_Minh). The original Draft wording below is historical.

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

## Proposed final Unity evidence closeout — pending independent closeout PR review

This **evidence-only documentation update does not close M9-T01**. Source acceptance and historical Unity evidence are recorded for external verification. No Unity, source, test runner, saved DTO, content, package, scene or font changes were made in this closeout document.

### Exact-head, merge and CI provenance

- Repository: `vitoo16/Startup`; implementation PR [#33](https://github.com/vitoo16/Startup/pull/33), targeting `main`.
- Source-reviewed and Unity-runtime-tested commit: [`68a5c30c267e2a79c4f6ed12b37f08a132ef90cd`](https://github.com/vitoo16/Startup/commit/68a5c30c267e2a79c4f6ed12b37f08a132ef90cd).
- Exact-head engine-free **CI #143**, run [37803360563](https://github.com/vitoo16/Startup/actions/runs/37803360563): **SUCCESS**; **22 M9-T01 engine-free checks** (part of the 90/90 business-ownership suite). CI is *not* Unity runtime proof. Older #133/#134 failures, #135 recovery and #140 pre-correction SUCCESS are retained above.
- PR #33 merged at **2026-10-08 16:20:13 UTC** (**23:20:13 UTC+07**), via two-parent merge commit [`a5cb2882041fbe700f2c022da731a813bc661719`](https://github.com/vitoo16/Startup/commit/a5cb2882041fbe700f2c022da731a813bc661719); parents `05b9776ad515ed8a169decdf3f2b1a6fd42aab7d` and `68a5c30c267e2a79c4f6ed12b37f08a132ef90cd`.
- Git tree ID of **both** approved head and merge commit: `c104838bd69c3fb31e94fbf1aa5f9717398a9bb9`; GitHub compare from approved SHA to merge reports **0 files changed**. The merge introduced no code/asset divergence.
- Subsequent `main` [`cb74327ee5b88ceda9c76f34f5adc3e92bdd2617`](https://github.com/vitoo16/Startup/commit/cb74327ee5b88ceda9c76f34f5adc3e92bdd2617) includes unrelated Android/URP config and generated-cache changes. **The old M9-T01 Unity runtime acceptance does NOT validate those later project settings**; review and execute a separate runtime/build gate if retaining them.

### Unity execution: original Windows evidence, not this documentation session

The [original runtime manifest](../../../M9-T01-LOCAL-HANDOFF/manifest.json) identifies the *tested exact SHA above*, Unity Editor **6000.3.25f1 (e1dba0a9aba4)**, Windows PowerShell 7.6.6, Unity CLI beta.12, active headless license, and an isolated clean worktree.

- Original Git LFS source fonts: NotoSans-Regular.ttf and LiberationSans.ttf; manifest records matching binary/OID SHA-256 verification (not mere LFS pointer hydration).
- Fresh Unity import without `Library/`: exit **0**; **0 C# compiler warnings, 0 C# compiler errors, and 0 asset import failures**. Additional informational lines matched asset filenames containing `Warning`; **do not claim every raw Unity log diagnostic is absent**.
- Canonical `scripts/Test-UnityHeadless.ps1 -Mode All -OutputDirectory <external>`: exit **0**, `STARTUP_LIFE_M7_EVIDENCE` UNSET under `-nographics`.
- Unity NUnit/JUnit: EditMode **9/9 PASS**, PlayMode **12/12 PASS**; combined **21/21**, failures/skips/inconclusive **0/0/0**. Reported runner exit **0**, after-run tracked/untracked source status empty and diff check exit **0**.
- **Coverage boundary:** 1–4 business portfolio, Study(180), 241-minute 81/80/80 apportionment, 30/120 cap, unsupported career revision and cold restore are **engine-free CI** scenarios, **not separately Unity-runtime tests by identity**.
- Visual evidence: **N/A** for allocator source. SaveVersion remains **2**; no migration or persisted DTO change.

### Preserved evidence and identity

The original ZIP and sidecar are committed under the project root `M9-T01-LOCAL-HANDOFF/` with Git LFS. The ZIP is **not embedded again** under `docs/evidence/` to avoid a duplicate binary:

- [Original Unity ZIP](../../../M9-T01-LOCAL-HANDOFF/M9-T01-unity-evidence.zip) — Git LFS OID SHA-256 `80a30d40c2da099f65baec3f217ffa7dd9c0a89045c44d70247fda059f393fe9`, declared size **333,154 bytes**.
- [Original ZIP SHA-256 sidecar](../../../M9-T01-LOCAL-HANDOFF/M9-T01-unity-evidence.zip.sha256) — full SHA `80a30d40c2da099f65baec3f217ffa7dd9c0a89045c44d70247fda059f393fe9`.
- [Original manifest](../../../M9-T01-LOCAL-HANDOFF/manifest.json) and [internal 13-file checksum list](../../../M9-T01-LOCAL-HANDOFF/evidence-SHA256SUMS.txt).
- [Original external Windows runner script](../../../M9-T01-LOCAL-HANDOFF/Run-M9-T01-UnityAcceptance.ps1) and [handoff instructions](../../../M9-T01-LOCAL-HANDOFF/M9-T01-UNITY-ACCEPTANCE-HANDOFF.md).
- Raw files **inside the LFS ZIP**: `unity-fresh-import-editor.log`, `canonical-runner-console.log`, `unity-tests/editmode-unity-cli.log`, `unity-tests/playmode-unity-cli.log`, `unity-tests/editmode-results.xml`, `unity-tests/playmode-results.xml`, their `.junit.xml` counterparts, and `unity-tests/unity-headless-summary.json`. See manifest for each file's full SHA-256 and expected size.
- Stable Git source reference: [`cb74327ee5b88ceda9c76f34f5adc3e92bdd2617` committed evidence tree](https://github.com/vitoo16/Startup/tree/cb74327ee5b88ceda9c76f34f5adc3e92bdd2617/M9-T01-LOCAL-HANDOFF). This is an immutable Git revision reference; **Git LFS object retention and public/raw ZIP accessibility are not separately guaranteed by this statement**.

**Evidence-verification boundary:** This closeout author independently cross-checked the Git LFS pointer OID, sidecar and manifest/checksum *text metadata*, but the connector did **not** hydrate/read the raw ZIP binary or recompute the 13 hashes here. The user-supplied **independent Astra final evidence audit reports** 13/13 byte checks and acceptance; no new verification of those original raw bytes is asserted by this documentation update. A stable, independently retrievable audit verdict report/permalink and exact audit timestamp are not present in the retained repository; Astra closeout review must verify/archive those before approval. Logs/artifacts must undergo credential/token check before any new public rehosting.

### Actual chronology and residual findings

1. Original M9-T01 source corrections applied to `68a5c30c267e2a79c4f6ed12b37f08a132ef90cd`; Astra source review **APPROVED** (review status supplied in handoff).
2. Unity fresh import and 9/9 EditMode + 12/12 PlayMode acceptance executed on `68a5c30c267e2a79c4f6ed12b37f08a132ef90cd`, attested by the original manifest. The manifest was generated at **2026-10-08T16:46:32Z (AFTER the implementation PR merge)**. The exact runtime start/finish timestamps are not independently verified from the raw logs here, so **do not assert that runtime execution itself preceded the merge**; archive the raw timestamp evidence during independent closeout review.
3. PR #33 merged into `main` at **2026-10-08 16:20:13Z**.
4. Independent Astra **FINAL Unity runtime evidence audit** reported **APPROVED/VERIFIED PASS** *after the merge* (supplied verdict; exact audit time and permanent report location **not independently recorded here**).
5. This evidence-only closeout PR is proposed **after** the final evidence audit.
6. Final independent **Astra closeout PR review**, then actual closeout PR merge, then milestone state reconciliation are still pending.

**P-01 HISTORICAL PROCESS DEVIATION:** PR #33 merged *before* independent final Unity evidence audit. This sequence must stay visible; do not re-date it or imply the final evidence audit preceded merge.

| Finding | Residual status and disposition |
|---|---|
| E-01 | **CLOSED** — independent Astra reports original ZIP/raw evidence verified |
| ENV-01 | **LOW / nonblocking** — licensing and ILPP diagnostic context retained in raw editor log |
| E-02 | **LOW / nonblocking** — separate import console log not retained; raw editor log is sufficient for recorded contract |
| E-03 | **LOW / nonblocking** — acceptance execution script retained separately from ZIP, linked above |
| P-01 | **Historical deviation** — implementation PR merge preceded final evidence audit; documented without rewriting chronology |
| D-01 | **Pending final closeout review** — prior M9-T01 `SESSION.md` and implementation ledger stale; this PR proposes the correction |
| New config follow-up | **Not part of M9-T01 tested SHA** — the later `main` URP/Android Build Profile changes require separate validation before claiming those settings accepted |

**Remaining release gates:** independent Astra evidence-only closeout PR review, raw LFS ZIP/checksum and final audit record confirmation, actual closeout PR merge and subsequent ledger CLOSED synchronization. **No source reimplementation, no Unity re-run solely for documentation, and no M9-T02 implementation.**

**Skills/read constraints:** repo `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `startup-life-asset-quality-gate`, `unity-cli`; `AGENTS.md`, implementation plan, CI Unity runner contract, prior M8 evidence conventions and source PR history. This update is documentation-only; no visual art QA or save migration applies.
