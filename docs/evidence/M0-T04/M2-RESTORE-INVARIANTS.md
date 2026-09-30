# Astra M2 restore-invariant closure

Date: 2026-10-01  
Owner model: GPT-5.6 Sol  
PR: #9 — `fix: enforce Astra M2 restore invariants`

## Scope

This slice addresses only Astra's MEDIUM M2 restore-consistency finding: checksum-valid save state must not be published when its monotonic identity counters or persisted scheduler history cannot represent a state produced by the deterministic runtime.

Out of scope:
- M1 outcome/playback public-contract work;
- save schema or migration changes;
- gameplay balancing or new gameplay rules;
- Unity scenes, prefabs, UI, content/art volume, signing, or device validation;
- milestone/task checkbox changes.

## Skills used

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`
- project save/gameplay/debug/QA skill bundle from `docs/SKILLS_MANIFEST.md`

## Acceptance criteria

- `NextOperation` is strictly greater than every issued canonical `RunId/op/N` receipt identity.
- `NextEntity` is strictly greater than every persisted run-scoped entity identity still represented by authoritative state.
- malformed/reused run-scoped identities are rejected before restore publication.
- consumed activity IDs are canonical, not in the future, and `CurrentActivity` references a committed operation/activity.
- persisted scheduler deck/cursor/cycle/last-scene/signature agree with committed career activities when referenced content is available.
- valid post-promotion old-signature transitions remain accepted until the next scheduler consume rebuilds the unconsumed suffix.
- valid scheduler state survives resignation through the latest historical employment.
- missing content keeps H2 `UnsupportedContent` precedence; existing-but-wrong-career scene references remain corruption.
- valid mid-cycle restore continues deterministically.
- no save-schema change or migration.

## Implementation

`StateValidation` now validates monotonic operation/entity identities, activity provenance, and scheduler restore coherence before a state can become authoritative. Scheduler consistency is derived from the already persisted deck/cursor/cycle/signature/last-scene and committed activity history; no new persisted fields are required.

Content compatibility remains deferred as established by H2/R2. M2 scheduler checks only run when required scheduler scene references are available in the active catalog, preventing missing content from being incorrectly downgraded to corruption. Known content that belongs to another career remains structural corruption.

## Regression coverage

`tools/RestoreInvariantChecks` adds seven engine-free .NET cases:

1. operation/entity counters cannot reuse issued identities;
2. scheduler cursor cannot rewind committed work;
3. scheduler cycle follows committed career activities across a cycle seam;
4. promoted-rank transition with the old signature remains valid until the next consume;
5. committed activity history cannot be removed or forged;
6. resigned employment preserves valid scheduler ownership through history;
7. valid mid-cycle restore continues byte-identically.

Existing suites remain mandatory in the same CI step to protect H1/H2/R2 and simulation behavior.

## Verified evidence

Engine-free CI run `36755334024` at implementation head `079fa0af27349c4a6f9745f03a1120db6a8bfda8` completed successfully:

- authoritative documentation: PASS;
- static Unity foundation: 31/31;
- Unity runner contract static gate: 30/30;
- repository security/release hygiene: 28/28;
- existing simulation regression: 35/35;
- H1/H2/R2 focused regression: 19/19;
- M2 restore-invariant regression: 7/7;
- all three .NET builds: 0 warnings / 0 errors;
- clean-worktree verification: PASS;
- artifact: `engine-free-foundation-reports`, ID `11115659442`, SHA-256 `9516b4bfebc718376f31066ec0f5545025fcb984cc64568723b7f62522765191`.

An earlier CI attempt exposed an H2 precedence regression for a missing scheduled scene; the correction defers M2 scheduler consistency until referenced scheduler content is available. The next run restored the full 19/19 H1/H2/R2 suite and passed all 7 M2 checks.

## Visual evidence

Not applicable. This slice changes engine-free restore validation only.

## Save impact

Behavioral validation tightening only. Save/envelope/DTO schema remains version 1; no migration is introduced. Valid existing fixtures used by the regression suites continue to load and continue deterministically.

## Known limitations

Evidence here is .NET/static only. It does not claim Unity Editor import/compile, EditMode, PlayMode, IL2CPP/AOT, Android/iOS build, signing, filesystem durability on devices, or physical-device lifecycle/save-resume behavior.

M1 outcome/playback public-contract work remains the next Astra finding after M2 is independently verified.
