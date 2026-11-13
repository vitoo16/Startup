# Startup Life — CI reliability and resource audit (2026-11-13)

**State: FIRST INSTRUMENTATION ONLY — NOT OPTIMIZED / NOT ACCEPTED.**
**Base commit:** `a3684e0e400a97fbff6652d3e590faa2cfea7aa2`. **PR branch:** `ci/audit-runtime-20261113`.

## Source and requirements checked

- Unity project 6000.3.25f1 with separate Core, Simulation, Application, Content, Infrastructure, Integration, Presentation, Editor and Unity tests.
- Engine-free test tooling in `tools/*Checks`, PowerShell scripts and static Python validators. Git LFS tracks PNG and other source assets.
- Production roadmap and `AGENTS.md`: engine-free checks must stay on code revisions and cannot certify Unity import, PlayMode, EditMode, IL2CPP, physical devices or artistic approval.
- `engine-free-ci.yml`: runs on all PRs and pushes to main, with existing cancellation for obsolete revisions; `unity-runtime-smoke.yml`: explicitly manual self-hosted Unity platform testing; six C03 source/pose/audit workflows have selective triggers and retained artifacts.
- Recent Actions allocation failures occur before executable steps and currently prevent fresh runner timing or same-SHA validation. The old successful run is historical only.

## Implemented (low-risk)

Added one diagnostic-only step immediately after checkout in engine-free CI. It records current commit, runner OS/architecture, Python, Git, Git LFS version and tracked LFS file count in `$RUNNER_TEMP/startup-life/runner-baseline.json`. The already-existing final artifact upload captures `$RUNNER_TEMP/startup-life/*.json`. This step does **not** alter simulation inputs, art assets, test commands, Unity setup, save format, security/approval gates or the existing runner timeout. It does not claim to measure job-runtime costs; the Actions jobs API must supply those once allocation recovers.

## Planned work and guardrails

- [x] Classify the current tree, Unity asmdefs, simulation tests, C03 workflows and manual Unity runner contract.
- [x] Add reproducible runner/source metadata artifact without changing the test matrix.
- [ ] Capture per-job and per-step elapsed time, CPU/memory and Git LFS checkout time on a successfully allocated GitHub-hosted runner. Record run IDs, exact SHAs, cancelled attempts and actual billable runner-minutes separately.
- [ ] Measure and compare selective LFS fetch, restored .NET/NuGet outputs and Python package caching only using exact same-sha test outcomes. Do not fetch partial asset sets without fixture validation.
- [ ] Create source-to-test traceability for deterministic simulation, save/replay/migrations, economic results, rendering source, sprite/rig and provenance. Shared or unknown paths must fall back to full engine-free tests.
- [ ] Preserve Unity EditMode/PlayMode, graphical screenshot, platform Build Profile and physical-device gates as separate evidence classes; no static PASS may imply any of them ran.
- [ ] Benchmark candidate workflow changes and keep only improvements with no lost assertions, passing clean checkout and rollback proof.

## Acceptance

Same-SHA engine-free CI green with runner metadata artifact, before/after job/runtime evidence, no missing test/asset files or altered test assertions, C03 source provenance preserved, and the Unity-runtime/device evidences clearly marked as separately verified or BLOCKED. Until Actions runner allocation is restored, all new tests remain NOT RUN and acceptance stays open.
