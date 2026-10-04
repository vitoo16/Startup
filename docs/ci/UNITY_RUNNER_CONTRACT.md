# Startup Life — Unity runner contract

Date: 2026-09-30  
Status: **prepared, not provisioned**  
Pinned Editor: **Unity 6000.3.25f1 (`e1dba0a9aba4`)**

This document defines the environment required to turn repository checks into real Unity compile/test/build evidence. It does not claim that such a runner currently exists.

## Why this is separate from engine-free CI

The normal `Engine-free CI` workflow can prove documentation, package/lock consistency, repository hygiene, and deterministic .NET simulation behavior on a GitHub-hosted Ubuntu runner. It cannot prove Unity compilation, Unity Test Framework execution, platform modules, mobile builds, or device behavior without a licensed Unity Editor and platform toolchain.

`Unity Runtime Smoke` is therefore manual (`workflow_dispatch`) and targets explicitly provisioned self-hosted runners only.

## Common requirements

Every Unity runner must provide:

- a clean self-hosted GitHub Actions runner assigned only to trusted repository workflows;
- PowerShell 7 (`pwsh`);
- Git + Git LFS;
- the reviewed Unity CLI available on `PATH`, or `UNITY_CLI_PATH` pointing to the executable;
- Unity `6000.3.25f1` with an active license usable non-interactively;
- enough disk space for `Library/`, package cache, test reports and build output;
- network access required for Unity package resolution on a clean cache;
- no project opened simultaneously in another Editor process when headless tests/builds run.

The runner must not auto-upgrade the Editor, package lock, Agent Skills, SDKs, or Xcode during a verification job.

## Android / Windows runner

Required labels:

```text
self-hosted
startup-life
windows
unity-6000-3
android
```

Additional requirements:

- Unity Android Build Support matching `6000.3.25f1`;
- matching SDK, NDK and OpenJDK installed through the Unity module/toolchain path;
- Android build tooling visible to the Editor;
- any release keystore injected through the CI secret store, never committed.

An unsigned/development smoke build may be used before release signing is provisioned, provided the retained build manifest says exactly what was produced.

## iOS / macOS runner

Required labels:

```text
self-hosted
startup-life
macos
unity-6000-3
ios
```

Additional requirements:

- Unity iOS Build Support matching `6000.3.25f1`;
- a compatible Xcode installation and selected command-line tools;
- enough space for the Unity-generated Xcode project and later archive;
- signing/App Store credentials only when archive/upload work is explicitly enabled.

The Unity smoke workflow can prove an iOS **Xcode project export** once the runner/profile exist. It cannot claim `.ipa`, signing, TestFlight, App Store processing, or iPhone behavior unless those later steps produce their own artifacts.

**Current blocker:** the user has no Mac host, so this route is not provisioned on 2026-09-30.

### ADR-010 current policy

Under [ADR-010](../adr/ADR-010-windows-android-first-ios-acceptance-deferral.md), iOS execution is currently **DEFERRED — NOT RUN**.

- The self-hosted macOS route defined above remains prepared but unprovisioned.
- No iOS workflow entrypoint, runner label, failure semantic, or future provisioning instruction is removed by the deferral.
- The deferred M0-T04 Mac/iOS prerequisites must still be completed before M18-T02 acceptance.
- Actual M18-T02 still requires the defined matching Mac + Unity iOS module + Xcode/IL2CPP route.
- Physical iPhone evidence is mandatory before M18-T02 can close.
- Android success, including Android emulator success, never implies iOS success.
- **DEFERRED does not mean PASS.**

## Repository entrypoints

### Real Unity tests

```powershell
./scripts/Test-UnityHeadless.ps1 -Mode All -OutputDirectory <evidence-dir>
```

The script uses official `unity test` against the pinned Editor and runs `StartupLife` EditMode then PlayMode tests. It requires NUnit and JUnit reports to exist. Exit semantics are retained:

- `0`: Unity produced passing test reports;
- `8`: tests ran and at least one failed;
- `6`: no valid verdict (for example compile, license, Editor, infrastructure or timeout failure);
- `2`: invalid CLI invocation.

A successful script run is real Unity test evidence. Merely having the script in Git is not.

### Build Profile build/export

```powershell
./scripts/Invoke-UnityBuild.ps1 -Profile <profile-name-or-asset> -OutputPath <path> -EvidenceDirectory <evidence-dir>
```

The wrapper uses official `unity build --profile`, requires the output and a CLI provenance manifest, and fails when either is absent.

Mobile targets deliberately require a **Unity 6 Build Profile** or a reviewed custom build method. The repository does not currently contain an accepted Android/iOS Build Profile. A live Unity session must create/import the profiles so Unity owns their serialized data and `.meta` identities; this connector session will not hand-author them.

## Manual workflow behavior

`.github/workflows/unity-runtime-smoke.yml`:

- is manual only;
- routes Android to the provisioned Windows labels and iOS to the provisioned macOS labels;
- checks out Git LFS;
- reruns the static foundation gate;
- runs EditMode + PlayMode through `Test-UnityHeadless.ps1`;
- defaults `run_build=false`;
- requires a Build Profile when `run_build=true`;
- uploads retained test/build evidence even when a later step fails.

No self-hosted runner should be registered with these labels until its prerequisites have been manually verified.

## Provisioning acceptance checklist

A runner route becomes usable only after evidence shows:

- [ ] exact Editor version/revision;
- [ ] active non-interactive license;
- [ ] Unity CLI invocation works;
- [ ] clean project import/compile;
- [ ] EditMode report exists and passes;
- [ ] PlayMode report exists and passes;
- [ ] required platform module is supported by `BuildPipeline`;
- [ ] committed Build Profile resolves;
- [ ] smoke build/export exists with provenance;
- [ ] logs/artifacts are retained by GitHub Actions.

Android and iOS satisfy this checklist independently. Passing Android never implies iOS readiness and vice versa.

## Device gates that remain outside the runner contract

Even after CI builds are green, M0/M18 device acceptance still requires real phone evidence for safe area, touch input, lifecycle interruption, save/resume, memory/performance, and visual correctness. CI artifacts are necessary but not sufficient for device QA.
