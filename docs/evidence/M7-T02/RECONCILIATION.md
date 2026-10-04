# M7-T02 — ADR-010 reconciliation

Date: 2026-10-04  
Owner model: GPT-5.6 Sol  
Scope: documentation / architecture-policy reconciliation only  
SaveVersion: 1

## Skills used

- `startup-life-session-orchestrator`
- `startup-life-gameplay-guardian`

This reconciliation changes implementation policy/status documentation only. No gameplay/runtime/content/scene/prefab/package/project-setting change is authorized.

## Original M7-T02 requirement

M7-T02 originally required fresh launch, pause during work, mid-course restart, next-day continuation, and backup recovery to pass in Editor and Android while also keeping an iOS smoke requirement current. Under the earlier platform policy, missing physical Android and iOS evidence kept the task checkbox open and M8 blocked.

## Existing Gate B findings

The verified runtime evidence records:

- Unity Editor/import/compile: PASS on the tested runtime tree;
- EditMode: 8/8 PASS;
- PlayMode: 11/11 PASS;
- Android emulator lifecycle/persistence acceptance: PASS for the M7 cases exercised;
- engine-free and static regression: PASS at the recorded runtime/publication heads;
- evidence provenance/runtime-tree publication audit: clean;
- physical Android: not executed;
- iOS: not executed because the Mac/Xcode/iPhone route is unavailable.

Under the old acceptance policy, the runtime report therefore concluded **M8 NOT READY / NOT SAFE FOR GATE B**. That conclusion remains accurate historical evidence for the policy in force when the report was written; it is not rewritten as an iOS or physical-Android execution result.

Sources:

- [Runtime acceptance report](RUNTIME-2026-10-04/REPORT.md)
- [Publication record](RUNTIME-2026-10-04/publication/PUBLICATION.md)

## Owner decision

The project owner explicitly authorized a Windows/Android-first implementation phase and moved the missing iOS acceptance out of M7-T02.

[ADR-010](../../adr/ADR-010-windows-android-first-ios-acceptance-deferral.md) records the authoritative amendment:

- iOS remains in MVP/product scope;
- iOS is not passed;
- M0-T04 retains the Mac/iOS infrastructure prerequisites;
- M18-T01 owns physical Android device acceptance;
- M18-T02 owns iOS lifecycle/signing/IL2CPP/physical-iPhone acceptance;
- M7-T02 may close on the already verified Editor + Android-emulator acceptance;
- M8-T01 becomes dependency-ready only after this reconciliation merges.

## Revised M7-T02 acceptance

Fresh launch, app pause during work, mid-course restart, next-day continuation, and backup recovery pass in Editor and on Android. Verified Android emulator execution satisfies this task's Android criterion.

iOS verification is deferred to M18-T02, with infrastructure prerequisites under M0-T04, and does not block M7-T02 during the current Windows/Android-first implementation phase.

Physical Android acceptance remains owned by M18-T01.

Neither deferred iOS nor physical Android is claimed passed.

## Evidence mapped to the revised acceptance

### Editor

Existing current-tree runtime evidence verifies the required Unity import/compile and lifecycle/persistence paths, including fresh launch, pause/replay behavior, mid-course restore, next-day restore, and recovery/fatal boot cases.

### Android criterion for M7-T02

The dedicated Android emulator executed the M7 lifecycle/persistence matrix and recorded checksummed state evidence. Under ADR-010, this verified emulator execution satisfies M7-T02's Android criterion.

### Physical Android

**Physical Android: NOT RUN — owned by M18-T01**

The emulator evidence does not complete M18-T01. Physical-device lifecycle, safe areas, touch, restart, storage, and the required layout/device matrix remain open.

### iOS

**iOS: DEFERRED — NOT RUN**

iOS is owned by M18-T02 for acceptance. M0-T04 retains its Mac/iOS runner/toolchain prerequisites. Android/Editor evidence cannot substitute for M18-T02.

## Save/schema boundary

- SaveVersion: **1**
- persisted field change: **none**
- migration: **none**
- gameplay behavior change: **none**
- lifecycle architecture change: **none**
- M1 frozen API change: **none**
- deterministic simulation change: **none**

No runtime tree change is part of this reconciliation.

## Why M7-T02 is now CLOSED

M7-T02 is closed because:

1. the behavior that remains in M7-T02's revised acceptance is already verified in Editor and on Android emulator;
2. the missing iOS criterion was explicitly moved to M18-T02 rather than declared successful;
3. physical Android remains a separate later device gate under M18-T01;
4. the deferred Mac/iOS prerequisites remain open under M0-T04;
5. no runtime/save/schema claim changed.

**M7-T02 CLOSED under ADR-010 revised acceptance.**

This closure does not start M8 implementation. M8-T01 becomes ready only when this reconciliation merges.

## Session closeout

Owner model: GPT-5.6 Sol

Skills used: startup-life-session-orchestrator; startup-life-gameplay-guardian.

Acceptance criteria: ADR-010 created; ledger/platform policy reconciled; historical evidence preserved; deferred iOS and physical Android remain explicitly not passed; no runtime tree change.

Tests: GitHub Engine-free CI run #77 passed on the reconciled policy/validator head: documentation validator PASS; Simulation 49/49; H1/H2/R2 19/19; M2 restore/provenance 35/35; Presentation 11/11; Content 8/8; Static Unity 31/31; M1 API 31/31; Content bridge 8/8; M7 static 43/43; runner 30/30; security/release 28/28; .NET builds 0 warnings / 0 errors; clean-worktree gate PASS. The initial run #76 exposed a PowerShell parser error in the first validator patch; that patch was rebuilt from main and run #77 passed. Unity rerun is not required because the diff remains documentation/validator-only.

Visual evidence: None; no visible runtime change.

Save impact: none; SaveVersion 1; no persisted-field change; no migration.

Known limitations: M18-T01 physical Android and M18-T02 iOS acceptance remain future gates.

Files changed: ADR/ledger/platform-policy/runner/evidence/validator documentation only.

Commit: reconciliation PR head.
