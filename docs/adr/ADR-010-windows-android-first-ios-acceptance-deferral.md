# ADR-010 — Windows/Android-first implementation and iOS acceptance deferral

Status: **accepted owner-authorized architecture amendment**. Date: 2026-10-04.

## Context

M7-T02 has verified first-playable lifecycle and persistence behavior in the Unity Editor and on a dedicated Android emulator, while the project does not currently have an available matching Mac/Xcode/iPhone execution route. Keeping the already verified Windows/Android implementation blocked solely by that external iOS dependency would prevent the next implementation phase without adding new evidence about the verified behavior.

This ADR changes platform acceptance sequencing only. It does not change gameplay, save architecture, deterministic simulation, frozen APIs, or product scope.

## Decision

Use a **Windows/Android-first implementation phase**.

iOS execution and acceptance are deferred from M7-T02 to **M18-T02**.

- M7-T02 may close using the already verified Editor and Android-emulator acceptance after this ADR and the implementation-ledger reconciliation merge.
- M0-T04 retains ownership of the Mac/iOS runner, Unity-module, toolchain, and build-route prerequisites.
- Physical Android acceptance remains owned by **M18-T01**.
- iOS lifecycle, signing, IL2CPP, serializer/AOT/stripping, and physical-iPhone acceptance are owned by **M18-T02**.
- Performance remains owned by M18-T03.
- Dual-platform release enforcement remains owned by M19.

This amendment becomes effective when ADR-010 and the corresponding ledger/evidence reconciliation merge.

## Motivation

The Mac/iOS infrastructure required for current-source iOS execution is unavailable.

Verified Editor and Android-emulator implementation should not remain blocked solely by an external Mac/iOS dependency when the missing iOS work already has an explicit later platform/device gate.

The deferral preserves truthful evidence boundaries: unavailable platform execution is moved, not reclassified as success.

## Deferred

The following are explicitly deferred from M7-T02 to the later Mac/iOS route and M18-T02 acceptance:

- Mac runner provisioning;
- Unity iOS module/toolchain verification;
- Unity iOS export;
- Xcode/IL2CPP build;
- signing required for physical-device execution;
- physical iPhone launch;
- iOS save/load;
- pause/resume lifecycle;
- cold restart/recovery;
- iOS serializer/AOT/stripping/device verification.

**iOS: DEFERRED — NOT RUN.**

## Not waived

The following remain required:

- iOS as MVP/product scope;
- physical iPhone acceptance;
- serializer/AOT/IL2CPP verification;
- safe-area, font, and device QA;
- physical Android device QA;
- dual-platform release acceptance;
- gameplay architecture;
- save architecture;
- deterministic simulation;
- frozen APIs.

**DEFERRED must never mean PASS.**

Android/Editor evidence never substitutes for required M18-T02 iOS evidence, and Android emulator evidence never substitutes for M18-T01 physical Android evidence.

## Consequences

After this reconciliation merges:

- M7-T02 is closed under its revised owner-authorized acceptance;
- M8-T01 becomes dependency-ready, but M8 is not started by this ADR;
- M0-T04 remains open because its deferred Mac/iOS prerequisite obligations still exist;
- M18-T01 remains open for physical Android device evidence;
- M18-T02 remains open for actual iOS execution and acceptance;
- later release gates remain blocked until their required platform evidence exists;
- any subsequent iOS-specific fix must rerun the affected cross-platform regressions.

No runtime/gameplay/save-schema change is authorized by this ADR. SaveVersion remains 1.

## Reopening trigger

When a matching Mac + Unity + Xcode route becomes available:

1. complete the deferred M0-T04 Mac/iOS prerequisites;
2. execute M18-T02 on current source;
3. run the current-source iOS lifecycle/save/serializer/IL2CPP matrix;
4. collect physical-iPhone evidence before M18-T02 closes.

Missing iOS infrastructure keeps M18-T02 open.

## Superseded policy

Earlier provisional wording in `docs/architecture/FIRST_PLAYABLE_CONTRACTS.md` that required the missing iOS subcriteria to keep M7-T02 and downstream implementation blocked is historical and is superseded **only** to the extent described by this ADR.

No unrelated prerequisite, task, platform gate, release criterion, save invariant, or architecture contract is waived.
