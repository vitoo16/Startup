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


## Regression caught during refactor

Initial refactor `75c695652483439a296aae5a5a5e0ddd5e6d90df` reached 0 build warnings/errors but failed 14+ real-file economic tests (CI #38284245191): the replay GameState had no `EconomicV3` attachment, while the authoritative payload was separately constructed. This is a **real boundary difference**, not proof of a valid historical replay state. Corrective approach: explicitly supply that payload to the one finalizer, validate `payload.Current == candidate`, and keep replay tests unchanged. Do not waive thresholds or relabel the initial result. Corrected SHA and CI results belong in the implementation journal.


## Increment 53 — P1 coordinator extraction

- Batch orchestration moved to `Integration/EventV4AdvanceCoordinator.cs`; V4 gateway continues to own every CAS and candidate serializer invocation via a four-method internal port.
- `Integration/EventV4CommandGuard.cs` provides a single namespace/frontier predicate implementation reused by both gateway and batch coordinator. Command-result conversion is reused across regular and batch paths.
- `EventV4CommitGateway` public methods, payload contracts and event error/status semantics remain unchanged. This is a responsibility separation, **not** a new scheduler or a second transaction engine.
- Target regression: engine-free economic checks including durable day/month batches, pending event interruptions, ambiguous CAS/cold retry, V2/V3 historical replay; Unity runtime tests remain deferred per AGENTS.md.


## Increment 54 — Canonical financial receipt identity

- Consolidated the duplicated *receipt-shape* checks from candidate construction, V4 cold replay and CAS transition into `EventRewardSettlementPlan.MatchesCoreReceipt`. Historical/source-authority validation **must remain separate** at each trust boundary.
- Negative tests exercise internal command namespace, canonical payload, revision, cue, duration, operation ID, parent and advance metadata. Never assume a receipt-shape match alone licenses payout.
- Milestone remains PARTIAL; exact-head CI results are documented separately in the M15 journal.

## Increment 55 — Three overlay scroll trees consolidated

- One uGUI hierarchy constructor `Presentation/MobileScrollViewFactory` replaces equivalent scroll creation in LifeDashboardOverlay, BusinessManagementOverlay and EventChoiceOverlay; per-screen visual parameters remain explicit and unchanged. No secondary safe-area or navigation authority introduced.
- Added Unity EditMode tests covering required ScrollRect/Viewport/Content hierarchy, rect anchors, masking, content growth and variant padding/spacing/sensitivity. These require an actual Editor run to count as evidence; engine-free static checks are not visual QA.
- Unrelated ButtonNode/TextNode helpers are intentionally not merged in this slice. Tests and frozen saves were not loosened.


## Increment 56 — Catalog provenance / compatibility debt

- V4 migration now stamps one deterministic SHA-256 fingerprint for **all selection and authored outcome inputs**, not a handwritten version label that can drift. Serializer rejects mismatched pinned content as `UnsupportedContent`.
- Untagged older V4 checkpoint compatibility is preserved (missing field means legacy, no invented fingerprint). Never introduce changed event content in production without an explicit archived-baseline roster and resolver: a missing field does not prove which authoring version selected past events.
- Next debt slice: supply immutable archived catalogs and an explicit policy for old untagged V4 runs, and route both read replay and future event commands through the resolved run-specific catalog.


## Increment 57 — P1 archive resolver without duplicate replay engine

- One immutable `EventCatalogArchive` maps verified authored SHA-256 fingerprints to historical snapshots. Serializer and V4 gateway resolve each run's catalog from the **same archive**; new runs pin current, previous runs retain old content and receipts.
- A legacy V4 save without a fingerprint always requires the declared historical baseline when the roster evolves. No silent fallback or self-repair is allowed. Ordinary single-catalog setup remains backwards compatible.
- Next step for shipping: bundle the actual released historical roster(s) as immutable checked assets and inject the archive into production bootstrap. A synthetic test snapshot demonstrates correctness but is not production evidence of the released roster.


## Increment 58 — Release asset retention

- Registered independent immutable first-playable event definition/policy baseline, separate from live content. Exactly 20 archived event definitions, 40 archived choices and four policies. **The intentional authored-data snapshot is not duplicate simulation code.**
- Production V4 opt-in bootstrap injects `EventCatalogArchive` once. Standard V3 bootstrap remains unchanged. Changes to live event content no longer overwrite the archived V4 rules.
- Separate remaining issue: released Vietnamese localization entries must be retained in immutable history too before changing those strings; fingerprint currently hashes localization keys rather than translated text.


## Increment 59 — Historical Vietnamese localization copy is intentional data retention

- The first-playable release now preserves its **80 exact Vietnamese title/body/choice strings** alongside the immutable authored rules. Current Unity StringTable updates are not permitted to rewrite what a historical V4 ticket displayed.
- Historical translation data intentionally repeats old user-facing copy (versioned release contract), **not gameplay source**. The runtime UI still uses the same localized TMP components; only the authoritative source changes when displaying an archived event.
- User-requested code-first mode: Unity and engine-free tests deferred, no PASS inferred. Technical debt to validate: historical literal→current localized label state transition, mobile overflow and future-release Vietnamese archives.
