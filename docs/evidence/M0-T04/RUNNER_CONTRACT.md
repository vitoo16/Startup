# M0-T04 — Unity runner contract preparation

Date: 2026-09-30  
Owner: GPT-5.6 Sol  
Status: **prepared and statically verified; runtime runners remain unprovisioned**

## Slice

Prepare the reproducible path for real Unity EditMode/PlayMode and mobile build/export evidence without claiming that the required licensed self-hosted runners currently exist.

## Skills used

- `startup-life-session-orchestrator`
- `unity-cli`
- `unity-project-setup`
- `unity-mcp-bridge`
- `unity-debug-profiler`
- `unity-qa-release`

The execution contract follows the official Unity CLI `unity test` / `unity build --profile` semantics. No live Editor/MCP operation is claimed in this connector session.

## Implementation

- `scripts/Test-UnityHeadless.ps1`
  - resolves Unity CLI from an explicit path, `UNITY_CLI_PATH`, or `PATH`;
  - hard-gates the project to Unity `6000.3.25f1`;
  - runs StartupLife EditMode and PlayMode with official `unity test`;
  - retains NUnit, JUnit, CLI logs and a JSON summary;
  - distinguishes CLI exit `8` (tests failed), `6` (no Unity verdict / compile-license-infra-timeout class), and `2` (invalid invocation).
- `scripts/Invoke-UnityBuild.ps1`
  - builds only through a Unity 6 Build Profile;
  - hard-gates the Editor version;
  - requires both build/export output and Unity CLI provenance.
- `.github/workflows/unity-runtime-smoke.yml`
  - manual dispatch only;
  - Android route requires self-hosted Windows/Unity/Android labels;
  - iOS route requires self-hosted macOS/Unity/iOS labels;
  - defaults `run_build=false`;
  - always uploads retained evidence.
- `docs/ci/UNITY_RUNNER_CONTRACT.md`
  - defines provisioning prerequisites and explicit Android/iOS evidence boundaries.
- `scripts/Test-UnityRunnerContractStatic.ps1`
  - validates script syntax and the runner/workflow contract in normal engine-free CI.

## Verification evidence

PR #3 verification run `36670672726`: **success** after one validator false-negative was fixed.

- Documentation validation: passed; 4/4 canonical sources, 49 expected task IDs, 28 Markdown files.
- Static Unity foundation: **31/31 passed**.
- Unity runner contract static gate: **30/30 passed**.
- Engine-free .NET build: **0 warnings / 0 errors**.
- Simulation regression suite: **35/35 passed**.
- Clean-worktree guard: passed.
- Artifact `engine-free-foundation-reports`, ID `11077951686`.
- Artifact SHA-256: `fe34dd946bbfad7ad606c7ff7cfec674374b7a82e4a43517ee6f1b46abe2fdc5`.

The first PR #3 run correctly failed because the static validator's source-literal regex did not match the script text. The validator was corrected and the complete suite was rerun; no gate was bypassed.

## Remaining runtime gates

- No provisioned self-hosted Windows/Android runner is evidenced.
- No provisioned macOS/iOS runner exists; the user's no-Mac blocker remains.
- No accepted Android/iOS Unity 6 Build Profile is committed yet. These assets must be created/imported by Unity so their serialization and `.meta` identity are Editor-owned; they are not hand-authored in this connector session.
- No fresh Unity import/compile, EditMode, PlayMode, Android build, iOS export, signing/archive, or physical-device evidence is claimed.

## Save impact

None.

## Visual evidence

None. Runner/CI preparation is non-visible and cannot replace portrait/device screenshots.

## Completion rule

M0-T04 remains unchecked until a real provisioned runner executes the runtime gates and mobile route(s) required by the ledger. Static preparation is necessary infrastructure, not milestone completion.
