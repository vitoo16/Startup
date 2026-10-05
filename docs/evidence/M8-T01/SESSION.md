# M8-T01 — Business ownership and Save v2 implementation evidence

Date: 2026-10-05  
Owner model: GPT-5.6 Sol  
Starting main: `cbe6db4edfe2dc17504d920a5a3307e76729ebc7`  
Branch: `feat/m8-t01-business-ownership-save-v2`  
PR: #24  
Unity pin: `6000.3.25f1`  
Save impact: **SaveVersion 2 — real v1→v2 migration**

## Skills loaded

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- `unity-game-director`
- `unity-gameplay-systems`
- `unity-mcp-bridge`
- `unity-qa-release`
- `unity-game-economy`

The five community Unity skills were read from the repository-pinned
`tea-x-random/unity-game-skills` revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.

## Implemented scope

M8-T01 implements:

- stable business enums and immutable `BusinessDefinition`;
- persisted active/closed `BusinessState` instances using the global entity sequence;
- command ordinals 7–10 for launch, reinvest, pricing, and closure while preserving 0–6;
- shared-wallet launch/reinvestment debits and business ledger attribution;
- closure without refund/liquidation and pricing without cash effect;
- immutable `IBusinessReadModel` / portfolio snapshots without changing the frozen M1 API;
- SaveVersion 2 with `SaveSchema.CurrentVersion`;
- frozen historical v1 wire codec;
- synthetic v0→historical v1→current v2 and historical v1→current v2 migration;
- schema-preserving H1 receipt identity normalization for physical v1 checkpoints;
- restore/provenance validation and replay for business operations.

M8-T02, M9, and M10 behavior is not implemented.

## Business acceptance evidence

Automatic Engine-free CI run **#97** on implementation/test head
`0bc8747174610dd2d29ea02638880d91234e216a` passed.

`BusinessOwnershipChecks`: **12/12 PASS**, covering:

- definitions/enums/defensive copies/catalog compatibility;
- exact command ordinals and frozen historical canonical payload vectors;
- launch acceptance matrix;
- reinvest acceptance matrix;
- pricing acceptance matrix;
- closure acceptance matrix;
- retry/stale/identity-conflict/rejected-byte invariants;
- conservation, ledger attribution, entity order, and read model;
- v0/v1 migration and v2 roundtrip matrix;
- H1 v1 schema-preserving normalization and first v2 gameplay commit;
- intrinsic business corruption precedence;
- unavailable business revision classification.

## H1 / H2 / R2 evidence

`AstraFoundationChecks`: **19/19 PASS**.

The M8 reconciliation keeps physical v1 compatibility writes at v1 and same revision while
ordinary reads expose the migrated v2 projection. Regression tests distinguish raw checkpoint
schema bytes from the current in-memory projection rather than incorrectly treating
`GameState.SaveVersion` after migration as the raw envelope schema.

H1 includes completed/partial day-month replay, unknown ownership fail-closed behavior,
durable normalization, interrupted compatibility writes, competing owner resolutions,
and malformed ownership groups.

## Restore / M2 provenance

`RestoreInvariantChecks`: **35/35 PASS**.

Business restore validation is additionally covered by `BusinessOwnershipChecks`, including
corruption-before-content classification and exact saved business revision compatibility.

## Regression evidence

Engine-free CI run #97:

- documentation validation: PASS;
- Simulation: **49/49**;
- Business ownership: **12/12**;
- H1/H2/R2: **19/19**;
- M2 restore/provenance: **35/35**;
- Presentation adapter: **11/11**;
- Content catalog: **8/8**;
- Static Unity foundation: **31/31**;
- frozen M1 public API: **31/31**;
- Content bridge: **8/8**;
- M7 static: **43/43**;
- Unity runner contract static: **30/30**;
- repository security/release hygiene: **28/28**;
- .NET builds: **0 warnings / 0 errors**;
- clean-worktree gate: PASS.

## Unity runtime evidence

The repository is pinned to Unity **6000.3.25f1**.

This GitHub connector session has no callable licensed live Unity Editor / Unity MCP execution
surface, so changed-head runtime verification is **NOT RUN**:

- clean Unity import/compile: NOT RUN;
- Startup Life Unity C# warnings/errors: NOT RUN;
- relevant EditMode: NOT RUN;
- relevant PlayMode save/load smoke: NOT RUN.

No runtime evidence is fabricated. Engine-free/static PASS does not close this runtime gate.

## Acceptance status

M8-T01 remains unchecked.

Current classification:

`M8-T01 IMPLEMENTED — ACCEPTANCE BLOCKER: Unity 6000.3.25f1 changed-head compile/EditMode/PlayMode verification not run`

Downstream:

`M8-T02 NOT READY — blocked by M8-T01`

PR #24 remains open and must not be merged automatically.
