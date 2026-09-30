# M0-T04 — Release/security foundation hygiene

Date: 2026-09-30  
Owner: GPT-5.6 Sol  
Status: **static foundation pass; M0-T04 remains incomplete**

## Slice

Harden repository/release hygiene before any signing/store-upload capability exists. This slice intentionally avoids gameplay architecture and does not claim release readiness.

## Skills used

- `startup-life-session-orchestrator`
- `unity-project-setup`
- `unity-debug-profiler`
- `unity-qa-release`
- `unity-cli`

## Implementation

- Added `docs/ci/RELEASE_SECURITY_VERSIONING.md` covering:
  - secret/signing material boundaries;
  - GitHub Actions trust boundary;
  - engine-free vs Unity test vs mobile build/export vs signed distribution artifacts;
  - marketing-version policy;
  - monotonic platform build-number policy;
  - current lack of store-build number injection/signing;
  - release acceptance gates.
- Added `scripts/Test-RepositorySecurityStatic.ps1` covering:
  - required `.gitignore` credential patterns;
  - critical Git LFS rules;
  - tracked sensitive credential filenames;
  - tracked private-key PEM blocks;
  - workflow discovery;
  - first-party GitHub Action commit-SHA pinning;
  - absence of `pull_request_target`;
  - read-only workflow permissions;
  - release contract invariants;
  - presence of Unity marketing version;
  - no build dirty-tree bypass.
- Wired the security/release report into engine-free CI.

## Verification evidence

PR #4 run `36671163839`: **success**.

- Documentation validation: passed; 4/4 canonical sources, 49 expected task IDs, 30 Markdown files.
- Static Unity foundation: **31/31 passed**.
- Unity runner contract: **30/30 passed**.
- Repository security/release hygiene: **28/28 passed**.
- Engine-free .NET build: **0 warnings / 0 errors**.
- Simulation regression suite: **35/35 passed**.
- Clean-worktree guard: passed.
- Artifact `engine-free-foundation-reports`, ID `11078670016`.
- Artifact SHA-256: `ef3d1ca63f62f694428a017a8934951c16c440a3da31fbc22eebeb70d9fe9e79`.

The preceding PR #4 attempt surfaced a wrapper semantic issue: `git grep` correctly returned exit code `1` for "no private key matches," all 28 checks reported passed, but PowerShell propagated the native exit code to the job. The script now normalizes only that expected no-match result to success; grep errors greater than `1` remain fatal. The complete suite was rerun from the new head.

## What remains unproven

- Unity runtime import/compile on the final stack.
- Unity EditMode/PlayMode runtime reports.
- Android Build Profile + Android build.
- macOS/iOS runner + iOS Build Profile/export.
- signing/archive/store upload.
- physical-device behavior and performance.

These limitations keep M0-T03/M0-T04 unchecked.

## Save impact

None.

## Visual evidence

None. Repository/release hygiene is non-visible and cannot replace visual/device QA.
