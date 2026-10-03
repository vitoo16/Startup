# M7-T01 presentation and local Unity runtime verification

Date: 2026-10-04 (Asia/Bangkok)  
Owner model: primary Codex, continuing the recorded primary takeover; no Sol/Gemini execution attributed.  
Branch: `fix/m7-presentation-runtime-gate`  
Starting main: `4c7b9e3e4f9b4b9ed91c1c1862fe1cf4a1710b14`  
Final functional source head: `b2bc2ccebdb63d3a578f9db2d401a6b2ab931ed7`

**M7-T01 NOT CLOSED.** The local source, generated assets, Unity journey, visuals and normal-save smoke now pass. Formal prerequisite acceptance remains pending: M7-T01 depends on M6-T02, while the authoritative ledger still records only M0-T01 complete. The [provisional sequencing allowance](../../architecture/FIRST_PLAYABLE_CONTRACTS.md) retains full M1–M7 acceptance gates pending dependency/platform review. This evidence does not close those upstream gates. No checkbox changes; M7-T02 is not started; **M8 NOT READY — blocked by M7-T02**.

## Revision and evidence integrity

PR #17 was freshly verified merged; its merge is the starting main above. Its Engine-free CI #61, run 37139073037, succeeded on 7a5ad94910316b76a928244110b0fdfdf2c4ca07; that is baseline evidence, not evidence for these changes.

The fixed functional head above identifies the final implementation and test source. Subsequent commits contain documentation/evidence only. A tracked document cannot contain its own commit hash: resolve the final repository SHA with `git rev-parse HEAD`, compare it with the [branch head](https://github.com/vitoo16/Startup/tree/fix/m7-presentation-runtime-gate), and use the PR's latest Engine-free CI check for that exact head. The PR description and final execution receipt record the resolved final SHA and run URL. Do not use historical #57/#58 as current verification.

[Runtime manifest](M7-RUNTIME-2026-10-04/manifest.json) contains artifact SHA-256 hashes, runner/version, actual counts, capture geometry, save-smoke results and task boundaries. The original local main was f4518ad85a5269d4463f0cfaf0eebf033ac49242 before fast-forwarding. Three pre-existing engine-free reports were copied outside the repository and preserved in the named stash `preserve pre-existing engine-free reports before M7 runtime gate`; they were not overwritten or included in this PR.

## Single Presentation command seam

**FirstPlayableFlow** is canonical. StartupLifeBootstrapper, CharacterCreationViewController, LifeScreenViewController, WorkShiftPlaybackController and StudyActionController now consume it. DaySummaryModal consumes its immutable FirstPlayableState.

FirstPlayableFlowCoordinator and its .meta were removed through Unity AssetDatabase after migration and serialized-scene verification. The obsolete tools/PresentationFlowChecks project/source were removed; their useful assertions were merged into PresentationChecks.

The canonical seam depends only on frozen IGameCommands/public Core contracts. Each envelope reads fresh RunId and Revision; ICommandIdSource is injected and empty IDs fail before dispatch. It maps create/job/boundary/course/study/resign/playback/day/month commands, publishes immutable collections and ActiveCourse, and exposes no mutable GameState, receipts, checkpoint or save API. Historical retry outcomes remain Application-owned. Backward playback is passed to Application and returns Rejected/playback.stale without writes or rewards, rather than introducing a competing Presentation rule.

PresentationChecks now has **11 cases**, including current RunId/latest Revision, command mappings, injected IDs, empty direct/batch IDs, immutable projection, ActiveCourse, acknowledgement activity, actual backward rejection, and old/cold-restored course retries with unchanged historical outcome/save bytes/write count.

Test-Simulation.ps1 assigns **presentation-adapter-report.json once**, and only PresentationChecks writes it. No duplicate suite or historical counts are retained.

## Implemented source — PRESENT

- Composition root: StartupLifeBootstrapper creates/restores GameSession using authored Content, JsonSaveSerializer and AtomicFileSaveStore.
- Character entry and Life UI: CharacterCreationViewController and LifeScreenViewController.
- Work/playback and evening study: WorkShiftPlaybackController and StudyActionController.
- Snapshot-based Day Summary: DaySummaryModal.
- Content bridge/builders: StartupLifeContentCatalogAsset, FirstPlayableContentTemplate and FirstPlayableContentBuilder.
- Editor scene generation/verification: FirstPlayableShellBuilder and FirstPlayableRuntimeGate.
- Vietnamese strings: FirstPlayableUI table builder, LocalizedKeyLabel and LocalizeStringEvent/TMP bindings.
- Safe area and Input System: MobileSafeArea, portrait CanvasScaler, EventSystem/InputSystemUIInputModule with default actions.
- Actual test source: FirstPlayableShellTests, FirstPlayableFlowTests, FirstPlayableVisualEvidence and existing foundation/content/baseline tests.

## Generated assets — PRESENT

All generated/modified serialized assets were produced and saved through Unity Editor APIs; no YAML was hand-edited.

- Assets/StartupLife/Scenes/FirstPlayable.unity and .meta.
- Assets/StartupLife/Data/FirstPlayableContent.asset and .meta.
- Assets/StartupLife/Localization/FirstPlayableUI.asset, FirstPlayableUI_vi.asset, FirstPlayableUI Shared Data.asset and their .meta files.
- Localization Addressables group registrations and EditorBuildSettings scene registration.
- Existing MobileBaseline scene, MobileTheme, Vietnamese locale, Localization settings, TMP resources and Noto font are reused.

FirstPlayable is now the enabled launch scene; MobileBaseline remains enabled for regression tests. The static foundation guard accepts either verified mobile shell as launch and still requires the baseline scene.

The scene was reopened after saving. Verification checks missing scripts, authored Content and all controller references, TMP font assignments, 1080 × 1920 scaler, safe area, Main Camera, Input System EventSystem, persistent action listeners, and fresh Character Creation visible/Life and Summary hidden.

## Real Unity execution

Installed licensed Windows Editor **6000.3.25f1**, matching ProjectVersion.txt. No live MCP surface was available; the installed Editor was executed directly in batch mode. PlayMode used graphics (no -nographics), D3D and the project's URP pipeline.

| Gate | Actual result | Evidence |
|---|---|---|
| Import/compile/packages/asmdefs | PASS; zero Startup Life C# errors/warnings in final runs | [Builder/import log](M7-RUNTIME-2026-10-04/builders.log) |
| Builders/reopened bindings | PASS; baseline reused safely, Content/shell generated, final idempotent gate passed | [Builder log](M7-RUNTIME-2026-10-04/builders.log) |
| EditMode | **8/8 PASS** | [XML](M7-RUNTIME-2026-10-04/editmode-results.xml), [log](M7-RUNTIME-2026-10-04/editmode.log) |
| PlayMode | **2/2 PASS** | [XML](M7-RUNTIME-2026-10-04/playmode-results.xml), [log](M7-RUNTIME-2026-10-04/playmode.log) |
| Localization/glyphs | PASS, including existing Vietnamese baseline test and first-playable course label | PlayMode XML/log and captures |
| Portrait/simulated notch | PASS at 1080 × 1920 | Captures below |
| First-playable journey | PASS through actual generated scene and views | CreateWorkStudyAndAdvanceDayThroughViews |
| Normal save/reload | PASS; real AtomicFileSaveStore output, schema 1, generation 17 | [Synthetic journey save](M7-RUNTIME-2026-10-04/visuals/normal-save.json) |

The journey proves clean save isolation, bootstrap readiness, character creation/entry transition, Developer acceptance, accelerated work, XP, 17:00 work end, acknowledgement with zero duplicate cash/XP/skill/grants, course purchase, 60-minute study/progress, next day 00:00 and Summary visibility. Summary cash/XP match the authoritative snapshot. Scene reload preserves name/time/cash/XP/course progress and passes normal Application restore validation. Existing user saves are backed up in memory and restored by test teardown.

Observed end state: cash **1,800,000 ₫**, career XP **40**, course progress **600,000 units (16%)**, next date **2026-09-02**. The exported save contains the legitimate earned salary claim **5,000,000/11**, while work did not turn accrued salary into duplicate spendable cash. This inspection is test evidence; views do not read save bytes.

Published logs omit license identifiers and replace machine-specific user/project paths; compilation, execution, render and test diagnostics are retained. Original full logs remain in the local m7-runtime evidence directory.

## Runtime defects corrected

1. NewScene unloaded previously held asset references. Reload theme/font/Content after scene creation; repair the reviewed generated scene through Editor APIs and verify after reopening.
2. Empty localized values displayed English missing-translation diagnostics. Supply the ready message and an intentionally blank non-empty fatal value.
3. Dense layout shrank buttons. Use compact row/gap spacing and a 96-unit minimum touch height; verify full and simulated-notched geometry.
4. Playback refreshed button state before clearing IsRunning. Refresh in finally after clearing it; PlayMode asserts the work button recovers.
5. Summary summed only the remaining AdvanceDay boundaries and omitted earlier work XP. Show authoritative cash and total career XP from FirstPlayableState with accurate Vietnamese labels. FormerlySerializedAs preserves the scene's renamed cash field; this is not a save schema migration.
6. Replace obsolete TMP wrapping API with the installed package's textWrappingMode API.

Early failed test attempts are not reported as passes: the frame-count playback timeout was replaced by a real-time deadline; the recording output directory was created correctly; deliberate capture logs are explicitly expected while unexpected logs remain asserted. Final XML reports above are the successful runs.

## Visual evidence

Real UI-inclusive Unity camera/render-target captures, not generated mockups:

1. [Character Creation](M7-RUNTIME-2026-10-04/visuals/01-character-creation.png).
2. [Life after Developer acceptance](M7-RUNTIME-2026-10-04/visuals/02-life-developer.png).
3. [Work playback/status](M7-RUNTIME-2026-10-04/visuals/03-work-playback.png).
4. [Evening study/course](M7-RUNTIME-2026-10-04/visuals/04-evening-study.png).
5. [Day Summary](M7-RUNTIME-2026-10-04/visuals/05-day-summary.png).
6. [Simulated notched safe area](M7-RUNTIME-2026-10-04/visuals/06-simulated-notched-safe-area.png).
7. [Workday/evening recording](M7-RUNTIME-2026-10-04/visuals/workday-evening.gif): 36 actual Unity frame samples, encoded with sampled time deltas, approximately 10 seconds. This is an automated Editor recording, not a physical-device video.

All six screens were visually inspected. Capture assertions reject missing translations/glyphs, text overflow, buttons below the 96-unit target, and buttons leaving the safe area. The simulated notch uses y=90 through y=1830 in a 1920-high canvas. Development UI placeholders/theme only; no polished/generated production art.

## Engine-free regressions

[Machine-readable reports](M7-RUNTIME-2026-10-04/manifest.json):

| Suite | Pass/total |
|---|---|
| SimulationChecks | 45/45 |
| H1/H2/R2 foundation regressions | 19/19 |
| M2 restore/provenance | 35/35 |
| PresentationChecks (consolidated) | 11/11 |
| ContentCatalogChecks | 8/8 |
| Unity foundation static | 31/31 |
| Frozen M1 API | 31/31 |
| Content bridge static | 8/8 |
| M7 shell static | 28/28 |
| Runner contract static | 30/30 |
| Security/release hygiene | 28/28 |

All five .NET executable builds passed with zero warnings/errors. Documentation validation passed; latest final-head CI is resolved from the PR checks rather than copied from the baseline.

## Session closeout

**Skills used:** startup-life-session-orchestrator, startup-life-gameplay-guardian, vietnam-art-direction; official ui, ui-ugui, localization, unity-cli; pinned community unity-game-director, unity-gameplay-systems, unity-mcp-bridge, unity-qa-release, unity-ui-designer, unity-localization from dafb97ef00f94e64e42e6260bc6b3af74cc83dad. Project Unity/offline/uGUI/Vietnam rules override unrelated package defaults.

**Acceptance criteria:** one public-contract seam; truthful unique report; real pinned-Editor import/build/bindings/EditMode/PlayMode/portrait/localization/save proof. Local checks pass; official prerequisite closure remains pending.

**Tests:** actual suite counts above; static/.NET evidence is distinguished from Unity execution.

**Visual evidence:** six actual portrait captures and rendered work/evening recording above.

**Save impact:** none. SaveVersion/schema stays 1; no GameState/DTO/migration/Application/Infrastructure/Simulation/frozen Core API changes. Normal persistence route and historical retry architecture retained.

**Known limitations:** formal upstream task acceptance remains pending. M7-T02 still owns pause checkpoints, mid-work interruption, mid-course restart, Android lifecycle/device backup recovery and current iOS smoke. No pause/Android/iOS/IL2CPP/device claim is made. Overall first-playable/platform acceptance is not closed.

**Files changed:** Presentation consumers/duplicate removal; merged engine-free harness/obsolete harness removal; report/source/foundation guards; Editor builder/runtime gate; generated scene/Content/localization/Addressables/build settings; EditMode/PlayMode/evidence helper; this closeout and dated evidence bundle. Runtime font-cache churn was restored to the existing inputs.

**Commit:** ef321f3 (canonical seam), c236f95 (Unity shell/runtime proof), b2bc2cc (report integrity); later commits update documentation/evidence only. No automatic merge.
