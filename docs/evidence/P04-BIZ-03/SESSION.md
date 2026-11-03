# P04-BIZ-03 — Verified Business Catalog Selection and V3/V4 Replay

**Date (Asia/Ho_Chi_Minh):** 2026-11-04
**Owner model:** GPT-6 Technical Director
**Baseline:** `9a9f8ae2d914489a145b2c69786a9544400033f6`
**Lane:** owner-authorized `main`
**Implementation status:** SOURCE IMPLEMENTED; final engine-free CI gate pending
**Unity runtime/visual/device evidence:** NOT RUN; not an approval

## Why this slice exists

P04-BIZ-02 created a seven-business preview catalog
(`first-playable.biz-a.v1`, `p04-biz-a.rules-v1`) without changing the
four-business `first-playable.v1` and `m9-t02.rules-v1`. Simply
pointing an old V3/V4 checkpoint at the new economic profile would
replay the wrong ruleset or reject the save. A content selector must
admit a track only after a source receipt replay has verified it.

## Code

- `VerifiedBusinessContentSelector` takes primary and backup bytes
  plus the old and Batch A serializers, and chooses a track **only**
  after `DeserializeAndValidate(...).Status == Valid`. A verified
  primary takes priority over backup. If a historical V2 checkpoint
  validates under both, select baseline to prevent implicit upgrades.
  An unreadable/unsupported primary **and** backup fail closed and are
  never converted into a new game. This class performs no file writes.
- `StartupLifeBootstrapper` independently builds the baseline
  and Batch A V3 serializers, and the corresponding V4 serializers.
  It checks existing save/backup bytes before selecting either track,
  then delegates actual persistence, recovery and CAS to the
  selected existing `AtomicFileSaveStore`.
- `enableBusinessBatchAForNewRuns` is serialized but **defaults false**.
  It changes only no-save genesis selection. Existing source-verified
  Batch A saves still select Batch A if this flag is later disabled;
  existing baseline saves still select baseline if it is enabled.
- New runs still originate from the original `first-playable.v1`
  revision-zero **V2 seed**. After the verified migration frontier,
  new commands use the explicitly selected live V3/V4 content and
  economic revision. No historic frozen source is edited.
- No new command, save schema, cash mutation path, settlement
  algorithm, business ledger or copy of the simulation is added.

## Deterministic regression coverage

`tools/EconomicChecks/Program.cs` now contains source-backed
regressions for:
1. New seven-business V3 character/agency launch: full capital
   charged once, two committed receipts, cold restore, repeat
   command ID, first operating advance and a second exact cold replay.
2. New seven-business V4 source-verified character/agency launch,
   cold replay of the persisted source, retry idempotency and boundary
   replay without duplicate charge/settlement.
3. Old V3 business receipt opened with its original ruleset remains
   valid; forced new-ruleset read must return
   `UnsupportedContent` / `save.ruleset_revision`, preserving
   exact bytes.
4. Selector: old primary is baseline even with new-run opt-in;
   new P04 V3/V4 save is Batch A even with opt-in disabled; corrupted
   primary can choose verified backup; invalid primary and backup
   refuse to start a fresh run.

`tools/tests/test_business_batch_a_content.py` adds source joins for
explicit opt-in, archived V2 seeding, two serializer branches,
read-only selector and V3/V4 regression presence.

## Acceptance boundary

This is **not** Unity or production approval. Before enabling the
Batch A inspector flag and regenerating authoring assets, require:
- exact-head engine-free CI PASS and investigation of any fixture
  failures, without changing simulation semantics to satisfy tests;
- Unity 6.3 editor compilation, ScriptableObject catalog/loc sync,
  first playable scene and portrait mobile visual QA;
- practical save backup/recovery scenarios with real device files;
- multi-business, multi-day market/obligation replay and balancing
  under revised rules, plus owner approval of tuning values.

## Closeout

- **Skills used:** startup-life-session-orchestrator,
  startup-life-gameplay-guardian, ui-ugui, localization
- **Tests:** V3/V4 source replay cases, ruleset refusal, verified
  serializer track, Python wiring; engine-free CI result pending
- **Visual evidence:** none, Unity Editor and physical mobile QA deferred
- **Save impact:** new opt-in code paths only; no auto-migration of old
  saves or changes to schema/old V2 archive/old economic profiles
- **Known limitations:** content opt-in default OFF; current Unity
  scene has not regenerated candidate ScriptableObjects; no device proof
- **Files changed:**
  `Assets/StartupLife/Scripts/Infrastructure/VerifiedBusinessContentSelector.cs`
  and `.meta`,
  `Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs`,
  `tools/EconomicChecks/Program.cs`,
  `tools/tests/test_business_batch_a_content.py`,
  `tools/PresentationChecks/Program.cs` (prior test fix),
  this evidence file
- **Commit:** use final `main` HEAD and exact-head GitHub Actions run

## CI-detected compatibility corrections

The first Engine-free run found a stale static bootstrap validator and a
career-to-business affinity expected list that omitted Chef's valid shared
supplier-operations tag. Both tests were repaired to check the exact current
semantics without altering gameplay. The next run exposed three source-level
replay fixture issues:
- The new Agency launch pays both its authored 1.2m capital and the
  contractual 60k daily opening fee. The test now checks the exact combined
  1.26m reduction, not only invested capital.
- Existing V2 source checkpoints must be read under their original
  `first-playable.v1` catalog while V3/V4 live simulation uses
  `first-playable.biz-a.v1`. The five-argument compatible
  `V3SaveCompatibilitySerializer` constructor now supports a separate
  live catalog without changing old callers.
- A valid old V3 ruleset supplied to a new evaluator is explicitly
  unsupported, not generic corruption. The ruleset check runs only
  **after validating the immutable frozen V2 prefix**, so malformed
  historical content cannot hide behind an unsupported-revision code.

Additional source files modified during CI diagnosis:
`Assets/StartupLife/Scripts/Infrastructure/V3SaveCompatibilitySerializer.cs`,
`Assets/StartupLife/Scripts/Simulation/VersionedReceiptReplay.cs`,
`scripts/Test-FirstPlayableShellStatic.ps1`,
`tools/ContentCatalogChecks/Program.cs`.

Final engine-free verdict belongs to the exact final HEAD; no earlier
red/cancelled run can be described as PASS.
