# M15 — Technical debt and duplication audit (2026-10-13)

**Branch/observed base:** `feat/m15-event-selection-foundation` at `76860f7b048a38b89768d9a84eb04232b7c1ec09`. **Owner:** GPT-6 Technical Director. **Scope:** production C# under `Assets/StartupLife/Scripts/`, with emphasis on engine-free simulation, historical compatibility, V3/V4 reward transaction and mobile UI. **PR #52 remains Draft; no merge.**

## Verified findings

| Risk | Finding | Classification | Action |
|---|---|---|---|
| P1 / concrete duplicate | `GameSession` and `VersionedReceiptReplay` both incremented core revisions, updated activity/cursor, created `CommandReceipt`, and invoked V3 finalization. The former already used `ReceiptCandidateFinalizer`, but cold replay duplicated it. | Real behavioral duplication: diverging future changes could break deterministic restore. | Move one helper to `Simulation` (lower assembly) and use it for both paths, preserving `Core → Simulation → Application/Integration` dependency flow. Keep same receipt fields and checks. |
| P1 / intentional historical copy | `ArchivedV2SimulationEngine` and `SimulationEngine` have substantial overlapping logic (386 matching six-line normalized windows in the targeted comparison). | **Intentional compatibility isolation**, NOT grounds for automatic merging. Historical V2 replay must remain frozen; rewriting it can reinterpret existing saves. | Record explicit exception; no refactor without byte-perfect golden fixtures, replay evidence, rollback plan. |
| P1 / concentration | `EventV4CommitGateway` ~630 lines; `GameSession` ~528; `StagedEventV4SaveSerializer` ~378. Responsibilities include commands, transaction staging, payout, batches, journal/wire admission. | Large-file/coordination debt, not automatically duplicated engines. | Split by responsibility only after establishing explicit ports and regression contracts. Preserve atomic CAS owner and cold replay proof. |
| P1 / authority boundary | `EventRewardCompositeProof` and `EventRewardReplayProof` validate overlapping reward identity fields, but at *different* boundaries (candidate proof vs cold replay admission). `V4ScopedCoreSerializer` is also a scoped read adapter, not another persistent serializer. | Legitimate repeated checks with drift risk, not safe to coalesce blindly. | Map a reusable immutable reward identity contract; retain separate proof assertions and make forgery tests cover both. |
| P2 / UI copy-paste | `LifeDashboardOverlay`, `BusinessManagementOverlay` and `EventChoiceOverlay` each construct similar `ScrollRect` viewport / mask / layout setup; `FirstPlayableShellBuilder` and `MobileBaselineBuilder` also repeat baseline canvas setup. | Concrete UI-building duplication. | Extract a small `MobileScrollViewFactory` only with Unity EditMode/PlayMode and portrait safe-area QA; no risky UI rewrite during economy audit. |
| P2 / test maintainability | `tools/EconomicChecks/Program.cs` ~261 KB; `BusinessOwnershipChecks/Program.cs` ~88 KB; `SimulationChecks/Program.cs` ~99 KB. | Test organization debt, not runtime duplication. | Split tests by milestone/domain without changing assertions, thresholds or frozen fixtures. Keep one CI invocation. |
| P2 / repeated serialization glue | V3/V4 staged serializers and reward composite code compute/check nested SHA-256, deserialize/serialize envelopes and maintain strict schema rules. | Some boilerplate overlap, but independently versioned wire contracts matter. | Centralize only version-independent digest helpers with byte-for-byte parity tests; do not merge versioned validators or let V3 deserialize rewarded receipts unscoped. |

## Scope and evidence policy

- Source inspection covered the M15 transaction classes, the historical/live simulation pair, the main gameplay receipt path, and a targeted pairwise duplicate scan across 12 core and 12 UI/projection files. **Not** a completed AST-wide or Unity Editor Project Auditor scan: local git/.NET/Unity cannot access this environment, and third-party skill samples under `.agents/` are excluded from production findings.
- Identical blocks do not prove equal semantics. Historical replay copies are protected as an architectural exception; extra validation on trust boundaries is defense in depth, not an invitation to remove checks.
- No hidden hardcoded balance values or non-approved migrations may be introduced to reduce line count.
- Before every subsequent clean-up slice: define accepted invariant, test normal execution + cold replay + failed write + retry where relevant, update evidence, keep PR Draft. Re-assess repo hotspots at M15 stabilization.

## Current safe refactor

Move `ReceiptCandidateFinalizer` (preserving Unity .meta GUID) into the `Simulation` assembly and reuse it from `VersionedReceiptReplay`. `GameSession` and `EventV4CoreCommandRunner` already import `StartupLife.Simulation`; no public receipt/wire shape changes. Exact-head engine-free CI is required before claiming PASS.

**Skills used:** startup-life-session-orchestrator; startup-life-gameplay-guardian; project-auditor-fixes (instructions reviewed; Unity Project Auditor execution unavailable); unity-gameplay-systems; unity-game-economy; unity-qa-release; unity-debug-profiler.

**Visual evidence:** NOT RUN (code-only). **Save impact:** none intended (schema and frozen fixtures untouched). **Limitations:** full Unity/static Project Auditor and broader AST duplication scan not run.
