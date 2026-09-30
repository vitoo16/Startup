# M0-T03 foundation audit

Date: 2026-09-30. Reviewer: delegated architecture/audit owner. Implementation owner: primary Codex under the recorded fallback. Status: **acceptance pending evidence completion; keep M0-T03 unchecked**.

This audit checks the Windows foundation against [M0-T03](../../IMPLEMENTATION_PLAN_MVP.md) and the staged work allowance in [contract section 12](../../architecture/FIRST_PLAYABLE_CONTRACTS.md). It does not require Mac/iOS or Android modules to establish the Windows baseline; those remaining provisioning/build/device gates belong to M0-T04 and later platform acceptance.

## Evidence inspected

| Criterion | Observation | Audit result |
|---|---|---|
| Concrete Unity 6.3 patch | ProjectVersion.txt pins 6000.3.25f1, revision e1dba0a9aba4. | Source pin verified; installed/live version reported by owner needs retained current diagnostic. |
| Resolved compatible packages | manifest and package lock agree for all inspected direct dependencies. URP 17.3.0, Animation 13.0.6, PSD Importer 12.0.2, Input System 1.20.0, Test Framework 1.6.0, uGUI 2.0.0, Localization 1.5.8; official Pipeline 0.8.0-exp.1. | Manifest/lock consistency verified. |
| Universal 2D/portrait | GraphicsSettings pipeline GUID resolves to UniversalRP, whose renderer GUID resolves to Renderer2D. Baseline scene has orthographic camera and post-processing disabled; project sets portrait default and disables landscape autorotation. | Source/asset wiring verified; screenshots consistent with portrait. |
| uGUI/TMP/localized text | Baseline scene contains TextMeshProUGUI and LocalizeStringEvent components. Font source and OFL retained. | Structure and visual output verified; real PlayMode localization-init report still pending. |
| Clean import/compile | Owner reports successful compilation and 3/3 Unity EditMode tests. Inspected FoundationTests source: deterministic quotas, leap/RNG check, safe-area reapplication. | Actual final report/compile log not yet retained in task evidence at audit time; do not infer clean compile from test source. |
| Visible metadata / text serialization | VersionControlSettings says Visible Meta Files; EditorSettings serialization mode is ForceText. All 131 inspected asset/directory entries have sidecar metadata on disk. | On-disk metadata verified. |
| Metadata tracked / generated files ignored | Git branch is main; git ls-files returned zero tracked metadata. Assets, Packages, and ProjectSettings are still untracked. Library/Temp/Logs/Builds/TestResults probes are ignored. Startup.slnx is generated and currently unignored. | Tracking criterion not met yet; add generated .slnx ignore rule before indexing verified source. |
| LFS | Git LFS 3.7.1 available; attributes apply LFS to screenshot PNG and licensed TTF. | Configuration verified; actual indexed pointer/commit evidence remains pending. |
| Dependency M0-T02 | Session addendum reports 39/39 after installation, but retained editor-gate-report.json still contains historical pinnedEditorInstalled=false and failed gate. | Retain the actual current passing report and reconcile task/dependency status. |

## Visual inspection

Inspected [portrait baseline](portrait-baseline.png) at 720×1280 and [notched safe-area simulation](notched-safe-area-simulation.png) at 720×1560. Both have legible Vietnamese copy/diacritics, calm cream/green colors, appropriate contrast for this baseline, and no visible text clipping. The longer body and glyph sample wrap cleanly in the narrower effective layout. The inset second capture is consistent with safe-area simulation; it is not physical-device evidence.

The scene's documented fictional An Bình locality, contemporary work/study wording, and actual TMP text follow the baseline locality contract. No generated imagery, baked Vietnamese text, stereotype decoration, or production-art approval is implicated. This is an explicitly labeled development glyph/layout baseline, not a Golden screen or playable game. The absence of a character/business loop is appropriate to this foundation task.

Before closing visual provenance, record the capture method, actual Game View resolution, simulated safe-area rectangle, source scene, and relevant revision with these images. The safe-area EditMode test checks reapplication mathematically but cannot by itself prove the screenshot used the same rectangle.

## Required closeout actions

1. Retain final Unity EditMode result and post-change compile/console diagnostics, with command/entrypoint and pass/fail counts. Include the pending PlayMode localization smoke result once executed; no pending test is a pass.
2. Replace or clearly supplement the historical Editor gate report with the actual current 39/39 artifact; reconcile M0-T02 status honestly rather than inferring dependency completion from installation.
3. Update SESSION.md: visuals and connection are no longer merely pending if backed by artifacts; link reports/captures and keep platform limitations explicit. Add capture dimensions/simulated safe-area provenance.
4. Ignore generated Startup.slnx, stage/track verified Unity assets with their metadata and package lock, and verify generated/cache folders remain untracked. Commit only the verified coherent slice, recording its identity or explicit current staging status.
5. Establish and retain the local M0-T04 test/build entrypoint smoke evidence required by section 12 before dependent first-playable integration. Full Mac/iOS, Android-module/device, and CI provisioning remain separate incomplete gates.

## Provisional integration decision

**Conditional authorization remains valid under section 12; the presently inspected record is not yet sufficient to close M0-T03.** No new architectural defect was found in this baseline. Continue completing the local foundation and test entrypoints now. Once the evidence and tracking gaps above are resolved, the owner may proceed through provisional M1–M7 functional order (Unity pure-behavior tests before dependent UI, then PlayMode/Android evidence as available) despite the external Mac/iOS blocker. This audit does not change that allowance or waive any acceptance criterion.

Do not mark M0-T04, downstream full milestones, overall first playable, or either platform's device acceptance complete on this baseline. An Android module INSTALL_LOCK_CONFLICT needs provisioning evidence; no Mac means the iOS route remains externally blocked.

## Session closeout

- Skills used: previously loaded startup-life-session-orchestrator, gameplay guardian, director, gameplay systems, MCP bridge, QA release, debug profiler, economy; additionally loaded vietnam-art-direction and startup-life-asset-quality-gate for visual-review boundaries.
- Tests: read-only manifest/lock comparison (zero version mismatches), asset-sidecar inventory (131 inspected, zero missing), Git/LFS/ignore inspection, settings/scene-reference inspection, test-source review, and two image inspections. Reviewer did not run or modify the owner's live Editor/test session.
- Visual evidence: two linked baseline screenshots inspected; device provenance not claimed.
- Save impact: none.
- Known limitations: pending acceptance evidence/tracking; no device build or physical lifecycle verification; no full gameplay integration audited.
- Files changed: only this audit document.
- Commit: none by reviewer.
