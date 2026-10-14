# Startup Life — Independent static code / responsive / performance audit

**Date:** 2026-10-15 (Asia/Ho_Chi_Minh)  
**Audit owner:** GPT-6, Technical Director  
**Exact source baseline:** `1311c246bb3e4d3cb4a318b230087532a939bc46` on `main`  
**Decision:** **HOLD feature/art integration** until the first responsive/observability safeguards are implemented. This report is *audit-only*: no production code, content, scene, art or save changes.  
**Latest same-SHA CI inspected:** Engine-free CI [38596606707](https://github.com/vitoo16/Startup/actions/runs/38596606707) — **SUCCESS**. **No Unity Editor/device/Profiler proof in this audit.**

## Review scope & method

Reviewed repository's recursive Git tree (nontruncated; 1,289 non-`.meta` blobs), Unity assembly definitions, main navigation and dynamic overlays, save read models and V4 storage, front-view C01 source/authoring pipeline, first playable Editor builder, source QA and Unity test workflow. Inspection includes direct GitHub source reads and comparisons; **not** a full compile, run, disassembly, memory profile, license audit or line-by-line security review of all 1,289 files. The stated code paths are source-confirmed; user-visible effects on devices remain hypotheses until reproduced.

**Required skills loaded:** `startup-life-session-orchestrator`, `ui-ugui`, `unity-cli`, `optimize-text-mesh-pro`, `project-auditor-fixes`, `2d-pixel-perfect`, `vietnam-art-direction`. Applicable AGENTS, active production roadmap, GDD, historical MVP and AI production workflow were considered.  
**Tests executed by this audit:** NONE. Existing engine-free CI source-HEAD evidence only.  
**Visual evidence:** NONE newly collected. Existing `FirstPlayableVisualEvidence` source inspected but not executed.  
**Save impact:** NONE.  
**Known limitations:** No reachable licensed Unity Editor, Android/iOS build, GPU/CPU/memory profiler trace, real screen recording or physical density/touch measurements. No release-level performance or universal-device compatibility can be certified.

## Strengths: preserve these boundaries

- `StartupLife.Core`, `Simulation` and `Integration` have explicit asmdef boundaries; Simulation/Integration have `noEngineReferences=true`. Business/time/saves are not directly recalculated by uGUI widgets.
- `GameSession` and V4 gateway retain authoritative commands and receipt checks. `AtomicFileSaveStore` provides atomic commit/backup/recovery and validated reads. Do not replace it with an unsafe in-memory-only UI representation.
- `MobileSafeArea` reacts to safe-area and screen-size changes; shared `MobileScrollViewFactory` has vertical layout and content height fitting. Pixel UI palette static parity/contrast checks and source provenance quarantine exist.
- Archived V2 and retained released event/catalog code are **justified compatibility/frozen history**, not automatically duplicate debt; do not delete/rename them during style/optimisation work.
- Unity EditMode/PlayMode workflows exist behind self-hosted runners; engine-free CI is a genuinely useful deterministic safety net, but categorically different evidence.

## Findings (severity is a release gate/engineering priority, not a claim of a reproduced device crash)

| ID / priority | Type | Verified source evidence | Risk / actual proof needed | Fix direction |
| --- | --- | --- | --- | --- |
| R-01 **P0 release blocker** | Responsive/accessibility gap | `FirstPlayableShellBuilder.cs:63–66` sets 1080×1920 CanvasScaler; `LifeDashboardOverlay.cs:22, 299–351, 444–472` fixed 96-reference-height controls, fixed-width top button, five horizontal tabs, overflowing text. | Under a geometrically matching 360×640 viewport the nominal scale is 1/3; a 96-unit height corresponds to ~32 rendered pixels **not automatically 48dp/44pt**. Actual physical hit targets/labels may be undersized. | Validate effective physical/dp targets and text on a device matrix; adaptive tab navigation and constraints independent of absolute reference pixels. |
| R-02 **P0 release blocker** | Dynamic type not satisfied | `LifeDashboardOverlay.cs:247–252, 299, 465–472` clamps scale to 0.9–1.2, stores hard-min layout height 50, uses overflow text; Event title/status and business captions use fixed preferred heights and Ellipsis (`EventChoiceOverlay.cs:340–405`, `BusinessManagementOverlay.cs:388–402`). | No 200%-text reflow contract; essential prices, event statuses or unavailable reasons may be clipped/hidden. No device proof yet. | 100–200% option, content-driven height, clipped/overflow diagnostics, per-state text/glyph QA. Avoid autosizing text until it is illegible. |
| R-03 **P0 evidence gate** | Visual tests do not exercise responsiveness | `FirstPlayableVisualEvidence.cs:21–47` creates only 1080×1920 render target, disables CanvasScaler and forces canvas scaleFactor=1, then simulates notch manually. | Current screenshot proof cannot detect screen-scaling, unequal-aspect viewport, font reflow, keyboard open, true device DPI or touch-size bugs. | Parameterized 360×640, 390×844, 430×932, 1080×2400, tall/short notch and tablet-if-supported; keep CanvasScaler active; run screenshots, bounds and focus assertions for **all tabs/modal states**. Real device gate separate. |
| P-01 **P1 high confidence performance hotspot** | Repeated synchronous save deserialization on main UI path | `LifeScreenViewController.cs:162–239` calls snapshot, finance/portfolio and the three overlays; `StartupLifeBootstrapper.cs:143–153` binds separate V4 readers; each `EventV4GameCommands.cs:40–86` reader invokes `Verified()` → `gateway.Read()` → `AtomicFileSaveStore.Read` → `File.ReadAllBytes` and serializer replay; `EventChoiceOverlay.Refresh` calls `ReadPendingChoice` separately. | One UI refresh can read/validate the same growing save several times. Potential main-thread stalls especially with large journal/backup. **Latency and GC amounts unmeasured.** | Snapshot-scoped *verified* view bundle per refresh/revision; reuse projections from one verified checkpoint, refresh on command/focus/recovery; **preserve CAS/failed-write/cold-replay authority**, no unverified persistent cache. Profile empty/long-save p50/p95. |
| P-02 **P1 allocation risk** | Eager projections/text churn | `LifeDashboardOverlay.Refresh:123–196` builds content for **all five pages** even though one page visible, using `string.Join`, LINQ and strings; `BusinessManagementOverlay.Refresh:257–324` sorts and materializes `ToArray` twice; `LifeScreenViewController.Refresh:162–239` eagerly changes multiple TMP fields. | Increases allocations, Canvas dirty work, layout churn on command/shift updates. Not measured as frame-time regression yet. | Render active page only; revision/dirtiness diff; hoist stable formatting data; cache display rows, pool buttons; profiler compare GC Alloc, Canvas.BuildBatch, TMP mesh updates, UI CPU per refresh. |
| R-04 **P1 usability risk** | Fixed-size critical captions | `BusinessManagementOverlay.Label:388–400` always Ellipsis with fixed preferredHeight, including readiness/finance; `EventChoiceOverlay.TextNode:393–405` has same default for title/status; modal title/status fixed 76/110 units. | Can truncate reasons a player needs to understand before a financially binding decision. | Dynamic height and multi-line treatment for business eligibility, amounts, event title/status; test long Vietnamese diacritics and legal/retry/error states at 200%. |
| R-05 **P1 integration gate** | Existing scene/serialized theme contract not proven | `FirstPlayableShellBuilder.RepairGeneratedAssetReferences:200–243` is needed for existing scenes; `FirstPlayableRuntimeGate.VerifyScene:28–60` checks font/canvas/safe-area but not pixel camera PPU, serialized `uiTheme`, scene import settings or disabled/active dynamic overlay behaviour. Current C01 generated prefab is quarantined in `Art/Source`. | Spec/CI green does **not** prove `FirstPlayable.unity` renders pixel art, correct theme or articulated actor. Static .unity text cannot reliably identify all script GUID bindings. | Unity Editor reopen+repair as controlled migration, assert serialized theme, PixelPerfectCamera, no missing scripts and scene/prototype refs; PlayMode capture & phone proof. |
| A-01 **P2 maintainability** | Repeated UI construction logic | `LifeDashboardOverlay.ButtonNode/TextNode/Node`, `BusinessManagementOverlay.ButtonObject/Label/Node`, `EventChoiceOverlay.CreateChoiceButton/TextNode/Node` each create very similar uGUI hierarchies and set styles. | Drift in state colors, localization, focus/accessible button size and text-scale policy across screens. | One themed UI-control factory/prefab family with naming/focus/interaction state and semantic text roles; migrate screen-by-screen, preserve existing authoritative callbacks. |
| P-03 **P2 scale risk** | Artist preview vs production render perf | `GoldenCharacterDraftBuilder.cs:100–166` authors ~20 layer SpriteRenderers and four animation clips for **one unapproved C01**. The Editor tool does not import onto device in CI. | Animator/Renderer batching, fill, atlas/rig and frame time cannot be inferred from JSON pixel count or png preview. | First verify actual Unity import and joint movement; profile SpriteRenderer batches/materials, rig update and memory for 1, 10, 30 visible actors. Reduce per-actor renderers only after evidence, not by faking limb motion. |
| Q-01 **P1 coverage gap** | Platform/runtime gating | `unity-runtime-smoke.yml` requires self-hosted Android/Windows and iOS/macOS runners; main GitHub engine-free CI never invokes Unity. Existing visual capture is one reference resolution. | No proven Android/iOS IL2CPP and physical-device target metrics at exact SHA. | Provision editor/runner/device pool, test cold start, 30min play, background/resume, low storage, keyboard, Android back, text scaling and screen rotation/orientation handling. |
| M-01 **P2 maintainability/test** | Complex concentrated code, justified history | `FirstPlayableShellBuilder.cs` ~539 lines, `LifeDashboardOverlay.cs` ~519, `BusinessManagementOverlay.cs` ~416, `EventChoiceOverlay.cs` ~420. `tools/EconomicChecks/Program.cs` ~300KB; immutable V2/runtime replay also sizable. | Changes are costly and drift-prone; **file size alone is not runtime-performance evidence**. | Split by reason/responsibility only after golden/regression baselines; preserve frozen historic byte/behavior. Avoid rename-only V2→V3 churn or duplicate gameplay paths. |

## Recommended implementation order (no code changed by audit)

1. **Gate A — measured baseline:** Real Unity 6000.3.25f1 Editor compilation/PlayMode, physical Android sample and Unity Profiler/Memory Profiler/Frame Debugger; preserve exact SHA, Unity version, device OS/DPI, graphics API, build IDs and cold/long save data. Do not invent FPS/battery/RAM numbers.
2. **P03-RESP-01 — responsive geometry:** Central `UiViewportProfile` derived from safe width/height, insets and scale; adaptive five-tab navigation, scroll-safe overlay framing, text-dependent heights and stable 44pt/48dp-class physical target policy per platform. No gameplay/save changes.
3. **P03-RESP-02 — readable high-density text & input:** 100%, 150%, **200%**, Vietnamese glyphs, large content, disabled/ambiguous event copy, focus, software keyboard/IME, Android back, reduced-motion preference. All error/finance/choice text must remain readable.
4. **P03-RESP-03 — visual QA that actually measures scaling:** parameterized capture with enabled CanvasScaler, full bounds/no overlap/perceptual screenshots across 360×640, 390×844, 430×932, plus tall/short/notched and tablets if declared supported; compare physically on real devices. Do not silently shrink text or relax test bounds.
5. **P03-PERF-01 — eliminate repeated verified disk reads per refresh:** Pure, immutable *transaction-scoped* projection bundle from one cold-validated V4 checkpoint; revalidate on command/recovery/resume. Add exact semantic/cold-replay/ambiguous-write regression before/after.
6. **P03-PERF-02 — presentation diffing and pooling:** Active-page-only formatting and changing-text-only TMP assignments; isolate frequently changing UI into bounded Canvas subtrees; optimize LINQ allocations where Profiler confirms. Compare CPU/GC/Canvas rebuild on same scenario.
7. **P03-ARCH-01 — deduplicate themed UI primitives:** Shared panel, button/label, nontruncating status and selectable state components; migrate one screen at a time. Keep controller command routing and save layers untouched.
8. **P03-CHAR-QA — only after runtime tooling exists:** C01 Editor import, articulated motion proof, silhouettes on actual pixel camera and 1/10/30-character stress; provenance and independent artwork approval required before `Approved` registry.

## Mandatory support / QA matrix (proposed, not tested)

| Dimension | Min practical examples | Why |
| --- | --- | --- |
| Portrait layouts | 360×640, 390×844, 430×932, 1080×2400 | narrow/typical/tall width and scaling |
| Insets | none, top notch, bottom gesture area, asymmetric cutout | safe-area geometry |
| Density | at least low/medium/high density Android and current iPhone sizing | 96 Canvas units ≠ universally usable physical target |
| UI type | 100%, 150%, 200% + maximum tested OS text setting | full Vietnamese reflow and action availability |
| User states | character creation, all five tabs, event choice/disabled/retry, 0/many businesses, long debt/history | variable content and modal height |
| Lifecycle | resume, offline save load, bad-disk fallback, keyboard open/closed, return from background | real device/stale view recovery |
| Graphics | low/mid/high Android tiers; actual iOS build when macOS provisioned | IL2CPP, GPU fill, pixel scaling and memory |

Do not promise literally every phone model: define support floor and tested platforms, use responsive constraints, and reject unsupported orientation/device classes explicitly. Tablets only if added to product support matrix.

## Required performance evidence before setting release budgets

Profile the same scripted 20–30 minute representative run in Development + non-development release build where appropriate. Record **median/p95/p99** frame time, `GC.Alloc` idle and per command, UI Canvas rebuild spikes, render batches/SetPass calls, texture+font resident memory, peak RSS, save read+validate latency versus 0/100/1,000-day history, startup/resume time, and thermal throttling after a sustained session. Target *direction* is 60 FPS supported mid-tier where possible, an evidence-approved 30 FPS fallback on low tier, near-zero per-frame idle allocations, and no lost/obscured interactive choices. **Do not set unmeasured numeric budget values as achieved.** Define finalized frame/memory/time thresholds after first hardware baseline.

## Session closeout

- **Owner model:** GPT-6 Technical Director.
- **Skills used:** startup-life-session-orchestrator, ui-ugui, unity-cli, optimize-text-mesh-pro, project-auditor-fixes, 2d-pixel-perfect, vietnam-art-direction.
- **Acceptance criteria:** exact-HEAD static audit, concrete file-path findings, separates confirmed source behaviour from unmeasured impact, responsive/device matrix, prioritised clean-code/performance work without touching active gameplay code.
- **Tests:** source review only; existing engine-free CI run 38596606707 **SUCCESS**. No additional tests executed in this audit.
- **Visual evidence:** NONE newly produced or inspected on a device.
- **Save impact:** NONE.
- **Known limitations:** no Unity editor/phone/profiler execution; actual freeze rate, FPS, memory, crash incidence, art visual quality and full catalog load time unknown.
- **Files changed:** only this audit document.
- **Commit:** inspect Git history of this file; do not fabricate current SHA in advance.
