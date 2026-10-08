# M9-T01 — Unity runtime acceptance / Windows handoff

## Status and evidence boundary

**Verdict for the current ChatGPT execution environment:** `UNITY ACCEPTANCE BLOCKED — LOCAL EXECUTION REQUIRED`.

**Why:** No Unity project checkout, Unity 6000.3.25f1 editor, Unity CLI, PowerShell or Git LFS are installed in the current execution container. GitHub is available through the connected GitHub tool, but the container cannot resolve `github.com` for `git clone`. No local Editor process, fresh import, NUnit/JUnit runtime test, license probe, or font hydration was performed here. Do not use GitHub CI as a substitute.

GitHub connector verification at preparation time:

- Repository: `vitoo16/Startup`.
- PR #33: OPEN, Draft, not merged.
- Exact PR head: `68a5c30c267e2a79c4f6ed12b37f08a132ef90cd`.
- Engine-free CI #143, run `37803360563`: completed SUCCESS on that head. It is **not Unity runtime evidence**.
- Required Unity version/revision: `6000.3.25f1 (e1dba0a9aba4)` from `ProjectSettings/ProjectVersion.txt`.
- Required canonical runner: `scripts/Test-UnityHeadless.ps1 -Mode All`, with optional `-OutputDirectory`, `-UnityCliPath`, `-TimeoutSeconds`, and `-AllowEditorInstall`. The local handoff **does not** use `-AllowEditorInstall`.

## Canonical source precedence

1. `AGENTS.md` → `docs/AI_PRODUCTION_WORKFLOW.md` for global workflow/skills and safety.
2. `docs/IMPLEMENTATION_PLAN_MVP.md` section **M9-T01** is the authoritative task ledger and acceptance contract, supplemented by the approved architecture/source audit in the handoff and PR #33.
3. `docs/ci/UNITY_RUNNER_CONTRACT.md` and the actual pinned `scripts/Test-UnityHeadless.ps1` define runtime/tooling and invocation semantics.
4. `docs/evidence/M8-T01/FONT_DETERMINISM_2026-10-07.md` documents the TMP/LFS deterministic fresh-import precedent. `docs/evidence/M9-T01/SESSION.md` is earlier implementation chronology, *not* a newer runtime pass.

Relevant skills read: `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `unity-cli` and repository QA/tooling documentation. Pinned community QA skills were identified by the project manifest but not executed; this is a non-implementation evidence handoff.

## Local Windows execution

Requirements: **Windows, PowerShell 7, Git, Git LFS, a working Unity CLI, installed and active Unity 6000.3.25f1 Editor matching `e1dba0a9aba4`, adequate disk/network/package access**. Close the game project in other Unity Editor instances. Use an existing **clean checkout at the exact approved SHA**. The script will not reset, fetch, checkout, merge, or rewrite that checkout. It creates an independent external detached worktree, ensuring a new, empty `Library` for the fresh import.

Save `Run-M9-T01-UnityAcceptance.ps1` anywhere **outside** the Startup repo. In PowerShell 7, supply **verified actual paths on your own Windows machine**. The following variables are placeholders to fill, not assumptions about installations:

```powershell
$Repo = 'C:\PATH\TO\Startup'
$UnityCli = 'C:\ACTUAL\PATH\TO\unity.exe'       # official Unity CLI, not Editor
$UnityEditor = 'C:\ACTUAL\PATH\TO\Unity.exe'  # installed 6000.3.25f1 Editor
$Out = Join-Path $env:TEMP ('StartupLife-M9-T01-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))

pwsh -NoProfile -File .\Run-M9-T01-UnityAcceptance.ps1 `
  -RepositoryPath $Repo `
  -UnityCliPath $UnityCli `
  -UnityEditorPath $UnityEditor `
  -EvidenceRoot $Out
```

The script verifies the Git identity, SHA and clean original source; creates an isolated detached worktree **outside** the repository; runs `git lfs pull`; verifies **both actual font binaries** against the SHA-256 OIDs committed in Git LFS pointers; checks Unity CLI installation/license; runs a real Unity `-batchmode -nographics -quit -projectPath ... -logFile ...` fresh import with the isolated worktree's initial `Library` absent; records compiler and import diagnostics; runs the canonical `scripts/Test-UnityHeadless.ps1 -Mode All -OutputDirectory <external> -UnityCliPath <verified>` in a separate PowerShell process with `STARTUP_LIFE_M7_EVIDENCE` unset; parses NUnit and JUnit for each mode; checks no skipped, failed, or inconclusive suites; and checks both original and tested worktrees still clean.

### If the original checkout is not at the approved SHA

**Stop.** Do not reset any dirty tree, test a newer commit, or force the branch backwards. Set up a separate clean local clone/worktree pointing to the known commit using your normal authenticated Git access, then rerun with `-RepositoryPath` pointing to that exact-head worktree. This script intentionally does not perform checkout operations to protect existing work.

## Evidence produced on the local machine

The script emits `EvidenceRoot/evidence/manifest.json`, `SHA256SUMS.txt`, `unity-fresh-import-editor.log`, `unity-fresh-import-console.log`, `canonical-runner-console.log`, `unity-editor-version.log`, `git-lfs-pull.log`, the canonical runner's raw `unity-tests/editmode-*` and `unity-tests/playmode-*` logs and NUnit/JUnit XML, plus `EvidenceRoot/M9-T01-unity-evidence.zip`. An external `EvidenceRoot/isolated-worktree` remains available for independent worktree/hash verification and is **not included** in the evidence ZIP (avoids packaging font files, secrets or Unity caches).

Raw logs may contain machine-specific account paths or licensing metadata. Review/redact *copies* before external sharing and record the redaction; never alter the original raw evidence used for audit. Do not attach font binaries to ChatGPT.

The script preserves partial logs and a machine-readable failed/blocked manifest when a gate fails, and exits nonzero. If no new runtime evidence exists, the correct outcome remains **BLOCKED**; a source-approved CI run does not close M9-T01.

## Evidence interpretation / known coverage boundary

M9-T01 contract: one through four businesses, full-time block reservation, redistribution, conserved time, employment, study, closure, cap, order independence. CI #143 exercised the associated .NET owner-allocation regressions (22 M9-T01 cases). The existing Unity test assemblies contain foundation, content, shell, presentation, lifecycle and mobile-baseline tests; do **not** claim the exact owner-time regression fixtures were re-executed under Unity unless their NUnit/JUnit identities establish that. Unity acceptance proves the accepted source compiles and passes the available engine runtime suites, and documents the source-only portion separately.

Required next handoff: provide the ZIP/manifest and raw suite results to **Astra** for independent final evidence audit. PR #33 stays Draft; do not merge, close M9-T01 or start M9-T02 from the local run alone.

## Session closeout

- Owner: GPT-6, Unity acceptance handoff (no Unity runtime in this environment).
- Skills: repo `startup-life-session-orchestrator`, `startup-life-gameplay-guardian`, `unity-cli`; Unity runner/QA contract and font-determinism closeout.
- Tests: GitHub source/engine-free CI #143 verified; local Unity **NOT RUN**.
- Visual evidence: N/A (no UI change and no runtime screenshot created).
- Save impact: none. No game repository files modified.
- Limitations: no Unity/Windows/Git LFS in the current container; local runner execution required; no verification yet of license/fonts/import/runtime.
- Files created: external handoff script and this instruction document only (no commit).
