# Final M2 provenance closure candidate

Date: 2026-10-03  
Owner model: GPT-5.6 Sol  
PR: #11 — `fix: close final Astra M2 provenance gaps`

## Scope

This follow-up addresses only the two HIGH findings remaining after the independent review of merged PR #10:

- **F1 — authoritative character/cash provenance:** a checksum-valid checkpoint could diverge from its committed creation inputs or cash history, publish a session, and fail a later legitimate command.
- **F2 — post-resignation activity ownership:** a valid unemployed boundary later on the resignation date could be misclassified as a historical career scene because the structural scheduler check inferred ownership from timestamps.

M2-C global entity identity continuity remains unchanged and was already independently closed. H1/H2/R2 remain regression-protected. M1 outcome/playback, gameplay expansion, UI/content work, and milestone/task checkboxes are outside this slice.

## Skills and workflow

Repository workflow and the project skill manifest were read before implementation.

Skills applied:

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- pinned `unity-game-director`
- pinned `unity-gameplay-systems`
- pinned `unity-game-economy`
- pinned `unity-debug-profiler`
- pinned `unity-qa-release`
- `unity-mcp-bridge` contract reviewed; no Unity/MCP scene action was needed for this engine-free C# slice

Community Unity skills were read from the repository-pinned revision
`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`.

No ADR was required because the correction tightens existing restore provenance without changing gameplay rules, dependency direction, persistence shape, or schema version.

## F1 correction — character and cash provenance

### Core creation binding

`StateValidation.ValidateReceiptTimeline` now binds the first committed `CreateCharacter` receipt to the persisted intrinsic character identity:

- trimmed name;
- starting age;
- derived birth date;
- background;
- appearance.

When the saved start definition is available, persisted `LearningSpeed` must also match that definition. A mismatch is structural corruption rather than a later continuation failure.

### Detached simulation reconstruction

`SimulationRestoreValidator.Validate` already reconstructs the candidate from the complete ordered receipt history through the production simulation engine. Its final authoritative comparison now also includes:

- name;
- starting age;
- birth date;
- background;
- appearance;
- learning speed;
- cash.

Therefore a candidate with forged starting inputs or cash cannot publish a restored session and then fail a dependent `Study` or `PurchaseCourse` command because of hidden divergence.

Compatibility precedence remains unchanged: unavailable required content still uses the existing `UnsupportedContent` path, while corruption provable from intrinsic or available definitions remains `Corrupt`.

## F2 correction — historical activity ownership

`StateValidation.ValidateSchedulerContent` no longer derives historical employment cursor/cycle counts from activity timestamps after the employment has ended.

The old heuristic was ambiguous because `EmploymentState.EndedIso` records only the date. After a mid-shift resignation, a legitimate unemployed boundary can begin at a timestamp that is also a former career slot.

For **current employment**, the existing timestamp/boundary structural checks remain.

For **historical employment**, deck ownership/signature/content structure is still checked, but exact cursor/cycle provenance is owned by the mandatory `SimulationRestoreValidator`, which reconstructs receipt/employment ownership using the production engine and quota scheduler. No end-minute field or save migration was added.

## Regression coverage

The M2 restore suite now contains 35 cases. Four new focused cases cover the final review findings:

1. forged creation receipt and persisted intrinsic character identity mismatches are rejected before publication;
2. a valid restored purchased course can execute `Study(60)`, while forged `LearningSpeed` is rejected before publication;
3. a valid low-cash restore rejects an unaffordable course through the normal economy rule, while forged cash is rejected before publication;
4. with a 540–1020 career, two scenes per shift, and quota 10, a character can consume the first scene, resign at minute 780, then commit the unemployed 780→1320 boundary; the resulting checkpoint remains valid and deterministically continuable.

The existing 31 M2 cases continue to cover receipt/activity rewind, reward replay, deterministic scheduler reachability, promotion/cycle seams, RNG/generation provenance, historical content classification, global entity continuity, serializer verifier requirements, and deterministic restore continuation.

## H1 test adaptation

The stronger F1 validator intentionally prevents public serialization of forged cash state.

Two H1 storage-adversarial fixtures previously used `serializer.Serialize` to manufacture same-generation gameplay-tampered bytes. Those fixtures now build a raw checksummed envelope with `JsonSaveSerializer.Wrap/WriteObject` so the storage layer is still directly tested against hostile bytes.

This does **not** weaken production validation:

- the public serializer still rejects the forged state;
- the compatibility store still fails closed;
- primary bytes remain unchanged;
- a different invalid backup is not misidentified as a pending ownership mirror.

## Verified functional evidence

Engine-free CI run `37114235959` at functional head
`30e9790190943360a3d0045f747e72252312fbfe` completed successfully:

- existing simulation regression: **35/35**;
- H1/H2/R2 focused regression: **19/19**;
- M2 restore regression: **35/35**;
- all three .NET harness builds: **0 warnings / 0 errors**;
- static Unity foundation: **31/31**;
- Unity runner contract static gate: **30/30**;
- repository security/release hygiene: **28/28**;
- authoritative documentation validation: PASS;
- clean-worktree verification: PASS;
- artifact `engine-free-foundation-reports`: ID `11270232850`;
- artifact SHA-256: `8f07d95e8436ecc6ded590ce2a4367d8768389bfb2a119489f632e65ac0e12a2`.

Two preceding CI attempts exposed obsolete H1 test construction rather than production regressions. The tests were corrected to preserve their storage-layer adversarial intent without bypassing or weakening the new F1 restore invariant.

## Save / architecture impact

- Save/envelope/DTO schema remains version 1.
- Synthetic-v0 migration remains unchanged.
- No persistence field was added.
- No end-minute migration was introduced.
- Core still owns the restore-verifier port.
- Application still supplies the Simulation-backed verifier.
- Infrastructure still has no Simulation dependency.
- Runtime quota scheduler and production simulation remain the authoritative deterministic reconstruction logic.
- No M1 API/outcome/playback work is included.

## Evidence boundary

This evidence is engine-free .NET/static evidence only.

It does not claim:

- Unity Editor import/compile;
- EditMode or PlayMode;
- IL2CPP/AOT;
- Android/iOS build or signing;
- physical-device filesystem durability;
- physical-device save/resume behavior;
- device timing/allocation performance.

Receipt-history reconstruction cost remains a later runtime/platform measurement gate, not a claimed device-performance result.

## Closeout status

This implementation is a **closure candidate**, not an independent declaration that M2 is closed.

PR #11 must receive an independent Astra verification of F1/F2, M2-C regression safety, and H1/H2/R2 regression safety before work proceeds to M1.
