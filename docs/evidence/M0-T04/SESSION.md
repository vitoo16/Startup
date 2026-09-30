# M0-T04 — Engine-free CI foundation

Date: 2026-09-30  
Owner: GPT-5.6 Sol  
Status: **PARTIAL — keep M0-T04 unchecked**

## Slice

Establish the first repository-hosted CI gate that can run without a licensed Unity Editor. The workflow validates authoritative documentation/task-ledger invariants and runs the existing scene-independent .NET simulation regression suite. The simulation report is written outside the checkout and uploaded as a GitHub Actions artifact so verification does not mutate tracked evidence.

This slice intentionally does **not** claim Unity compilation, EditMode/PlayMode, MCP connectivity, Android/iOS builds, or device QA.

## Skills used

- `startup-life-session-orchestrator` — project-local mandatory session workflow.
- `new-unity-project` — official Unity setup/source-control rules.
- `unity-cli` — official CLI/test/build and CI guidance.
- `unity-package-management` — official reproducible package/lock guidance.
- `unity-project-setup` — pinned community CI/repository foundation guidance.
- `unity-mcp-bridge` — pinned community execution/fallback rules; no live Editor was available in this connector session, so no MCP action is claimed.
- `unity-debug-profiler` — pinned community evidence-first verification rules.
- `unity-qa-release` — pinned community automated-test/release evidence rules.
- `unity-localization` — pinned community localization verification boundary; no localization data changed in this slice.

Community skills were read from the revision already locked in `docs/tooling/skills.lock.json` (`dafb97ef00f94e64e42e6260bc6b3af74cc83dad`). Official Unity skills were read from the official Unity skill repository; the repository's existing lock remains the provenance record.

## Implementation

Added `.github/workflows/engine-free-ci.yml` with these gates:

1. checkout on pull requests targeting `main`, pushes to `main`, and manual dispatch;
2. provision .NET 8;
3. run `scripts/validate-documentation.ps1`;
4. run `scripts/Test-Simulation.ps1` with the JSON report under `runner.temp`;
5. fail if verification dirties the checkout;
6. upload `engine-free-simulation-report` for 14 days.

The workflow pins the current verified action releases by commit SHA:

- `actions/checkout` v7.0.1 → `3d3c42e5aac5ba805825da76410c181273ba90b1`
- `actions/setup-dotnet` v6.0.0 → `a98b56852c35b8e3190ac28c8c2271da59106c68`
- `actions/upload-artifact` v7.0.1 → `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`

The first CI run passed but exposed GitHub's Node 20 deprecation warning from older `@v4` actions. The pins above were then upgraded to the current Node 24-compatible releases and verified in a second clean run.

The workflow deliberately does not invoke `scripts/Test-Unity.ps1`: that script targets the locally installed Unity CLI/Editor and requires a real ready Editor. A future M0-T04 slice must add licensed Windows/macOS Unity runners only after runner labels, licenses, platform modules, and Mac/Xcode provisioning are known.

## Acceptance / tests

Fresh GitHub Actions verification on PR #1, run `36666827469`:

- Workflow/job conclusion: **success**.
- Authoritative documentation validation: **passed**.
  - canonical source parity: **4/4**;
  - task ledger: **49 unique expected IDs**;
  - only `M0-T01` remains marked complete;
  - exact approved dependencies/expanded skill requirements passed;
  - Markdown links checked: **25 files**.
- Engine-free .NET build: **succeeded with 0 warnings / 0 errors**.
- Simulation regression suite: **35/35 passed**.
- Repository-clean guard after verification: **passed**.
- Artifact upload: **passed**.
  - artifact: `engine-free-simulation-report`;
  - artifact ID: `11076412510`;
  - size: 1334 bytes;
  - digest: `sha256:324872c3b432ccb846c4e3a01f82f8d9f8b3e7872274d815c2762b3009d7b463`;
  - expires: 2026-10-14.
- The second run contains no Node 20 action deprecation warning after the action upgrade.

Because this evidence file is itself a follow-up commit, the PR head must still receive a final CI pass before merge. The retained run above proves the workflow implementation and pinned action set; it does not replace the final-head check.

## Visual evidence

None. This is a non-visible repository/CI slice.

## Save impact

None. No runtime schema, migration, serializer, or save fixture changed.

## Known limitations / remaining M0-T04 gates

- No licensed Unity GitHub Actions runner has been provisioned or exercised.
- No fresh Unity import/compile, EditMode, or PlayMode report is produced by this engine-free workflow.
- No Android SDK/NDK/JDK module/build evidence.
- No macOS Unity/Xcode/iOS build evidence; the existing external Mac/iPhone provisioning blocker remains.
- No physical-device lifecycle evidence.
- No claim that M0-T03 or M0-T04 is complete.

## Files changed in this slice

- `.github/workflows/engine-free-ci.yml`
- `docs/evidence/M0-T04/SESSION.md`
- `docs/IMPLEMENTATION_PROGRESS.md` (evidence index only)

## Commit / PR

- Branch: `feat/m0-t04-engine-free-ci`
- PR: #1 — `ci: add M0-T04 engine-free verification gate`
- Verified implementation run: `36666827469`
- Final PR-head run: record/confirm before merge; do not mark M0-T04 complete from this slice alone.
